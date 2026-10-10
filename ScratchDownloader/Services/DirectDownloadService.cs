using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ScratchDownloader.Localization;
using ScratchDownloader.Models;
using Timer = System.Timers.Timer;

namespace ScratchDownloader.Services;

public class DirectDownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;
    private double _lastProgressReported;
    private const int BufferSize = 81920; // 80 KB buffer size for reading/writing
    readonly Timer _progressTimer = new Timer(1000); // 1 second interval
    private string MetadataFilePath => $"{DestinationFilePath}.meta.json";

    public double CapSpeed { get; set; } = 0; // 0 means no cap
    public DownloadProgress Progress { get; set; } = new();
    public int SegmentCount { get; set; } = 4;
    public Uri? Uri { get; set; }
    public string? DestinationFilePath { get; set; }
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Completed;
    public event EventHandler? Initializing;
    public event EventHandler? Downloading;
    public event EventHandler? Processing;


    public DirectDownloadService()
    {
        _httpClient = new HttpClient();
        _progressTimer.Elapsed += (sender, args) =>
        {
            _progressTimer.Stop();
            foreach (var segmentProgress in Progress.SegmentProgress.Values)
            {
                if (segmentProgress.SpeedTimer.ElapsedMilliseconds > 1000)
                {
                    segmentProgress.BytesPerSecond = 0;
                    segmentProgress.LastReportedMs = 0;
                    segmentProgress.SpeedTimer.Restart();
                }
            }

            if (Progress.BytesPerSecond >= 0)
            {
                Progress.BytesPerSecond = Progress.SegmentProgress.Values.Sum(s => s.BytesPerSecond);
            }

            _progressTimer.Start();
        };
        _progressTimer.Start();
    }

    public async Task<FileDataInformation> GetFileDataInformation(string uri,
        CancellationToken cancellationToken = default)
    {
        var data = new FileDataInformation();
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            uri);
        HttpClient _httpClient = new();

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var finalUri = response.RequestMessage?.RequestUri ?? new Uri(uri);
        var fileName = GetFileName(response, finalUri);
        var fileExtension = GetExtension(fileName);

        long fileSizeBytes = 0;

        // Extract content length header
        if (response.Content.Headers.ContentLength.HasValue)
        {
            var bytes = response.Content.Headers.ContentLength.Value;
            fileSizeBytes = bytes;
        }
        else
        {
            fileSizeBytes = 0;
        }

        data.Uri = finalUri;
        data.FinalUri = finalUri;
        data.FileName = fileName;
        data.FileSizeBytes = fileSizeBytes;
        data.FileExtension = fileExtension;
        data.DownloadType = DownloadType.Direct;
        return data;
    }

    public bool CanHandle(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        if(url.Contains("youtube.com") || url.Contains("youtu.be"))
            return false;
        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
               && !uri.AbsolutePath.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        Task.Factory.StartNew(() => StartAsync(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void Resume(CancellationToken cancellationToken = default)
    {
        _ = ResumeAsync(cancellationToken);
    }

    public void Pause()
    {
        _cts?.Cancel();
    }

    public void Stop()
    {
        _cts?.Cancel();
        if (File.Exists(MetadataFilePath))
            File.Delete(MetadataFilePath);
    }

    private async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateInputs();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var directory = Path.GetDirectoryName(DestinationFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            DownloadMetadata metadata;
            Initializing?.Invoke(this, EventArgs.Empty);
            if (File.Exists(MetadataFilePath))
            {
                metadata = LoadMetadata();
            }
            else
            {
                if (File.Exists(DestinationFilePath)) File.Delete(DestinationFilePath);

                metadata = await InitializeMetadataAsync(_cts.Token);
            }

            Downloading?.Invoke(this, EventArgs.Empty);
            await ProcessDownloadAsync(metadata, _cts.Token);
        }
        catch (Exception e)
        {
            ErrorOccurred?.Invoke(this, e.Message);
        }
    }

    private async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(MetadataFilePath))
                throw new FileNotFoundException("No saved state found to resume from.", MetadataFilePath);

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var metadata = LoadMetadata();
            await ProcessDownloadAsync(metadata, _cts.Token);
        }
        catch (Exception e)
        {
            ErrorOccurred?.Invoke(this, e.Message);
        }
    }

    private async Task ProcessDownloadAsync(DownloadMetadata metadata, CancellationToken token)
    {
        var segmentProgressMap = new ConcurrentDictionary<int, SegmentProgress>();
        Progress.Progress = _lastProgressReported;
        Progress.BytesPerSecond = 0;


        // Pre-fill existing segment states
        foreach (var seg in metadata.Segments)
        {
            var segTotal = seg.EndByte - seg.StartByte + 1;
            var initialPercent = segTotal > 0 ? (double)seg.BytesDownloaded / segTotal * 100 : 0;

            segmentProgressMap[seg.Index] = new SegmentProgress
            {
                Index = seg.Index,
                Progress = initialPercent,
                BytesDownloaded = seg.BytesDownloaded,
                TotalBytes = segTotal,
                BytesPerSecond = 0
            };
        }

        Progress.SegmentProgress = segmentProgressMap;
        var overallStopwatch = Stopwatch.StartNew();
        var initialTotalBytes = metadata.Segments.Sum(s => s.BytesDownloaded);

        var tasks = metadata.Segments.Select(segment => Task.Run(async () =>
        {
            await DownloadSegmentAsync(
                segment,
                metadata,
                overallStopwatch,
                initialTotalBytes,
                token);
        }, token));

        try
        {
            await Task.WhenAll(tasks);
            Progress.SegmentProgress.Values.ToList().ForEach(s =>
            {
                s.Progress = 100;
                s.BytesPerSecond = 0;
            });
            Completed?.Invoke(this, EventArgs.Empty);
            if (File.Exists(MetadataFilePath)) File.Delete(MetadataFilePath);
        }
        catch (OperationCanceledException)
        {
            //throw;
        }
        catch (Exception e)
        {
            ErrorOccurred?.Invoke(this, e.Message);
        }
    }

    private async Task DownloadSegmentAsync(
        SegmentState segment,
        DownloadMetadata metadata,
        Stopwatch overallStopwatch,
        long initialTotalBytesRead,
        CancellationToken token)
    {
        var segmentStopwatch = Stopwatch.StartNew();
        long sessionBytesDownloadedForSegment = 0;
        long totalSegmentBytes = segment.EndByte - segment.StartByte + 1;

        try
        {
            var currentStart = segment.StartByte + segment.BytesDownloaded;

            // Skip completed segments
            if (currentStart > segment.EndByte)
                return;

            var request = new HttpRequestMessage(HttpMethod.Get, Uri);
            request.Headers.Range = new RangeHeaderValue(currentStart, segment.EndByte);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(token);

            using var fileStream = new FileStream(
                DestinationFilePath!,
                FileMode.Open,
                FileAccess.Write,
                FileShare.ReadWrite,
                BufferSize,
                true);

            fileStream.Seek(currentStart, SeekOrigin.Begin);

            var buffer = new byte[BufferSize];
            int read;

            // Track time for chunk throttling
            var chunkStopwatch = Stopwatch.StartNew();

            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read, token);

                segment.BytesDownloaded += read;
                sessionBytesDownloadedForSegment += read;
                SaveMetadata(metadata);

                // --- SPEED CAP LIMITING ---
                if (CapSpeed > 0)
                {
                    // Share the total cap speed equally among active/total segments
                    double segmentCapSpeed = CapSpeed / metadata.Segments.Count;

                    // Expected time in milliseconds to download 'read' bytes at 'segmentCapSpeed'
                    double expectedMs = (read / segmentCapSpeed) * 1000.0;
                    double elapsedMs = chunkStopwatch.Elapsed.TotalMilliseconds;

                    if (elapsedMs < expectedMs)
                    {
                        int delayMs = (int)(expectedMs - elapsedMs);
                        if (delayMs > 0)
                        {
                            await Task.Delay(delayMs, token);
                        }
                    }

                    chunkStopwatch.Restart();
                }

                // Update progress after 100 milliseconds
                var segmentProgress = Progress.SegmentProgress[segment.Index];
                if (segmentStopwatch.ElapsedMilliseconds - segmentProgress.LastReportedMs >= 100)
                {
                    UpdateProgress(
                        segment,
                        metadata,
                        overallStopwatch,
                        initialTotalBytesRead,
                        segmentStopwatch,
                        sessionBytesDownloadedForSegment,
                        totalSegmentBytes);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            UpdateProgress(
                segment,
                metadata,
                overallStopwatch,
                initialTotalBytesRead,
                segmentStopwatch,
                sessionBytesDownloadedForSegment,
                totalSegmentBytes);
        }
    }

    private void UpdateProgress(SegmentState segment, DownloadMetadata metadata, Stopwatch overallStopwatch,
        long initialTotalBytesRead, Stopwatch segmentStopwatch, long sessionBytesDownloadedForSegment,
        long totalSegmentBytes)
    {
        // Calculate segment speed
        var segElapsedSec = segmentStopwatch.Elapsed.TotalSeconds;
        var segSpeed = segElapsedSec > 0 ? sessionBytesDownloadedForSegment / segElapsedSec : 0;
        var segPercent = (double)segment.BytesDownloaded / totalSegmentBytes * 100;
        var currentSegProgress = Progress.SegmentProgress[segment.Index];
        currentSegProgress.LastReportedMs = currentSegProgress.SpeedTimer.ElapsedMilliseconds;
        currentSegProgress.Index = segment.Index;
        currentSegProgress.Progress = segPercent;
        currentSegProgress.BytesDownloaded = segment.BytesDownloaded;
        currentSegProgress.TotalBytes = totalSegmentBytes;
        currentSegProgress.BytesPerSecond = segSpeed;
        currentSegProgress.SpeedTimer.Restart();
        // Calculate overall progress & speed
        var currentTotalDownloaded = Progress.SegmentProgress.Values.Sum(s => s.BytesDownloaded);
        var overallPercent =
            metadata.TotalBytes > 0 ? (double)currentTotalDownloaded / metadata.TotalBytes * 100 : 0;

        var sessionTotalDownloaded = currentTotalDownloaded - initialTotalBytesRead;
        var overallElapsedSec = overallStopwatch.Elapsed.TotalSeconds;
        var overallSpeed = overallElapsedSec > 0 ? sessionTotalDownloaded / overallElapsedSec : 0;

        _lastProgressReported = Math.Max(_lastProgressReported, overallPercent);
        Progress.Progress = overallPercent;
        Progress.BytesPerSecond = overallSpeed;
        Progress.TotalBytes = metadata.TotalBytes;
        Progress.BytesDownloaded = currentTotalDownloaded;
    }

    private async Task<DownloadMetadata> InitializeMetadataAsync(CancellationToken token)
    {
        try
        {
            using var headResponse = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, Uri), token);
            headResponse.EnsureSuccessStatusCode();

            var totalBytes = headResponse.Content.Headers.ContentLength
                             ?? throw new InvalidOperationException("Server did not return Content-Length.");

            var supportsRanges = headResponse.Headers.AcceptRanges.Contains("bytes");

            using (var fs = new FileStream(DestinationFilePath!, FileMode.Create, FileAccess.Write,
                       FileShare.ReadWrite))
            {
                fs.SetLength(totalBytes);
            }

            var effectiveSegments = supportsRanges ? Math.Max(1, SegmentCount) : 1;
            var segmentSize = totalBytes / effectiveSegments;

            var metadata = new DownloadMetadata
            {
                Url = Uri!.ToString(),
                TotalBytes = totalBytes
            };

            for (var i = 0; i < effectiveSegments; i++)
            {
                var startByte = i * segmentSize;
                var endByte = i == effectiveSegments - 1 ? totalBytes - 1 : startByte + segmentSize - 1;

                metadata.Segments.Add(new SegmentState
                {
                    Index = i,
                    StartByte = startByte,
                    EndByte = endByte,
                    BytesDownloaded = 0
                });
            }

            SaveMetadata(metadata);
            return metadata;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void SaveMetadata(DownloadMetadata metadata)
    {
        lock (this)
        {
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MetadataFilePath, json);
        }
    }

    private DownloadMetadata LoadMetadata()
    {
        var json = File.ReadAllText(MetadataFilePath);
        return JsonSerializer.Deserialize<DownloadMetadata>(json)
               ?? throw new InvalidDataException("Metadata file is invalid.");
    }

    private void ValidateInputs()
    {
        if (Uri == null) throw new InvalidOperationException("Uri must be set before starting.");
        if (string.IsNullOrWhiteSpace(DestinationFilePath))
            throw new InvalidOperationException("DestinationFilePath must be set.");
    }

    private static string GetFileName(
        HttpResponseMessage response,
        Uri uri)
    {
        var contentDisposition =
            response.Content.Headers.ContentDisposition;

        var name = contentDisposition?.FileNameStar ??
                   contentDisposition?.FileName;

        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim('"');

        var pathName = Path.GetFileName(uri.LocalPath);

        if (!string.IsNullOrWhiteSpace(pathName))
            return pathName;

        return Strings.Get(Language.FilePropertiesTabControl.DownloadFileName);
    }

    private static string GetExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
            return Strings.Get(Language.FilePropertiesTabControl.FileType);

        return extension.TrimStart('.').ToUpperInvariant();
    }
}