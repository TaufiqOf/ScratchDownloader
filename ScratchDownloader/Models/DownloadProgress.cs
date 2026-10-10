using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using ScratchDownloader.Localization;

namespace ScratchDownloader.Models;

public partial class DownloadProgress : ViewModelBase
{
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial double BytesPerSecond { get; set; } // Bytes per second
    [ObservableProperty] public partial long TotalBytes { get; set; }
    [ObservableProperty] public partial long BytesDownloaded { get; set; }
    [ObservableProperty] public partial string ProgressText { get; set; } = "0.00%";
    [ObservableProperty] public partial string Speed { get; set; } = "0.00 B/s";
    [JsonIgnore][ObservableProperty] public partial string FileSizeDisplay { get; set; } = "0/0 MB";
    [ObservableProperty] public partial string Eta { get; set; }
    
    [ObservableProperty]
    public partial ConcurrentDictionary<int, SegmentProgress> SegmentProgress { get; set; } = new();

    [JsonIgnore] public ObservableCollection<TrackerStatus> Trackers { get; } = new();
    [JsonIgnore] public ObservableCollection<PeerStatus> Peers { get; } = new();

    public DownloadProgress()
    {
        Trackers.CollectionChanged += OnItemsChanged;
        Peers.CollectionChanged += OnItemsChanged;
    }

    [JsonIgnore] public int TrackerCount => Trackers.Count;
    [JsonIgnore] public int ActiveTrackerCount => Trackers.Count(t => t.Status == "Active");
    [JsonIgnore] public int FailedTrackerCount => Trackers.Count(t => t.Status is "Error" or "Unsupported");
    [JsonIgnore] public int PeerCount => Peers.Count;
    [JsonIgnore] public int ConnectedPeerCount => Peers.Count(p => p.Status == "Connected");
    [JsonIgnore] public int ConnectingPeerCount => Peers.Count(p => p.Status == "Connecting");
    [JsonIgnore] public int UnreachablePeerCount => Peers.Count(p => p.Status is "Unavailable" or "Error");
    [JsonIgnore] public int DisconnectedPeerCount => Peers.Count(p => p.Status == "Disconnected");
    [JsonIgnore] public int DiscoveredPeerCount => Peers.Count(p => p.Status == "Discovered");
    [JsonIgnore] public string PeerBytesReceivedDisplay => ByteSize.FromBytes(Peers.Sum(p => p.BytesReceived)).Humanize("0.00");

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (INotifyPropertyChanged item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
        if (e.NewItems != null)
            foreach (INotifyPropertyChanged item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;
        RefreshSummary();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TrackerStatus.Status) or nameof(PeerStatus.BytesReceived))
            RefreshSummary();
    }

    private void RefreshSummary()
    {
        OnPropertyChanged(nameof(TrackerCount));
        OnPropertyChanged(nameof(ActiveTrackerCount));
        OnPropertyChanged(nameof(FailedTrackerCount));
        OnPropertyChanged(nameof(PeerCount));
        OnPropertyChanged(nameof(ConnectedPeerCount));
        OnPropertyChanged(nameof(ConnectingPeerCount));
        OnPropertyChanged(nameof(UnreachablePeerCount));
        OnPropertyChanged(nameof(DisconnectedPeerCount));
        OnPropertyChanged(nameof(DiscoveredPeerCount));
        OnPropertyChanged(nameof(PeerBytesReceivedDisplay));
    }

    partial void OnProgressChanged(double value)
    {
        ProgressText = $"{value:0.00}%";
    }

    partial void OnBytesPerSecondChanged(double value)
    {
        Speed = ByteSize.FromBytes(value).Humanize("0.00") + "/s";
    }

    partial void OnTotalBytesChanged(long value)
    {
        if (TotalBytes == value)
        {
            FileSizeDisplay = $"{ByteSize.FromBytes(value).Humanize("0.0")}";
        }
        else
        {
            FileSizeDisplay =
                $"{ByteSize.FromBytes(value).Humanize("0.0")}/{ByteSize.FromBytes(TotalBytes).Humanize("0.00")}";
        }
    }

    partial void OnBytesDownloadedChanged(long value)
    {
        if (TotalBytes == value)
        {
            FileSizeDisplay = $"{ByteSize.FromBytes(value).Humanize("0.0")}";
        }
        else
        {
            FileSizeDisplay =
                $"{ByteSize.FromBytes(value).Humanize("0.0")}/{ByteSize.FromBytes(TotalBytes).Humanize("0.00")}";
        }

        UpdateEta();
    }

    private void UpdateEta()
    {
        if (TotalBytes <= 0 || BytesPerSecond <= 0 || Progress >= 100)
        {
            Eta = "--";
            return;
        }

        // Calculate remaining bytes based on actual percent completed
        var remainingBytes = TotalBytes * (1.0 - Progress / 100.0);

        // Compute seconds remaining safely
        var secondsRemaining = remainingBytes / BytesPerSecond;

        if (double.IsNaN(secondsRemaining) || double.IsInfinity(secondsRemaining) || secondsRemaining < 0)
        {
            Eta = "--";
            return;
        }

        if (secondsRemaining < 1)
        {
            Eta = Strings.Get(Language.Time.LessThanOneSecond);
            return;
        }

        Eta = TimeSpan.FromSeconds(secondsRemaining).Humanize();
    }
}