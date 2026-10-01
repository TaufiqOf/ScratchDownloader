using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ScratchDownloader.Models;

namespace ScratchDownloader.Services;

public class DirectDownloadService : IDownloadService
{
    public DownloadProgress Progress { get; set; } = new DownloadProgress();

    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;

    public event EventHandler? Completed;
    public event EventHandler? Initializing;
    public event EventHandler? Downloading;

    public int SegmentCount { get; set; } = 4;
    public Uri? Uri { get; set; }
    public string? DestinationFilePath { get; set; }

    private string MetadataFilePath => $"{DestinationFilePath}.meta.json";


    public DirectDownloadService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ValidateInputs();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        string? directory = Path.GetDirectoryName(DestinationFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DownloadMetadata metadata;
        Initializing?.Invoke(this, EventArgs.Empty);
        if (File.Exists(MetadataFilePath))
        {
            metadata = LoadMetadata();
        }
        else
        {
            if (File.Exists(DestinationFilePath))
            {
                File.Delete(DestinationFilePath);
            }

            metadata = await InitializeMetadataAsync(_cts.Token);
        }

        Downloading?.Invoke(this, EventArgs.Empty);
        await ProcessDownloadAsync(metadata, _cts.Token);
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(MetadataFilePath))
        {
            throw new FileNotFoundException("No saved state found to resume from.", MetadataFilePath);
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        DownloadMetadata metadata = LoadMetadata();
        await ProcessDownloadAsync(metadata, _cts.Token);
    }

    private async Task ProcessDownloadAsync(DownloadMetadata metadata, CancellationToken token)
    {
        var segmentProgressMap = new ConcurrentDictionary<int, SegmentProgress>();
        Progress.Progress = _lastProgressReported;
        Progress.BytesPerSecond = 0;


        // Pre-fill existing segment states
        foreach (var seg in metadata.Segments)
        {
            long segTotal = seg.EndByte - seg.StartByte + 1;
            double initialPercent = segTotal > 0 ? (double)seg.BytesDownloaded / segTotal * 100 : 0;

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
        long initialTotalBytes = metadata.Segments.Sum(s => s.BytesDownloaded);

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
            Completed?.Invoke(this, EventArgs.Empty);
            if (File.Exists(MetadataFilePath))
            {
                File.Delete(MetadataFilePath);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    private double _lastProgressReported = 0;

    private async Task DownloadSegmentAsync(
        SegmentState segment,
        DownloadMetadata metadata,
        Stopwatch overallStopwatch,
        long initialTotalBytesRead,
        CancellationToken token)
    {
        try
        {
            long currentStart = segment.StartByte + segment.BytesDownloaded;
            long totalSegmentBytes = segment.EndByte - segment.StartByte + 1;

            // Skip completed segments
            if (currentStart > segment.EndByte)
                return;

            var request = new HttpRequestMessage(HttpMethod.Get, Uri);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(currentStart, segment.EndByte);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(token);

            using var fileStream = new FileStream(
                DestinationFilePath!,
                FileMode.Open,
                FileAccess.Write,
                FileShare.ReadWrite,
                bufferSize: 81920,
                useAsync: true);

            fileStream.Seek(currentStart, SeekOrigin.Begin);

            byte[] buffer = new byte[81920];
            int read;

            var segmentStopwatch = Stopwatch.StartNew();
            long sessionBytesDownloadedForSegment = 0;

            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read, token);

                segment.BytesDownloaded += read;
                sessionBytesDownloadedForSegment += read;

                // Calculate segment speed
                double segElapsedSec = segmentStopwatch.Elapsed.TotalSeconds;
                double segSpeed = segElapsedSec > 0 ? sessionBytesDownloadedForSegment / segElapsedSec : 0;
                double segPercent = (double)segment.BytesDownloaded / totalSegmentBytes * 100;

                var currentSegProgress = Progress.SegmentProgress[segment.Index];
                currentSegProgress.Index = segment.Index;
                currentSegProgress.Progress = segPercent;
                currentSegProgress.BytesDownloaded = segment.BytesDownloaded;
                currentSegProgress.TotalBytes = totalSegmentBytes;
                currentSegProgress.BytesPerSecond = segSpeed;


                // Calculate overall progress & speed
                long currentTotalDownloaded = Progress.SegmentProgress.Values.Sum(s => s.BytesDownloaded);
                double overallPercent =
                    metadata.TotalBytes > 0 ? (double)currentTotalDownloaded / metadata.TotalBytes * 100 : 0;

                long sessionTotalDownloaded = currentTotalDownloaded - initialTotalBytesRead;
                double overallElapsedSec = overallStopwatch.Elapsed.TotalSeconds;
                double overallSpeed = overallElapsedSec > 0 ? sessionTotalDownloaded / overallElapsedSec : 0;

                SaveMetadata(metadata);
                _lastProgressReported = Math.Max(_lastProgressReported, overallPercent);
                Progress.Progress = overallPercent;
                Progress.BytesPerSecond = overallSpeed;
                Progress.TotalBytes = metadata.TotalBytes;
                Progress.BytesDownloaded = currentTotalDownloaded;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<DownloadMetadata> InitializeMetadataAsync(CancellationToken token)
    {
        try
        {
            using var headResponse = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, Uri), token);
            headResponse.EnsureSuccessStatusCode();

            long totalBytes = headResponse.Content.Headers.ContentLength
                              ?? throw new InvalidOperationException("Server did not return Content-Length.");

            bool supportsRanges = headResponse.Headers.AcceptRanges.Contains("bytes");

            using (var fs = new FileStream(DestinationFilePath!, FileMode.Create, FileAccess.Write,
                       FileShare.ReadWrite))
            {
                fs.SetLength(totalBytes);
            }

            int effectiveSegments = supportsRanges ? Math.Max(1, SegmentCount) : 1;
            long segmentSize = totalBytes / effectiveSegments;

            var metadata = new DownloadMetadata
            {
                Url = Uri!.ToString(),
                TotalBytes = totalBytes
            };

            for (int i = 0; i < effectiveSegments; i++)
            {
                long startByte = i * segmentSize;
                long endByte = (i == effectiveSegments - 1) ? totalBytes - 1 : (startByte + segmentSize - 1);

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
            string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MetadataFilePath, json);
        }
    }

    private DownloadMetadata LoadMetadata()
    {
        string json = File.ReadAllText(MetadataFilePath);
        return JsonSerializer.Deserialize<DownloadMetadata>(json)
               ?? throw new InvalidDataException("Metadata file is invalid.");
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        Task.Factory.StartNew(() => StartAsync(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void Pause()
    {
        _cts?.Cancel();
    }

    public void Resume(CancellationToken cancellationToken = default)
    {
        _ = ResumeAsync(cancellationToken);
    }

    public void Stop()
    {
        _cts?.Cancel();
        if (File.Exists(MetadataFilePath))
            File.Delete(MetadataFilePath);
    }

    private void ValidateInputs()
    {
        if (Uri == null) throw new InvalidOperationException("Uri must be set before starting.");
        if (string.IsNullOrWhiteSpace(DestinationFilePath))
            throw new InvalidOperationException("DestinationFilePath must be set.");
    }
}