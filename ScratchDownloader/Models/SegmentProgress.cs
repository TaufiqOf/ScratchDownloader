using System;
using System.Diagnostics;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScratchDownloader.Models;

public partial class SegmentProgress : ViewModelBase
{
    [ObservableProperty] public partial int Index { get; set; }
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial long BytesDownloaded { get; set; }
    [ObservableProperty] public partial long TotalBytes { get; set; }
    [ObservableProperty] public partial double BytesPerSecond { get; set; } // Bytes per second
    public Stopwatch SpeedTimer { get; set; } = new Stopwatch(); // 1 second interval
    public long LastReportedMs { get; set; }
    public TimeSpan LastUpdate { get; set; }
}