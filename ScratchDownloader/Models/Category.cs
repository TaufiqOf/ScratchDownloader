using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;

namespace ScratchDownloader.Models;

public partial class Category : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Extension { get; set; } = string.Empty;
    public Icon Icon { get; set; } = Icon.Document;
    public string Folder { get; set; } = string.Empty;
    
    [ObservableProperty] private bool isExpanded;

    public override bool Equals(object? obj)
    {
        return Id == (obj as Category)?.Id;
    }
}