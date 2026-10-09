using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScratchDownloader.Models;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace ScratchDownloader.Services;

public class YoutubeDownloadService : IDownloadService
{
    private const int BufferSize = 81920;
    private readonly YoutubeClient _youtubeClient = new();
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private static string FfmpegExecutableName = "./ffmpeg/{0}/ffmpeg";
    public DownloadProgress Progress { get; set; } = new();
    public double CapSpeed { get; set; }
    public int SegmentCount { get; set; } = 1;
    public Uri Uri { get; set; } = null!;
    public string? DestinationFilePath { get; set; }
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Completed;
    public event EventHandler? Initializing;
    public event EventHandler? Downloading;
    public event EventHandler? Processing;

    public YoutubeDownloadService()
    {
        if (OperatingSystem.IsWindows())
            FfmpegExecutableName = "./ffmpeg/windows/ffmpeg.exe";
        else if (OperatingSystem.IsLinux())
            FfmpegExecutableName = "./ffmpeg/linux/ffmpeg";
        else if (OperatingSystem.IsMacOS())
            FfmpegExecutableName = "./ffmpeg/macos/ffmpeg";
    }
    public async Task<FileDataInformation> GetFileDataInformation(string uri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("A YouTube video URL is required.", nameof(uri));

        var normalizedUri = uri.Trim();
        if (!Uri.TryCreate(normalizedUri, UriKind.Absolute, out var source) || !IsYoutubeHost(source.Host))
            throw new ArgumentException("A YouTube video URL is required.", nameof(uri));

        var videoId = VideoId.Parse(normalizedUri);
        var (video, plan) = await ResolveVideoAsync(videoId, cancellationToken).ConfigureAwait(false);
        var extension = plan.Container.Name;
        var fileName = $"{SanitizeFileName(video.Title)}.{extension}";
        var finalUri = new Uri(video.Url);

        return new FileDataInformation
        {
            DownloadType = DownloadType.Youtube,
            Uri = finalUri,
            FinalUri = finalUri,
            FileName = fileName,
            FileSizeBytes = plan.TotalBytes,
            FileExtension = extension.ToUpperInvariant()
        };
    }

    public bool CanHandle(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var normalizedUrl = url.Trim();
        var isYoutubeHost = Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri)
                            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                            && IsYoutubeHost(uri.Host)
                            && VideoId.TryParse(normalizedUrl) != null;
        return isYoutubeHost;
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        StartDownload(cancellationToken, resumeExisting: false);
    }

    public void Resume(CancellationToken cancellationToken = default)
    {
        StartDownload(cancellationToken, resumeExisting: true);
    }

    public void Pause()
    {
        _cts?.Cancel();
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private void StartDownload(CancellationToken cancellationToken, bool resumeExisting)
    {
        lock (_sync)
        {
            var previousCts = _cts;
            var previousWorker = _worker;
            previousCts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cts.Token;

            _worker = Task.Run(async () =>
            {
                if (previousWorker != null)
                    await previousWorker.ConfigureAwait(false);

                previousCts?.Dispose();
                await RunAsync(token, resumeExisting).ConfigureAwait(false);
            });
        }
    }

    private async Task RunAsync(CancellationToken token, bool resumeExisting)
    {
        try
        {
            ValidateInputs();
            token.ThrowIfCancellationRequested();
            Initializing?.Invoke(this, EventArgs.Empty);

            var videoId = VideoId.Parse(Uri.ToString());
            var (_, plan) = await ResolveVideoAsync(videoId, token).ConfigureAwait(false);
            var destinationPath = Path.GetFullPath(DestinationFilePath!);
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var targets = plan.Streams.Count == 1
                ? new List<string> { destinationPath }
                : new List<string> { $"{destinationPath}.video.part", $"{destinationPath}.audio.part" };
            var existing = targets.Sum(t => resumeExisting && File.Exists(t) ? new FileInfo(t).Length : 0);

            var state = new TransferState { TotalBytes = plan.TotalBytes, LastBytes = existing };
            InitializeProgress(plan.TotalBytes, existing);
            Downloading?.Invoke(this, EventArgs.Empty);

            for (var i = 0; i < plan.Streams.Count; i++)
            {
                await DownloadStreamAsync(plan.Streams[i], targets[i], resumeExisting, state, token)
                    .ConfigureAwait(false);
            }
            Processing?.Invoke(this, EventArgs.Empty);
            if (plan.Streams.Count > 1)
            {
                await MuxAsync(targets[0], targets[1], destinationPath, plan.Container, token).ConfigureAwait(false);
                foreach (var target in targets)
                    File.Delete(target);
            }

            var totalBytes = state.Downloaded;
            UpdateProgress(totalBytes, totalBytes, 0, 0);
            Progress.BytesDownloaded = totalBytes;
            Progress.TotalBytes = totalBytes;
            Progress.Progress = 100;
            Progress.BytesPerSecond = 0;
            foreach (var segment in Progress.SegmentProgress.Values)
            {
                segment.TotalBytes = totalBytes;
                segment.Progress = 100;
            }

            Completed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private sealed record DownloadPlan(IReadOnlyList<IStreamInfo> Streams, Container Container)
    {
        public long TotalBytes => Streams.Sum(s => s.Size.Bytes);
    }

    private sealed class TransferState
    {
        public Stopwatch Watch { get; } = Stopwatch.StartNew();
        public TimeSpan LastUpdate { get; set; }
        public long LastBytes { get; set; }
        public long TotalBytes { get; init; }
        public long Downloaded { get; set; }
        public long SessionBytes { get; set; }
    }

    private async Task<(Video Video, DownloadPlan Plan)> ResolveVideoAsync(
        VideoId videoId,
        CancellationToken cancellationToken)
    {
        try
        {
            var video = await _youtubeClient.Videos.GetAsync(videoId, cancellationToken).ConfigureAwait(false);
            var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId, cancellationToken)
                .ConfigureAwait(false);

            if (await IsFfmpegAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                var videoOnly = manifest.GetVideoOnlyStreams().ToList();
                var audioOnly = manifest.GetAudioOnlyStreams().ToList();
                foreach (var container in new[] { Container.Mp4, Container.WebM })
                {
                    var videos = videoOnly.Where(v => v.Container == container).ToList();
                    var audios = audioOnly.Where(a => a.Container == container).ToList();
                    if (videos.Count == 0 || audios.Count == 0)
                        continue;

                    return (video, new DownloadPlan(
                        new IStreamInfo[] { videos.GetWithHighestVideoQuality(), audios.GetWithHighestBitrate() },
                        container));
                }
            }

            var muxedStreams = manifest.GetMuxedStreams().ToList();
            if (muxedStreams.Count == 0)
                throw new NotSupportedException(
                    "This YouTube video only offers separate audio and video streams, which require ffmpeg to merge. Install ffmpeg and make it available on PATH.");

            var muxed = muxedStreams.GetWithHighestVideoQuality();
            return (video, new DownloadPlan(new IStreamInfo[] { muxed }, muxed.Container));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static bool? _ffmpegAvailable;

    private static async Task<bool> IsFfmpegAvailableAsync(CancellationToken token)
    {
        if (_ffmpegAvailable.HasValue)
            return _ffmpegAvailable.Value;

        try
        {
            using var process = Process.Start(new ProcessStartInfo(FfmpegExecutableName, "-version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null)
                return false;

            await process.WaitForExitAsync(token).ConfigureAwait(false);
            _ffmpegAvailable = process.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            _ffmpegAvailable = false;
        }

        return _ffmpegAvailable.Value;
    }

    private async Task DownloadStreamAsync(IStreamInfo streamInfo, string path, bool resumeExisting,
        TransferState state, CancellationToken token)
    {
        var size = streamInfo.Size.Bytes;
        var startingOffset = resumeExisting && File.Exists(path) ? new FileInfo(path).Length : 0;
        if (size > 0 && startingOffset > size)
            throw new InvalidDataException("The existing partial file is larger than the YouTube stream.");

        if (size > 0 && startingOffset == size)
        {
            state.Downloaded += size;
            return;
        }

        await using var input = await _youtubeClient.Videos.Streams.GetAsync(streamInfo, token)
            .ConfigureAwait(false);
        await using var output = new FileStream(
            path,
            resumeExisting ? FileMode.OpenOrCreate : FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            BufferSize,
            useAsync: true);
        output.Seek(startingOffset, SeekOrigin.Begin);

        var buffer = new byte[BufferSize];
        long skipped = 0;
        while (skipped < startingOffset)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, startingOffset - skipped);
            var n = await input.ReadAsync(buffer.AsMemory(0, bytesToRead), token).ConfigureAwait(false);
            if (n == 0)
                throw new EndOfStreamException(
                    "The YouTube stream ended before the saved partial file could be resumed.");

            skipped += n;
            state.SessionBytes += n;
            await ApplySpeedLimitAsync(state, token).ConfigureAwait(false);
        }

        state.Downloaded += startingOffset;
        var received = startingOffset;
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
            received += read;
            state.Downloaded += read;
            state.SessionBytes += read;

            await ApplySpeedLimitAsync(state, token).ConfigureAwait(false);

            if (state.Watch.Elapsed - state.LastUpdate >= TimeSpan.FromMilliseconds(200))
                ReportProgress(state);
        }

        if (size > 0 && received != size)
            throw new IOException($"YouTube stream ended early: received {received} of {size} bytes.");

        ReportProgress(state);
    }

    private void ReportProgress(TransferState state)
    {
        var elapsed = (state.Watch.Elapsed - state.LastUpdate).TotalSeconds;
        UpdateProgress(state.Downloaded, state.TotalBytes, state.Downloaded - state.LastBytes, elapsed);
        state.LastBytes = state.Downloaded;
        state.LastUpdate = state.Watch.Elapsed;
    }

    private static async Task MuxAsync(string videoPath, string audioPath, string outputPath, Container container,
        CancellationToken token)
    {
        var tempOutput = $"{outputPath}.muxing";
        var startInfo = new ProcessStartInfo(FfmpegExecutableName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in new[]
                 {
                     "-y", "-i", videoPath, "-i", audioPath, "-map", "0:v:0", "-map", "1:a:0", "-c", "copy",
                     "-f", container.Name, tempOutput
                 })
            startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("Unable to start ffmpeg.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { }
            if (File.Exists(tempOutput)) File.Delete(tempOutput);
            throw;
        }

        await stdout.ConfigureAwait(false);
        var error = await stderr.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            if (File.Exists(tempOutput)) File.Delete(tempOutput);
            throw new IOException($"ffmpeg failed to merge audio and video: {error.Trim()}");
        }

        File.Move(tempOutput, outputPath, overwrite: true);
    }

    private void InitializeProgress(long totalBytes, long startingOffset)
    {
        Progress.SegmentProgress = new ConcurrentDictionary<int, SegmentProgress>
        {
            [0] = new SegmentProgress
            {
                Index = 0,
                BytesDownloaded = startingOffset,
                TotalBytes = totalBytes,
                Progress = totalBytes > 0 ? startingOffset * 100d / totalBytes : 0,
                BytesPerSecond = 0
            }
        };
        Progress.BytesDownloaded = startingOffset;
        Progress.TotalBytes = totalBytes;
        Progress.Progress = totalBytes > 0 ? startingOffset * 100d / totalBytes : 0;
        Progress.BytesPerSecond = 0;
    }

    private async Task ApplySpeedLimitAsync(TransferState state, CancellationToken token)
    {
        if (CapSpeed <= 0)
            return;

        var targetDuration = TimeSpan.FromSeconds(state.SessionBytes / CapSpeed);
        var remaining = targetDuration - state.Watch.Elapsed;
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, token).ConfigureAwait(false);
    }

    private void UpdateProgress(long downloaded, long totalBytes, long bytesSinceUpdate, double elapsedSeconds)
    {
        var progress = totalBytes > 0 ? Math.Min(100, downloaded * 100d / totalBytes) : 0;
        var speed = elapsedSeconds > 0 ? bytesSinceUpdate / elapsedSeconds : 0;
        Progress.BytesDownloaded = downloaded;
        Progress.TotalBytes = totalBytes;
        Progress.Progress = progress;
        Progress.BytesPerSecond = speed;
        if (!Progress.SegmentProgress.TryGetValue(0, out var segment))
            return;

        segment.BytesDownloaded = downloaded;
        segment.TotalBytes = totalBytes;
        segment.Progress = progress;
        segment.BytesPerSecond = speed;
    }

    private void ValidateInputs()
    {
        if (Uri == null)
            throw new InvalidOperationException("Uri must be set before starting.");
        if (string.IsNullOrWhiteSpace(DestinationFilePath))
            throw new InvalidOperationException("DestinationFilePath must be set.");
    }

    private static bool IsYoutubeHost(string host)
    {
        return host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
               || host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
               || host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase)
               || host.Equals("youtube-nocookie.com", StringComparison.OrdinalIgnoreCase)
               || host.EndsWith(".youtube-nocookie.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars()
            .Concat(new[] { '/', '\\' })
            .ToHashSet();
        var sanitized = new string(fileName.Select(character =>
            invalidCharacters.Contains(character) || char.IsControl(character) ? '_' : character).ToArray())
            .Trim()
            .TrimEnd('.');
        return string.IsNullOrWhiteSpace(sanitized) ? "youtube-video" : sanitized;
    }
}
