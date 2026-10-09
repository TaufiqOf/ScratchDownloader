using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using bzTorrent;
using bzTorrent.Data;
using bzTorrent.DHT;
using bzTorrent.IO;
using ScratchDownloader.Models;

namespace ScratchDownloader.Services;

/// <summary>
/// Downloads a .torrent file via the bzTorrent peer-wire APIs.
/// Uri must identify a local .torrent file, an HTTP(S) .torrent file, or a magnet link.
/// A regular file URL is not a BitTorrent resource.
/// </summary>
public class BitTorrentDownloadService : IDownloadService, IDisposable
{
    private const int BlockSize = 16 * 1024;
    private const int TrackerPort = 6881;
    private const int PeerLimit = 40;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private long _lastBytes;
    private long _totalBytes;
    private Stopwatch? _speedWatch;
    private readonly ConcurrentDictionary<int, byte[]> _pieceBuffers = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, int>> _blockLengths = new();
    private readonly ConcurrentDictionary<int, byte> _completedPieces = new();
    private readonly ConcurrentDictionary<int, byte> _requestedBlocks = new();
    private readonly ConcurrentDictionary<string, byte> _connectedPeers = new();
    private readonly ConcurrentBag<IPeerWireClient> _clients = new();
    private IMetadata? _metadata;

    public DownloadProgress Progress { get; set; } = new();

    /// <summary>Maximum aggregate download speed in bytes per second; zero means unlimited.</summary>
    public double CapSpeed { get; set; }

    /// <summary>Number of peer connections to establish concurrently (minimum one, max PeerLimit).</summary>
    public int SegmentCount { get; set; } = 4;

    public Uri Uri { get; set; } = null!;
    public string? DestinationFilePath { get; set; }
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Completed;
    public event EventHandler? Initializing;
    public event EventHandler? Downloading;

    public BitTorrentDownloadService()
    {
    }

    public async Task<FileDataInformation> GetFileDataInformation(string uri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("A torrent file path or magnet URI is required.", nameof(uri));

        var result = new FileDataInformation();
        IMetadata metadata;
        if (uri.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            metadata = MagnetLink.ResolveToMetadata(uri);
            var magnet = MagnetLink.Resolve(uri);
            result.Uri = new Uri(uri);
            result.FinalUri = new Uri(uri);
            result.FileName = string.IsNullOrWhiteSpace(magnet.Name) ? "torrent-download" : magnet.Name;
            result.FileSizeBytes = 0; // Magnet metadata is not available until peers exchange it.
            result.FileExtension = "TORRENT";
            return result;
        }

        byte[] torrentBytes;
        if (System.Uri.TryCreate(uri, UriKind.Absolute, out var source) &&
            (source.Scheme == System.Uri.UriSchemeHttp || source.Scheme == System.Uri.UriSchemeHttps))
        {
            using var client = new HttpClient();
            torrentBytes = await client.GetByteArrayAsync(source, cancellationToken).ConfigureAwait(false);
            result.Uri = source;
            result.FinalUri = source;
        }
        else
        {
            var path = Path.GetFullPath(uri);
            torrentBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            result.Uri = new Uri(path);
            result.FinalUri = new Uri(path);
        }

        using var stream = new MemoryStream(torrentBytes);
        metadata = new Metadata(stream);
        if (metadata.PieceSize <= 0 || metadata.PieceHashes.Count == 0)
            throw new InvalidDataException("The supplied file does not contain complete BitTorrent metadata.");

        var fileInfos = metadata.GetFileInfos();
        result.FileName = IsMultiFileTorrent(metadata)
            ? metadata.Name
            : fileInfos.FirstOrDefault()?.Filename ?? metadata.Name ?? "download";
        result.FileSizeBytes = fileInfos.Sum(f => f.FileSize);
        result.FileExtension = Path.GetExtension(result.FileName).TrimStart('.').ToUpperInvariant();
        return result;
    }

    public bool CanHandle(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        url = url.Trim();
        if (url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ||
                   uri.Scheme == Uri.UriSchemeFile)
               && uri.AbsolutePath.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pieceBuffers.Clear();
            _blockLengths.Clear();
            _completedPieces.Clear();
            _requestedBlocks.Clear();
            _connectedPeers.Clear();
            var token = _cts.Token;
            _worker = Task.Run(() => RunAsync(token));
        }
    }

    public void Resume(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_worker is { IsCompleted: false })
                return;
        }

        Start(cancellationToken);
    }

    public void Pause()
    {
        _cts?.Cancel();
        DisconnectPeers();
    }

    public void Stop()
    {
        _cts?.Cancel();
        DisconnectPeers();
        lock (_sync)
        {
            _pieceBuffers.Clear();
            _blockLengths.Clear();
            _requestedBlocks.Clear();
            _completedPieces.Clear();
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            ValidateInputs();
            Initializing?.Invoke(this, EventArgs.Empty);
            _metadata = await LoadMetadataAsync(Uri, token).ConfigureAwait(false);
            if (_metadata.PieceSize <= 0 || _metadata.PieceHashes.Count == 0)
                throw new InvalidOperationException(
                    "The magnet link does not yet contain torrent metadata. Magnet metadata exchange is not supported by this service; use a .torrent file.");

            if (string.IsNullOrWhiteSpace(_metadata.HashString))
                throw new InvalidDataException("Torrent info hash is missing.");
            var files = _metadata.GetFileInfos();
            _totalBytes = files.Sum(f => f.FileSize);
            if (_totalBytes < 0) throw new InvalidDataException("Torrent size is invalid.");
            CreateOutputFiles(files);
            InitializeProgress(_metadata);
            Downloading?.Invoke(this, EventArgs.Empty);

            // Web seeds are supported directly by bzTorrent. Peer-wire is used as a complementary source.
            await DownloadFromTrackersAsync(token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();
            if (_completedPieces.Count != _metadata.PieceHashes.Count)
                throw new IOException(
                    $"Torrent download incomplete: {_completedPieces.Count}/{_metadata.PieceHashes.Count} pieces downloaded. The current service implementation needs reachable peers or working web seeds.");

            Progress.Progress = 100;
            Progress.BytesDownloaded = _totalBytes;
            Progress.TotalBytes = _totalBytes;
            Progress.BytesPerSecond = 0;
            foreach (var segment in Progress.SegmentProgress.Values) segment.Progress = 100;
            Completed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is the expected pause/stop mechanism; already written verified pieces remain reusable.
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
        finally
        {
            DisconnectPeers();
        }
    }

    private async Task<IMetadata> LoadMetadataAsync(Uri source, CancellationToken token)
    {
        if (source.IsAbsoluteUri && source.Scheme.Equals("magnet", StringComparison.OrdinalIgnoreCase))
            return await Task.Run(() => MagnetLink.ResolveToMetadata(source.ToString()), token).ConfigureAwait(false);

        byte[] bytes;
        if (source.IsAbsoluteUri &&
            (source.Scheme == System.Uri.UriSchemeHttp || source.Scheme == System.Uri.UriSchemeHttps))
        {
            using var client = new HttpClient();
            bytes = await client.GetByteArrayAsync(source, token).ConfigureAwait(false);
        }
        else
        {
            var path = source.IsAbsoluteUri && source.IsFile ? source.LocalPath : source.OriginalString;
            bytes = await File.ReadAllBytesAsync(path, token).ConfigureAwait(false);
        }

        return await Task.Run(() =>
        {
            using var stream = new MemoryStream(bytes);
            return (IMetadata)new Metadata(stream);
        }, token).ConfigureAwait(false);
    }

    private void CreateOutputFiles(IReadOnlyCollection<MetadataFileInfo> files)
    {
        var destination = Path.GetFullPath(DestinationFilePath!);
        var isMultiFile = IsMultiFileTorrent(_metadata!);
        var root = isMultiFile ? destination : Path.GetDirectoryName(destination)!;
        if (isMultiFile)
            Directory.CreateDirectory(root);
        else
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        foreach (var file in files)
        {
            var target = isMultiFile
                ? Path.GetFullPath(Path.Combine(root, file.Filename.Replace('/', Path.DirectorySeparatorChar)))
                : destination;
            if (isMultiFile &&
                !target.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Torrent contains an unsafe file path.");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var stream = new FileStream(target, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            if (stream.Length != file.FileSize) stream.SetLength(file.FileSize);
        }
    }

    private void InitializeProgress(IMetadata metadata)
    {
        var pieces = metadata.PieceHashes.Count;
        var count = Math.Max(1, Math.Min(Math.Max(1, SegmentCount), Math.Max(1, pieces)));
        Progress.SegmentProgress = new ConcurrentDictionary<int, SegmentProgress>();
        for (var i = 0; i < count; i++)
        {
            var p = new SegmentProgress
                { Index = i, BytesDownloaded = 0, TotalBytes = 0, Progress = 0, BytesPerSecond = 0 };
            Progress.SegmentProgress[i] = p;
        }

        Progress.TotalBytes = _totalBytes;
        Progress.BytesDownloaded = 0;
        Progress.Progress = 0;
        Progress.BytesPerSecond = 0;
        _speedWatch = Stopwatch.StartNew();
        _lastBytes = 0;
    }

    private async Task DownloadFromTrackersAsync(CancellationToken token)
    {
        var trackerUrls = new List<string>();

        if (!string.IsNullOrWhiteSpace(_metadata!.Announce))
            trackerUrls.Add(_metadata.Announce);

        if (_metadata.AnnounceList != null)
            trackerUrls.AddRange(_metadata.AnnounceList);

        trackerUrls = trackerUrls
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (trackerUrls.Count == 0)
        {
            throw new InvalidOperationException(
                "The torrent contains no tracker URLs. " +
                "This service currently does not implement DHT peer discovery.");
        }

        var peers = new List<IPEndPoint>();
        var dhtPeers = new ConcurrentDictionary<string, IPEndPoint>(StringComparer.Ordinal);

        // DHT is independent of trackers and can find peers when a tracker returns
        // only a small peer list. Keep it alive while connecting to discovered peers.
        using var dht = new DHTClient();
        dht.PeerFound += endpoint =>
        {
            if (endpoint != null)
                dhtPeers.TryAdd(endpoint.ToString(), endpoint);
        };

        try
        {
            var bootstrapNodes = new List<IPEndPoint>();
            foreach (var host in new[]
                     {
                         "router.bittorrent.com",
                         "router.utorrent.com",
                         "dht.transmissionbt.com"
                     })
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var address = Dns.GetHostAddresses(host)
                        .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (address != null)
                        bootstrapNodes.Add(new IPEndPoint(address, 6881));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DHT bootstrap DNS lookup failed ({host}): {ex.Message}");
                }
            }

            if (bootstrapNodes.Count > 0)
            {
                await dht.BootstrapAsync(bootstrapNodes).ConfigureAwait(false);
                dht.StartSearch(_metadata.Hash);
                Debug.WriteLine($"DHT search started with {bootstrapNodes.Count} bootstrap nodes.");
            }
            else
            {
                Debug.WriteLine("DHT search not started: no bootstrap nodes resolved.");
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DHT startup failed; continuing with trackers: {ex.Message}");
        }

        foreach (var trackerUrl in trackerUrls)
        {
            token.ThrowIfCancellationRequested();

            if (!trackerUrl.StartsWith("udp://", StringComparison.OrdinalIgnoreCase) &&
                !trackerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !trackerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Skipping unsupported tracker URL: {trackerUrl}");
                continue;
            }

            Debug.WriteLine($"Announcing to tracker: {trackerUrl}");

            var trackerSucceeded = false;

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    ITrackerClient tracker =
                        trackerUrl.StartsWith(
                            "udp://",
                            StringComparison.OrdinalIgnoreCase)
                            ? new UDPTrackerClient()
                            : new HTTPTrackerClient();

                    var peerId = CreatePeerId();

                    // Prefer the extended announce overload when this installed
                    // bzTorrent version exposes it; otherwise fall back to the
                    // stable three-argument API.
                    var announceResult = await Task.Run(
                        () => InvokeTrackerAnnounce(
                            tracker,
                            trackerUrl,
                            _metadata.HashString,
                            peerId,
                            Math.Max(0L, _totalBytes - Progress.BytesDownloaded)),
                        token
                    ).ConfigureAwait(false);

                    var peersProperty = announceResult?.GetType().GetProperty("Peers");
                    var returnedPeers = (peersProperty?.GetValue(announceResult) as IEnumerable<IPEndPoint>)?
                        .Where(p => p != null)
                        .ToList() ?? new List<IPEndPoint>();

                    Debug.WriteLine(
                        $"Tracker {trackerUrl}, attempt {attempt}: " +
                        $"returned {returnedPeers.Count} peers.");

                    peers.AddRange(returnedPeers);
                    trackerSucceeded = true;

                    // A successful response with zero peers is still a successful
                    // announce. Retry only if you want to query the tracker again
                    // for a fresh peer list.
                    if (returnedPeers.Count > 0)
                        break;

                    if (attempt < 3)
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(2 * attempt),
                            token
                        ).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"Tracker {trackerUrl}, attempt {attempt} failed: " +
                        $"{ex.GetType().Name}: {ex.Message}");

                    if (attempt < 3)
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(2 * attempt),
                            token
                        ).ConfigureAwait(false);
                    }
                }
            }

            if (!trackerSucceeded)
            {
                Debug.WriteLine(
                    $"Tracker failed after retries: {trackerUrl}");
            }
        }

        // Give DHT a short window to discover peers. DHT remains alive for the
        // rest of this method, so new endpoints can also be collected meanwhile.
        var dhtCollectionWindow = TimeSpan.FromSeconds(12);
        var dhtWatch = Stopwatch.StartNew();
        while (dhtWatch.Elapsed < dhtCollectionWindow)
        {
            token.ThrowIfCancellationRequested();
            if (_completedPieces.Count == _metadata.PieceHashes.Count)
                break;

            await Task.Delay(250, token).ConfigureAwait(false);
            if (dhtPeers.Count >= PeerLimit)
                break;
        }

        peers.AddRange(dhtPeers.Values);

        // Deduplicate endpoints from trackers and DHT.
        peers = peers
            .Where(p => p != null)
            .Distinct(new IpEndPointComparer())
            .Take(PeerLimit)
            .ToList();

        Debug.WriteLine(
            $"Peer discovery complete: {peers.Count} unique peers " +
            $"(DHT contributed {dhtPeers.Count}).");

        if (peers.Count == 0)
        {
            throw new IOException(
                "No peers were discovered by trackers or DHT. Check tracker diagnostics, " +
                "outbound UDP connectivity, and the torrent's DHT support.");
        }

        var parallelism = Math.Max(
            1,
            Math.Min(Math.Max(1, SegmentCount), peers.Count));

        using var semaphore = new SemaphoreSlim(parallelism);

        var tasks = peers.Select(async peer =>
        {
            await semaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                Debug.WriteLine($"Trying BitTorrent peer: {peer}");

                await DownloadPeerAsync(peer, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Peer {peer} failed: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private object? InvokeTrackerAnnounce(
        ITrackerClient tracker,
        string trackerUrl,
        string infoHash,
        string peerId,
        long bytesLeft)
    {
        var methods = tracker.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Announce")
            .OrderByDescending(m => m.GetParameters().Length)
            .ToList();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length < 3 ||
                parameters[0].ParameterType != typeof(string) ||
                parameters[1].ParameterType != typeof(string) ||
                parameters[2].ParameterType != typeof(string))
                continue;

            var args = new object?[parameters.Length];
            args[0] = trackerUrl;
            args[1] = infoHash;
            args[2] = peerId;

            for (var i = 3; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var name = parameter.Name?.ToLowerInvariant() ?? string.Empty;
                object? value = parameter.HasDefaultValue ? parameter.DefaultValue : null;

                if (name.Contains("download")) value = Progress.BytesDownloaded;
                else if (name == "left" || name.Contains("bytesleft")) value = bytesLeft;
                else if (name.Contains("upload")) value = 0L;
                else if (name.Contains("numwant")) value = 200;
                else if (name == "port" || name.Contains("listenport")) value = TrackerPort;
                else if (name.Contains("ipaddress")) value = 0;
                else if (name == "key") value = 0;
                else if (name == "compact") value = 0;
                else if (name == "event") value = 2;

                if (value == null && parameter.ParameterType.IsValueType &&
                    Nullable.GetUnderlyingType(parameter.ParameterType) == null)
                    value = Activator.CreateInstance(parameter.ParameterType);

                if (value != null && !parameter.ParameterType.IsInstanceOfType(value))
                {
                    try { value = Convert.ChangeType(value, parameter.ParameterType); }
                    catch { value = parameter.HasDefaultValue ? parameter.DefaultValue : Activator.CreateInstance(parameter.ParameterType); }
                }
                args[i] = value;
            }

            try
            {
                Debug.WriteLine(
                    $"Using tracker overload: {method} " +
                    $"(numWant={args.Select((value, index) => new { value, index }) .FirstOrDefault(x => parameters[x.index].Name?.Contains("numwant", StringComparison.OrdinalIgnoreCase) == true)?.value ?? "default"})");
                return method.Invoke(tracker, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Debug.WriteLine($"Tracker announce overload failed: {ex.InnerException.Message}");
                // Try the next available overload, including the legacy API.
            }
        }

        throw new MissingMethodException(
            "No compatible bzTorrent tracker Announce overload was found.");
    }

    private async Task DownloadPeerAsync(IPEndPoint endpoint, CancellationToken token)
    {
        var key = endpoint.ToString();
        if (!_connectedPeers.TryAdd(key, 0)) return;
        var connection = new PeerWireConnection<TCPSocket> { Timeout = 8 };
        var client = new PeerWireClient(connection)
        {
            Hash = _metadata!.HashString,
            LocalPeerID = CreatePeerId(),
            KeepConnectionAlive = true
        };
        _clients.Add(client);
        var piecesAvailable = new ConcurrentDictionary<int, byte>();
        var blockSignal = new SemaphoreSlim(0);
        var pending = new ConcurrentDictionary<int, byte>();
        client.BitField += (_, _, bitfield) =>
        {
            for (var i = 0; i < bitfield.Length; i++)
                if (bitfield[i])
                    piecesAvailable[i] = 0;
        };
        client.Have += (_, index) => piecesAvailable[index] = 0;
        client.UnChoke += _ => client.SendInterested();
        client.Piece += (_, pieceIndex, begin, data) =>
        {
            try
            {
                if (pieceIndex < 0 || pieceIndex >= _metadata.PieceHashes.Count || begin < 0) return;
                var keyBlock = (pieceIndex * 100000) + begin;
                if (!_requestedBlocks.ContainsKey(keyBlock)) return;
                var expected = Math.Min(BlockSize, GetPieceLength(pieceIndex) - begin);
                if (expected <= 0 || data.Length != expected ||
                    begin + data.Length > GetPieceLength(pieceIndex)) return;
                var buffer = _pieceBuffers.GetOrAdd(pieceIndex, _ => new byte[GetPieceLength(pieceIndex)]);
                Buffer.BlockCopy(data, 0, buffer, begin, data.Length);
                var lengths = _blockLengths.GetOrAdd(pieceIndex, _ => new ConcurrentDictionary<int, int>());
                lengths[begin] = data.Length;
                pending.TryRemove(keyBlock, out byte ignoredPending);
                _requestedBlocks.TryRemove(keyBlock, out byte ignoredRequested);
                var wantedBlocks = (int)Math.Ceiling(GetPieceLength(pieceIndex) / (double)BlockSize);
                if (lengths.Count == wantedBlocks && VerifyPiece(pieceIndex, buffer))
                {
                    lock (_sync) SavePiece(pieceIndex, buffer);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        };
        blockSignal.Release();

        try
        {
            await Task.Run((Action)(() =>
            {
                try
                {
                    client.Connect(endpoint);
                    client.Handshake(_metadata!.HashString, client.LocalPeerID);
                    var timeout = Stopwatch.StartNew();
                    while (!token.IsCancellationRequested && timeout.Elapsed < TimeSpan.FromSeconds(12))
                    {
                        if (!client.Process()) break;
                        if (client.ReceivedHandshake) break;
                        Thread.Sleep(5);
                    }
                }
                catch (Exception e) when (!token.IsCancellationRequested)
                {
                    // Unreachable BitTorrent peers are common. Log the endpoint and move on.
                    Debug.WriteLine($"Peer connection/handshake failed ({endpoint}): {e.Message}");
                }
                catch (OperationCanceledException)
                {
                    // Expected when paused or stopped.
                }
            }), token).ConfigureAwait(false);

            if (!client.ReceivedHandshake) return;
            client.SendInterested();
            // The bzTorrent peer client is synchronously polled; request blocks in a bounded worker.
            await Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _completedPieces.Count < _metadata!.PieceHashes.Count &&
                       client.ReceivedHandshake)
                {
                    if (!client.Process()) break;
                    var candidatePieces = piecesAvailable.Keys
                        .Where(i => !_completedPieces.ContainsKey(i))
                        .OrderBy(i => i)
                        .ToArray();
                    var requestedAny = false;
                    foreach (var targetPiece in candidatePieces)
                    {
                        var totalLength = GetPieceLength(targetPiece);
                        for (var offset = 0; offset < totalLength; offset += BlockSize)
                        {
                            var blockKey = targetPiece * 100000 + offset;
                            if (_completedPieces.ContainsKey(targetPiece) || !_requestedBlocks.TryAdd(blockKey, 0))
                                continue;
                            var length = Math.Min(BlockSize, totalLength - offset);
                            pending[blockKey] = 0;
                            client.SendRequest((uint)targetPiece, (uint)offset, (uint)length);
                            requestedAny = true;
                            // Keep the outstanding request pipeline small for this synchronous peer API.
                            if (pending.Count >= 4) break;
                        }

                        if (pending.Count >= 4) break;
                    }

                    await Task.Delay(requestedAny ? 5 : 20, token).ConfigureAwait(false);
                }
            }, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            Debug.WriteLine($"Peer {endpoint} failed: {ex.Message}");
        }
        finally
        {
            try
            {
                client.Disconnect();
            }
            catch
            {
            }

            blockSignal.Dispose();
        }
    }

    private int GetPieceLength(int pieceIndex)
    {
        var start = (long)pieceIndex * _metadata!.PieceSize;
        return (int)Math.Min(_metadata.PieceSize, _totalBytes - start);
    }

    private bool VerifyPiece(int pieceIndex, byte[] bytes)
    {
        var hashes = _metadata!.PieceHashes.ToArray();
        if (pieceIndex < 0 || pieceIndex >= hashes.Length) return false;
        using var sha1 = SHA1.Create();
        var computed = sha1.ComputeHash(bytes);
        return computed.SequenceEqual(hashes[pieceIndex]);
    }

    private void SavePiece(int pieceIndex, byte[] bytes)
    {
        if (_completedPieces.ContainsKey(pieceIndex)) return;
        var pieceStart = (long)pieceIndex * _metadata!.PieceSize;
        var pieceEnd = pieceStart + bytes.Length;
        foreach (var file in _metadata.GetFileInfos())
        {
            var fileEnd = file.FileStartByte + file.FileSize;
            if (pieceEnd <= file.FileStartByte || pieceStart >= fileEnd) continue;
            var sourceOffset = (int)Math.Max(file.FileStartByte - pieceStart, 0);
            var fileOffset = Math.Max(pieceStart - file.FileStartByte, 0);
            var count = (int)Math.Min(bytes.Length - sourceOffset, file.FileSize - fileOffset);
            var path = GetOutputPath(file);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.Seek(fileOffset, SeekOrigin.Begin);
            stream.Write(bytes, sourceOffset, count);
        }

        if (!_completedPieces.TryAdd(pieceIndex, 0)) return;
        _pieceBuffers.TryRemove(pieceIndex, out byte[]? ignoredBuffer);
        _blockLengths.TryRemove(pieceIndex, out ConcurrentDictionary<int, int>? ignoredLengths);
        UpdateProgress(bytes.Length);
    }

    private string GetOutputPath(MetadataFileInfo file)
    {
        var destination = Path.GetFullPath(DestinationFilePath!);
        return !IsMultiFileTorrent(_metadata!)
            ? destination
            : Path.GetFullPath(Path.Combine(destination, file.Filename.Replace('/', Path.DirectorySeparatorChar)));
    }

    private void UpdateProgress(int addedBytes)
    {
        var downloaded = _completedPieces.Keys.Sum(GetPieceLengthSafe);
        Progress.BytesDownloaded = downloaded;
        Progress.TotalBytes = _totalBytes;
        Progress.Progress = _totalBytes == 0 ? 100 : Math.Min(100, downloaded * 100.0 / _totalBytes);
        _speedWatch ??= Stopwatch.StartNew();
        var elapsed = _speedWatch.Elapsed.TotalSeconds;
        Progress.BytesPerSecond = elapsed > 0 ? Math.Max(0, downloaded - _lastBytes) / elapsed : 0;
        _lastBytes = downloaded;
        _speedWatch.Restart();
        if (Progress.SegmentProgress.Count <= 0) return;
        var s = Progress.SegmentProgress.Values.ElementAt(_completedPieces.Count % Progress.SegmentProgress.Count);
        s.BytesDownloaded += addedBytes;
        s.TotalBytes = _totalBytes;
        s.Progress = Progress.Progress;
        s.BytesPerSecond = Progress.BytesPerSecond;
    }

    private int GetPieceLengthSafe(int pieceIndex) => GetPieceLength(pieceIndex);

    private async Task ApplySpeedLimitAsync(int bytes, CancellationToken token)
    {
        if (CapSpeed <= 0) return;
        var milliseconds = bytes / CapSpeed * 1000d;
        if (milliseconds > 0) await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), token).ConfigureAwait(false);
    }

    private static string CreatePeerId()
    {
        // 20-byte peer ID: client prefix followed by a stable-size random suffix.
        return "-SD0100-" + Guid.NewGuid().ToString("N").Substring(0, 12);
    }

    // Reflection keeps this service compatible with bzTorrent package versions whose
    // IMetadata interface predates the IsMultiFile property.
    private static bool IsMultiFileTorrent(IMetadata metadata)
    {
        var property = metadata.GetType().GetProperty("IsMultiFile");
        if (property?.PropertyType == typeof(bool))
            return (bool)(property.GetValue(metadata) ?? false);

        var files = metadata.GetFileInfos();
        return files.Count > 1;
    }

    private void ValidateInputs()
    {
        if (Uri == null) throw new InvalidOperationException("Uri must be set before starting.");
        if (string.IsNullOrWhiteSpace(DestinationFilePath))
            throw new InvalidOperationException("DestinationFilePath must be set.");
    }

    private void DisconnectPeers()
    {
        foreach (var client in _clients)
        {
            try
            {
                client.Disconnect();
            }
            catch
            {
            }
        }

        _connectedPeers.Clear();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }

    private sealed class IpEndPointComparer : IEqualityComparer<IPEndPoint>
    {
        public bool Equals(IPEndPoint? x, IPEndPoint? y) => x != null && y != null && x.Equals(y);
        public int GetHashCode(IPEndPoint obj) => obj.GetHashCode();
    }
}