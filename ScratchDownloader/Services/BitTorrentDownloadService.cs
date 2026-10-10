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
using Avalonia.Threading;
using bzTorrent;
using bzTorrent.Data;
using bzTorrent.DHT;
using bzTorrent.IO;
using ScratchDownloader.Models;

namespace ScratchDownloader.Services;

/// <summary>
/// Downloads a .torrent file or magnet link via the bzTorrent peer-wire APIs.
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
    private readonly ConcurrentDictionary<string, TrackerStatus> _trackerStatuses =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PeerStatus> _peerStatuses =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _queuedPeers =
        new(StringComparer.OrdinalIgnoreCase);
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
    public event EventHandler? Processing;

    public BitTorrentDownloadService()
    {
    }

    public async Task<FileDataInformation> GetFileDataInformation(string uri,
                                                                  CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"[BitTorrent] GetFileDataInformation started for URI: {uri}");
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("A torrent file path or magnet URI is required.", nameof(uri));

        var result = new FileDataInformation
        {
            DownloadType = DownloadType.BitTorrent
        };

        if (uri.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine("[BitTorrent] Resolving magnet link basic info...");
            var magnet = MagnetLink.Resolve(uri);
            result.Uri = new Uri(uri);
            result.FinalUri = new Uri(uri);
            result.FileName = string.IsNullOrWhiteSpace(magnet.Name) ? "torrent-download" : magnet.Name;
            result.FileSizeBytes = 0; // Size unknown until metadata is fetched from peers.
            result.FileExtension = "TORRENT";
            Debug.WriteLine($"[BitTorrent] Magnet basic info resolved. Name: {result.FileName}");
            return result;
        }

        byte[] torrentBytes;
        if (System.Uri.TryCreate(uri, UriKind.Absolute, out var source) &&
            (source.Scheme == System.Uri.UriSchemeHttp || source.Scheme == System.Uri.UriSchemeHttps))
        {
            Debug.WriteLine($"[BitTorrent] Downloading .torrent file from HTTP source: {source}");
            using var client = new HttpClient();
            torrentBytes = await client.GetByteArrayAsync(source, cancellationToken).ConfigureAwait(false);
            result.Uri = source;
            result.FinalUri = source;
        }
        else
        {
            var path = Path.GetFullPath(uri);
            Debug.WriteLine($"[BitTorrent] Reading local .torrent file from path: {path}");
            torrentBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            result.Uri = new Uri(path);
            result.FinalUri = new Uri(path);
        }

        using var stream = new MemoryStream(torrentBytes);
        var metadata = new Metadata(stream);
        if (metadata.PieceSize <= 0 || metadata.PieceHashes.Count == 0)
            throw new InvalidDataException("The supplied file does not contain complete metadata.");

        var fileInfos = metadata.GetFileInfos();
        result.FileName = IsMultiFileTorrent(metadata)
        ? metadata.Name
        : fileInfos.FirstOrDefault()?.Filename ?? metadata.Name ?? "download";
        result.FileSizeBytes = fileInfos.Sum(f => f.FileSize);
        result.FileExtension = Path.GetExtension(result.FileName).TrimStart('.').ToUpperInvariant();
        Debug.WriteLine(
            $"[BitTorrent] File info parsed successfully. Name: {result.FileName}, Size: {result.FileSizeBytes} bytes");
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
        Debug.WriteLine("[BitTorrent] Start called.");
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
            _queuedPeers.Clear();
            ResetConnectionStatuses();
            var token = _cts.Token;
            _worker = Task.Run(() => RunAsync(token));
        }
    }

    public void Resume(CancellationToken cancellationToken = default)
    {
        Debug.WriteLine("[BitTorrent] Resume called.");
        lock (_sync)
        {
            if (_worker is { IsCompleted: false })
                return;
        }

        Start(cancellationToken);
    }

    public void Pause()
    {
        Debug.WriteLine("[BitTorrent] Pause called.");
        _cts?.Cancel();
        DisconnectPeers();
    }

    public void Stop()
    {
        Debug.WriteLine("[BitTorrent] Stop called.");
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
            Debug.WriteLine("[BitTorrent] RunAsync worker task started.");
            ValidateInputs();
            Initializing?.Invoke(this, EventArgs.Empty);

            _metadata = await LoadMetadataAsync(Uri, token).ConfigureAwait(false);

            // Timeout or maximum attempts safeguard to prevent infinite looping when metadata cannot be resolved
            var metadataStopwatch = Stopwatch.StartNew();
            var maxMetadataWait = TimeSpan.FromSeconds(45);

            while ((_metadata == null || _metadata.PieceSize <= 0 || _metadata.PieceHashes.Count == 0) &&
                !token.IsCancellationRequested)
            {
                if (metadataStopwatch.Elapsed > maxMetadataWait)
                {
                    throw new TimeoutException("Timed out waiting to resolve torrent metadata from peers or trackers.");
                }

                Debug.WriteLine(
                    "[BitTorrent] Magnet metadata not yet available. Attempting exchange to fetch metadata...");
                try
                {
                    _metadata = await TryResolveMagnetMetadataAsync(Uri.ToString(), token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BitTorrent] Magnet metadata fetch attempt failed: {ex.Message}");
                }

                if (_metadata == null || _metadata.PieceSize <= 0 || _metadata.PieceHashes.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
                }
            }

            token.ThrowIfCancellationRequested();

            if (_metadata == null || string.IsNullOrWhiteSpace(_metadata.HashString) ||
                _metadata.PieceHashes.Count == 0)
                throw new InvalidOperationException(
                    "Failed to resolve torrent metadata from the magnet link or source.");

            var files = _metadata.GetFileInfos();
            _totalBytes = files.Sum(f => f.FileSize);
            if (_totalBytes < 0) throw new InvalidDataException("Torrent size is invalid.");
            CreateOutputFiles(files);
            InitializeProgress(_metadata);
            Downloading?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine(
                $"[BitTorrent] Initialization complete. Total pieces: {_metadata.PieceHashes.Count}, Total Size: {_totalBytes} bytes.");

            // Continuous background tracker & DHT discovery running alongside active peer downloads
            var peerQueue = new ConcurrentQueue<IPEndPoint>();
            using var discoveryCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            var discoveryTask = StartContinuousDiscoveryAsync(peerQueue, discoveryCts.Token);
            var consumerTask = StartPeerConsumerAsync(peerQueue, token);

            // Wait until download completes or cancellation requested
            while (!token.IsCancellationRequested && _completedPieces.Count < _metadata.PieceHashes.Count)
            {
                await Task.Delay(1000, token).ConfigureAwait(false);
            }

            discoveryCts.Cancel();
            try { await discoveryTask; } catch { }

            token.ThrowIfCancellationRequested();
            Progress.Progress = 100;
            Progress.BytesDownloaded = _totalBytes;
            Progress.TotalBytes = _totalBytes;
            Progress.BytesPerSecond = 0;
            foreach (var segment in Progress.SegmentProgress.Values) segment.Progress = 100;
            Debug.WriteLine("[BitTorrent] Download completed successfully!");
            Completed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[BitTorrent] RunAsync cancelled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[BitTorrent] RunAsync failed with unhandled exception: {ex.GetType().Name}: {ex.Message}");
            ErrorOccurred?.Invoke(this, ex.Message);
        }
        finally
        {
            DisconnectPeers();
            Debug.WriteLine("[BitTorrent] RunAsync finished and peers disconnected.");
        }
    }

    private async Task StartContinuousDiscoveryAsync(ConcurrentQueue<IPEndPoint> peerQueue, CancellationToken token)
    {
        var trackerUrls = new List<string>();

        if (_metadata != null)
        {
            if (!string.IsNullOrWhiteSpace(_metadata.Announce))
                trackerUrls.Add(_metadata.Announce);

            if (_metadata.AnnounceList != null)
                trackerUrls.AddRange(_metadata.AnnounceList);
        }

        if (Uri != null && Uri.ToString().StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var magnet = MagnetLink.Resolve(Uri.ToString());
                if (magnet.Trackers != null)
                    trackerUrls.AddRange(magnet.Trackers);
            }
            catch
            {
            }
        }

        trackerUrls = trackerUrls
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        foreach (var trackerUrl in trackerUrls)
            GetOrAddTrackerStatus(trackerUrl);

        using var dht = new DHTClient();
        dht.PeerFound += endpoint =>
        {
            if (endpoint != null && !_connectedPeers.ContainsKey(endpoint.ToString()))
            {
                RecordPeerDiscovery(endpoint, "DHT", peerQueue);
                Debug.WriteLine($"[DHT] New peer discovered and queued: {endpoint}");
            }
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
                try
                {
                    var address = Dns.GetHostAddresses(host)
                    .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (address != null)
                        bootstrapNodes.Add(new IPEndPoint(address, 6881));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DHT] Bootstrap DNS lookup failed ({host}): {ex.Message}");
                }
            }

            if (bootstrapNodes.Count > 0)
            {
                await dht.BootstrapAsync(bootstrapNodes).ConfigureAwait(false);
                if (_metadata != null && _metadata.Hash != null)
                {
                    dht.StartSearch(_metadata.Hash);
                }
                else if (Uri != null && Uri.ToString().StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
                {
                    var magnet = MagnetLink.Resolve(Uri.ToString());
                    if (magnet.Hash != null)
                        dht.StartSearch(magnet.Hash);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DHT] Startup failed: {ex.Message}");
        }

        // Continuously loop over trackers and keep searching for new peers
        while (!token.IsCancellationRequested)
        {
            foreach (var trackerUrl in trackerUrls)
            {
                if (token.IsCancellationRequested) break;

                if (!trackerUrl.StartsWith("udp://", StringComparison.OrdinalIgnoreCase) &&
                    !trackerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !trackerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateTrackerStatus(trackerUrl, status => status.Status = "Unsupported");
                    continue;
                }

                try
                {
                    UpdateTrackerStatus(trackerUrl, status =>
                    {
                        status.Status = "Announcing";
                        status.LastError = null;
                    });
                    Debug.WriteLine($"[Tracker] Announcing to continuous tracker: {trackerUrl}");
                    ITrackerClient tracker =
                    trackerUrl.StartsWith("udp://", StringComparison.OrdinalIgnoreCase)
                    ? new UDPTrackerClient() { Timeout = 10 }
                    : new HTTPTrackerClient() { Timeout = 10 };

                    var peerId = CreatePeerId();
                    var hashString = _metadata?.HashString ?? MagnetLink.Resolve(Uri.ToString()).HashString;

                    var announceResult = await Task.Run(
                        () => InvokeTrackerAnnounce(
                            tracker,
                            trackerUrl,
                            hashString,
                            peerId,
                            Math.Max(0L, _totalBytes - Progress.BytesDownloaded)),
                                                        token
                    ).ConfigureAwait(false);

                    var peersProperty = announceResult?.GetType().GetProperty("Peers");
                    var returnedPeers = (peersProperty?.GetValue(announceResult) as IEnumerable<IPEndPoint>)?
                    .Where(p => p != null)
                    .ToList() ?? new List<IPEndPoint>();

                    Debug.WriteLine($"[Tracker] {trackerUrl} returned {returnedPeers.Count} peers.");
                    UpdateTrackerStatus(trackerUrl, status =>
                    {
                        status.Status = "Active";
                        status.PeersDiscovered = returnedPeers.Count;
                        status.LastAnnouncedAt = DateTime.Now;
                        status.LastError = null;
                    });
                    foreach (var p in returnedPeers)
                    {
                        if (!_connectedPeers.ContainsKey(p.ToString()))
                        {
                            RecordPeerDiscovery(p, trackerUrl, peerQueue);
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    UpdateTrackerStatus(trackerUrl, status => status.Status = "Stopped");
                    throw;
                }
                catch (Exception ex)
                {
                    UpdateTrackerStatus(trackerUrl, status =>
                    {
                        status.Status = "Error";
                        status.LastError = ex.Message;
                        status.LastAnnouncedAt = DateTime.Now;
                    });
                    Debug.WriteLine($"[Tracker] Continuous query failed for {trackerUrl}: {ex.Message}");
                }
            }

            // Wait before re-announcing to trackers to avoid spamming
            await Task.Delay(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
        }
    }

    private async Task StartPeerConsumerAsync(ConcurrentQueue<IPEndPoint> peerQueue, CancellationToken token)
    {
        var semaphore = new SemaphoreSlim(PeerLimit);
        var activeTasks = new List<Task>();

        while (!token.IsCancellationRequested)
        {
            // Clean up finished tasks
            activeTasks.RemoveAll(t => t.IsCompleted);

            if (peerQueue.TryDequeue(out var peer))
            {
                if (peer == null)
                    continue;

                _queuedPeers.TryRemove(peer.ToString(), out _);
                if (!_connectedPeers.ContainsKey(peer.ToString()))
                {
                    await semaphore.WaitAsync(token).ConfigureAwait(false);
                    UpdatePeerStatus(peer, status =>
                    {
                        status.Status = "Connecting";
                        status.LastUpdatedAt = DateTime.Now;
                        status.LastError = null;
                    });
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await DownloadPeerAsync(peer, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Peer] Consumer task error for {peer}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, token);

                    activeTasks.Add(task);
                }
            }
            else
            {
                await Task.Delay(200, token).ConfigureAwait(false);
            }
        }
    }

    private async Task<IMetadata?> LoadMetadataAsync(Uri source, CancellationToken token)
    {
        Debug.WriteLine($"[BitTorrent] Loading metadata from URI: {source}");
        if (source.IsAbsoluteUri && source.Scheme.Equals("magnet", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return await Task.Run(() => MagnetLink.ResolveToMetadata(source.ToString()), token)
                .ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

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

    private async Task<IMetadata?> TryResolveMagnetMetadataAsync(string magnetUri, CancellationToken token)
    {
        try
        {
            var metadata = MagnetLink.ResolveToMetadata(magnetUri);
            if (metadata != null && metadata.PieceHashes.Count > 0)
                return metadata;
        }
        catch
        {
        }

        return null;
    }

    private void CreateOutputFiles(IReadOnlyCollection<MetadataFileInfo> files)
    {
        var destination = Path.GetFullPath(DestinationFilePath!);
        var isMultiFile = IsMultiFileTorrent(_metadata!);
        var root = isMultiFile ? destination : Path.GetDirectoryName(destination)!;
        Debug.WriteLine(
            $"[BitTorrent] Creating output structure. Destination: {destination}, IsMultiFile: {isMultiFile}");
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
                    try
                    {
                        value = Convert.ChangeType(value, parameter.ParameterType);
                    }
                    catch
                    {
                        value = parameter.HasDefaultValue
                        ? parameter.DefaultValue
                        : Activator.CreateInstance(parameter.ParameterType);
                    }
                }

                args[i] = value;
            }

            try
            {
                return method.Invoke(tracker, args);
            }
            catch (TargetInvocationException)
            {
            }
        }

        throw new MissingMethodException("No compatible bzTorrent tracker Announce overload was found.");
    }

    private async Task DownloadPeerAsync(IPEndPoint endpoint, CancellationToken token)
    {
        var key = endpoint.ToString();
        if (!_connectedPeers.TryAdd(key, 0)) return;
        UpdatePeerStatus(endpoint, status =>
        {
            status.Status = "Connecting";
            status.LastUpdatedAt = DateTime.Now;
        });

        Debug.WriteLine($"[Peer] Connecting to peer: {endpoint}");
        var connection = new PeerWireConnection<TCPSocket> { Timeout = 8 };
        var hashStr = _metadata?.HashString ?? MagnetLink.Resolve(Uri.ToString()).HashString;
        var client = new PeerWireClient(connection)
        {
            Hash = hashStr,
            LocalPeerID = CreatePeerId(),
            KeepConnectionAlive = true
        };
        _clients.Add(client);

        var piecesAvailable = new ConcurrentDictionary<int, byte>();
        var pending = new ConcurrentDictionary<int, byte>();

        client.BitField += (_, _, bitfield) =>
        {
            for (var i = 0; i < bitfield.Length; i++)
                if (bitfield[i])
                    piecesAvailable[i] = 0;
        };
        client.Have += (_, index) => piecesAvailable[index] = 0;
        client.UnChoke += _ =>
        {
            Debug.WriteLine($"[Peer] Unchoked by peer {endpoint}");
            client.SendInterested();
        };
        client.Piece += (_, pieceIndex, begin, data) =>
        {
            try
            {
                if (_metadata == null || pieceIndex < 0 || pieceIndex >= _metadata.PieceHashes.Count ||
                    begin < 0) return;
                var keyBlock = (pieceIndex * 100000) + begin;
                if (!_requestedBlocks.ContainsKey(keyBlock)) return;
                var expected = Math.Min(BlockSize, GetPieceLength(pieceIndex) - begin);
                if (expected <= 0 || data.Length != expected ||
                    begin + data.Length > GetPieceLength(pieceIndex)) return;
                var buffer = _pieceBuffers.GetOrAdd(pieceIndex, _ => new byte[GetPieceLength(pieceIndex)]);
                Buffer.BlockCopy(data, 0, buffer, begin, data.Length);
                var lengths = _blockLengths.GetOrAdd(pieceIndex, _ => new ConcurrentDictionary<int, int>());
                lengths[begin] = data.Length;
                UpdatePeerStatus(endpoint, status =>
                {
                    status.BytesReceived += data.Length;
                    status.LastUpdatedAt = DateTime.Now;
                });
                byte fr;
                pending.TryRemove(keyBlock, out fr);
                _requestedBlocks.TryRemove(keyBlock, out fr);
                var wantedBlocks = (int)Math.Ceiling(GetPieceLength(pieceIndex) / (double)BlockSize);
                if (lengths.Count == wantedBlocks && VerifyPiece(pieceIndex, buffer))
                {
                    Debug.WriteLine($"[Peer] Successfully verified and downloaded piece {pieceIndex} from {endpoint}");
                    lock (_sync) SavePiece(pieceIndex, buffer);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Peer] Error processing piece data from {endpoint}: {ex.Message}");
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        };

        try
        {
            await Task.Run(() =>
            {
                try
                {
                    client.Connect(endpoint);
                    client.Handshake(hashStr, client.LocalPeerID);
                    var timeout = Stopwatch.StartNew();
                    while (!token.IsCancellationRequested && timeout.Elapsed < TimeSpan.FromSeconds(8))
                    {
                        if (!client.Process()) break;
                        if (client.ReceivedHandshake) break;
                        Thread.Sleep(5);
                    }
                }
                catch (Exception ex)
                {
                    UpdatePeerStatus(endpoint, status =>
                    {
                        status.Status = "Error";
                        status.LastError = ex.Message;
                        status.LastUpdatedAt = DateTime.Now;
                    });
                    Debug.WriteLine($"[Peer] Handshake exception with {endpoint}: {ex.Message}");
                }
            }, token).ConfigureAwait(false);

            if (!client.ReceivedHandshake)
            {
                UpdatePeerStatus(endpoint, status =>
                {
                    status.Status = "Unavailable";
                    status.LastError = "Peer did not complete the handshake.";
                    status.LastUpdatedAt = DateTime.Now;
                });
                Debug.WriteLine($"[Peer] Handshake failed or timed out for peer {endpoint}");
                return;
            }

            Debug.WriteLine($"[Peer] Handshake successful with peer {endpoint}. Sending Interested.");
            UpdatePeerStatus(endpoint, status =>
            {
                status.Status = "Connected";
                status.LastUpdatedAt = DateTime.Now;
            });
            client.SendInterested();

            await Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _metadata != null &&
                    _completedPieces.Count < _metadata.PieceHashes.Count &&
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
        catch (Exception ex)
        {
            UpdatePeerStatus(endpoint, status =>
            {
                status.Status = "Error";
                status.LastError = ex.Message;
                status.LastUpdatedAt = DateTime.Now;
            });
            Debug.WriteLine($"[Peer] Communication loop error with {endpoint}: {ex.Message}");
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

            _connectedPeers.TryRemove(key, out _);
            UpdatePeerStatus(endpoint, status =>
            {
                if (status.Status != "Error")
                    status.Status = token.IsCancellationRequested ? "Disconnected" : "Unavailable";
                status.LastUpdatedAt = DateTime.Now;
            });
            Debug.WriteLine($"[Peer] Disconnected from peer {endpoint}");
        }
    }

    private int GetPieceLength(int pieceIndex)
    {
        var start = (long)pieceIndex * _metadata!.PieceSize;
        return (int)Math.Min(_metadata.PieceSize, _totalBytes - start);
    }

    private void ResetConnectionStatuses()
    {
        _trackerStatuses.Clear();
        _peerStatuses.Clear();
        Dispatcher.UIThread.Post(() =>
        {
            Progress.Trackers.Clear();
            Progress.Peers.Clear();
        });
    }

    private TrackerStatus GetOrAddTrackerStatus(string trackerUrl)
    {
        var status = _trackerStatuses.GetOrAdd(trackerUrl, url => new TrackerStatus { Url = url });
        Dispatcher.UIThread.Post(() =>
        {
            if (!Progress.Trackers.Contains(status))
                Progress.Trackers.Add(status);
        });
        return status;
    }

    private void UpdateTrackerStatus(string trackerUrl, Action<TrackerStatus> update)
    {
        var status = GetOrAddTrackerStatus(trackerUrl);
        Dispatcher.UIThread.Post(() => update(status));
    }

    private void RecordPeerDiscovery(IPEndPoint endpoint, string source, ConcurrentQueue<IPEndPoint> peerQueue)
    {
        var key = endpoint.ToString();
        var status = _peerStatuses.GetOrAdd(key, _ => new PeerStatus
        {
            Endpoint = key,
            DiscoverySource = source
        });

        Dispatcher.UIThread.Post(() =>
        {
            if (!Progress.Peers.Contains(status))
                Progress.Peers.Add(status);

            if (!status.DiscoverySource.Contains(source, StringComparison.OrdinalIgnoreCase))
                status.DiscoverySource = $"{status.DiscoverySource}, {source}";
            if (status.Status is not ("Connecting" or "Connected"))
                status.Status = "Discovered";
            status.LastUpdatedAt = DateTime.Now;
        });

        if (!_connectedPeers.ContainsKey(key) && _queuedPeers.TryAdd(key, 0))
            peerQueue.Enqueue(endpoint);
    }

    private PeerStatus GetOrAddPeerStatus(IPEndPoint endpoint)
    {
        var key = endpoint.ToString();
        var status = _peerStatuses.GetOrAdd(key, _ => new PeerStatus
        {
            Endpoint = key,
            DiscoverySource = "Unknown"
        });
        Dispatcher.UIThread.Post(() =>
        {
            if (!Progress.Peers.Contains(status))
                Progress.Peers.Add(status);
        });
        return status;
    }

    private void UpdatePeerStatus(IPEndPoint endpoint, Action<PeerStatus> update)
    {
        var status = GetOrAddPeerStatus(endpoint);
        Dispatcher.UIThread.Post(() => update(status));
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
        _pieceBuffers.TryRemove(pieceIndex, out _);
        _blockLengths.TryRemove(pieceIndex, out _);
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
        Debug.WriteLine(
            $"[Progress] Downloaded: {downloaded}/{_totalBytes} bytes ({Progress.Progress:F2}%) at {Progress.BytesPerSecond:F2} B/s");
    }

    private int GetPieceLengthSafe(int pieceIndex) => GetPieceLength(pieceIndex);

    private static string CreatePeerId()
    {
        return "-SD0100-" + Guid.NewGuid().ToString("N").Substring(0, 12);
    }

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
        Debug.WriteLine("[BitTorrent] Disconnecting all peers and cleaning up client list.");
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

        _clients.Clear();
        _connectedPeers.Clear();
    }

    public void Dispose()
    {
        Debug.WriteLine("[BitTorrent] Dispose called.");
        Stop();
        _cts?.Dispose();
    }

    private sealed class IpEndPointComparer : IEqualityComparer<IPEndPoint>
    {
        public bool Equals(IPEndPoint? x, IPEndPoint? y) => x != null && y != null && x.Equals(y);
        public int GetHashCode(IPEndPoint obj) => obj.GetHashCode();
    }
}