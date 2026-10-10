using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScratchDownloader.Models;

public partial class TrackerStatus : ObservableObject
{
    [ObservableProperty] public partial string Url { get; set; } = string.Empty;
    [ObservableProperty] public partial string Status { get; set; } = "Waiting";
    [ObservableProperty] public partial int PeersDiscovered { get; set; }
    [ObservableProperty] public partial DateTime? LastAnnouncedAt { get; set; }
    [ObservableProperty] public partial string? LastError { get; set; }
}
