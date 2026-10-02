namespace ScratchDownloader.Models;

public enum DownloadStatus
{
    Initializing,
    Queued,
    Downloading,
    Paused,
    Stopped,
    Completed,
    Failed
}