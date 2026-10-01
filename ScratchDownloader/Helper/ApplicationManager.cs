using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using FluentIcons.Common;
using ScratchDownloader.Models;

namespace ScratchDownloader.Helper;

public static class ApplicationManager
{
    static ApplicationManager()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Categories = new Dictionary<string, Category>
        {
            {
                "Documents",
                new Category
                {
                    Id = "Documents",
                    Name = "Documents",
                    Icon = Icon.Document,
                    Folder = Path.Combine(home, "Documents"),
                    Extension = "pdf, doc, docx, txt, rtf, xls, xlsx, ppt, pptx, odt, csv",
                    Description = "Text documents, spreadsheets, and presentations"
                }
            },
            {
                "Videos",
                new Category
                {
                    Id = "Videos",
                    Name = "Videos",
                    Icon = Icon.Video,
                    Folder = Path.Combine(home, "Videos"),
                    Extension = "mp4, mkv, avi, mov, wmv, flv, webm, m4v, 3gp, ts",
                    Description = "Movies, clips, and video files"
                }
            },
            {
                "Music",
                new Category
                {
                    Id = "Music",
                    Name = "Music",
                    Icon = Icon.MusicNote1,
                    Folder = Path.Combine(home, "Music"),
                    Extension = "mp3, wav, flac, aac, ogg, m4a, wma, opus, alac",
                    Description = "Songs, podcasts, and audio files"
                }
            },
            {
                "Images",
                new Category
                {
                    Id = "Images",
                    Name = "Images",
                    Icon = Icon.Image,
                    Folder = Path.Combine(home, "Pictures"),
                    Extension = "jpg, jpeg, png, gif, bmp, webp, svg, ico, tiff, psd",
                    Description = "Photos, graphics, and vector images"
                }
            },
            {
                "Archives",
                new Category
                {
                    Id = "Archives",
                    Name = "Archives",
                    Icon = Icon.FolderZip,
                    Folder = Path.Combine(home, "Downloads", "Archives"),
                    Extension = "zip, rar, 7z, tar, gz, bz2, xz, zst, lz, lzma, tgz, tbz2, txz",
                    Description = "Compressed archives and package files"
                }
            },
            {
                "Applications",
                new Category
                {
                    Id = "Applications",
                    Name = "Applications",
                    Icon = Icon.AppFolder,
                    Folder = Path.Combine(home, "Downloads", "Applications"),
                    Extension = "exe, msi, deb, rpm, AppImage, dmg, pkg, apk, iso, bin, sh",
                    Description = "Software installers, executables, and disk images"
                }
            },
            {
                "Other",
                new Category
                {
                    Id = "Other",
                    Name = "Other",
                    Icon = Icon.DocumentBorder,
                    Folder = Path.Combine(home, "Downloads", "Other"),
                    Extension = string.Empty,
                    Description = "Everything the other categories don't claim"
                }
            }
        };
    }

    public static Window? MainWindow
    {
        get;
        set
        {
            field = value;
            DialogManager.Initialize(value);
        }
    }

    public static Dictionary<string, Queue> Queues { get; set; } = new Dictionary<string, Queue>
    {
        { "Main", new Queue() { Id = "Main", Name = "Main" } },
        { "Secondary", new Queue() { Id = "Secondary", Name = "Secondary" } },

    };

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

    public static async Task<string?> SaveFileDialog(string title = "Select a file", string?
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

    public static async Task<string?> OpenFolderDialog(string? path)
    {
        path ??= Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            "Downloads");
        var storageProvider = GetStorageProvider();
        if (storageProvider == null)
            return null;
        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = $"Select Save Folder",
            AllowMultiple = false,
            SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(path)
        });

        if (folders.Count > 0)
        {
            return folders[0].Path.LocalPath;
            // Save settings here if applicable: SettingsService.Save();
        }

        return null;
    }

    public static DownloadManager DownloadManager { get; set; } = new DownloadManager();

    public static Dictionary<string, Category> Categories { get; }
}