using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScratchDownloader.Models;

public enum QueueDaysOfWeek
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}

public enum OperationMode
{
    Nothing,
    Notify,
    Sleep,
    Shutdown,
    RunScript
}

public partial class Queue : ObservableObject
{
    [ObservableProperty] public partial string Id { get; set; }
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial int MaxConcurrentDownloads { get; set; } = 2;
    [ObservableProperty] public partial int Segments { get; set; } = 8;
    [ObservableProperty] public partial double MaxSpeedLimit { get; set; } = 0; // 0 means no limit
    [ObservableProperty] public partial TimeSpan? StartTime { get; set; }
    [ObservableProperty] public partial List<QueueDaysOfWeek> DaysOfWeek { get; set; } = new();
    [ObservableProperty] public partial OperationMode AfterComplete { get; set; } = OperationMode.Nothing;
    [ObservableProperty] public partial bool IsExpanded { get; set; }

    public override bool Equals(object? obj)
    {
        return Id == (obj as Queue)?.Id;
    }
}