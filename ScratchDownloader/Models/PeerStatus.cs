using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScratchDownloader.Models;

public partial class PeerStatus : ObservableObject
{
    [ObservableProperty] public partial string Endpoint { get; set; } = string.Empty;
    [ObservableProperty] public partial string DiscoverySource { get; set; } = string.Empty;
    [ObservableProperty] public partial string Status { get; set; } = "Discovered";
    [ObservableProperty] public partial long BytesReceived { get; set; }
    [ObservableProperty] public partial DateTime DiscoveredAt { get; set; } = DateTime.Now;
    [ObservableProperty] public partial DateTime LastUpdatedAt { get; set; } = DateTime.Now;
    [ObservableProperty] public partial string? LastError { get; set; }
}
