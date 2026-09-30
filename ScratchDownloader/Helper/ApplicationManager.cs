using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

namespace ScratchDownloader.Helper;

public static class ApplicationManager
{
    public static Window MainWindow
    {
        get;
        set
        {
            field = value;
            DialogManager.Initialize(value);
        }
    }

    public static TopLevel? GetTopLevel()
    {
        return TopLevel.GetTopLevel(MainWindow);
    }

    public static IClipboard? GetClipboard()
    {
        return GetTopLevel()?.Clipboard;
    }

    public static IStorageProvider? GetStorageProvider()
    {
        return GetTopLevel()?.StorageProvider;
    }

    public static async Task<string?> SaveStorageProvider(string title = "Select a file", string?
            startPath = null,
        string? suggestedFileName = "Untitled",
        bool allowMultiple = false,
        List<FilePickerFileType>? fileTypeFilter = null)
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider == null)
            return null;

        startPath ??= Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            "Downloads");
        var startFolder = await storageProvider.TryGetFolderFromPathAsync(startPath);
        var filePicker = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            SuggestedStartLocation = startFolder,
            FileTypeChoices = fileTypeFilter ?? new List<FilePickerFileType>
            {
                new("All Files")
                {
                    Patterns = new List<string> { "*" }
                }
            }
        });
        return filePicker?.TryGetLocalPath();
    }
    
    public static DownloadManager DownloadManager { get; set; } = new DownloadManager();
}