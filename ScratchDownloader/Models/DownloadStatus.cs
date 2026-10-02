namespace ScratchDownloader.Models;

public enum DownloadStatus
{
    Initializing=0,
    Queued=1,
    Downloading=2,
    Paused=3,
    Stopped=4,
    Completed=5,
    Failed=6
}