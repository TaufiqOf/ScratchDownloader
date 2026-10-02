using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;

namespace ScratchDownloader.Models;

public partial class Category : ObservableObject
{
    [ObservableProperty] public partial string Id { get; set; } = string.Empty;
    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string Description { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsActive { get; set; } = true;
    [ObservableProperty] public partial string Extension { get; set; } = string.Empty;
    [ObservableProperty] public partial Icon Icon { get; set; } = Icon.Document;
    [ObservableProperty] public partial string Folder { get; set; } = string.Empty;
    [ObservableProperty] public partial string? QueueId { get; set; }

    [JsonIgnore] [ObservableProperty] public partial Queue? SelectedQueue { get; set; }

    [ObservableProperty] public partial bool IsExpanded { get; set; }


    partial void OnQueueIdChanged(string? value)
    {
        Console.WriteLine(
            $"QueueId changed to: {value}");
    }
}