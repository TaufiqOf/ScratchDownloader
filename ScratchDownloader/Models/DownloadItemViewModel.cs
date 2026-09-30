using CommunityToolkit.Mvvm.ComponentModel;

namespace ScratchDownloader.Models;

public abstract partial class DownloadItemViewModel : ObservableObject
{
    [ObservableProperty] public partial string FileName { get; set; } = string.Empty;

    [ObservableProperty] public partial string Url { get; set; } = string.Empty;

    [ObservableProperty] public partial string Status { get; set; } = "Queued";

    [ObservableProperty] public partial double Progress { get; set; }

    [ObservableProperty] public partial string ProgressText { get; set; } = "0%";

    [ObservableProperty] public partial string Speed { get; set; } = "0 MB/s";

    [ObservableProperty] public partial double SpeedValue { get; set; }

    [ObservableProperty] public partial string Size { get; set; } = "0 MB";

    [ObservableProperty] public partial string Eta { get; set; } = "--";

    [ObservableProperty] public partial long DownloadedBytes { get; set; }

    public void Resume()
    {
        Status = "Downloading";
    }

    public void Stop()
    {
        Status = "Stopped";
    }

    public void Pause()
    {
        Status = "Paused";
    }
}