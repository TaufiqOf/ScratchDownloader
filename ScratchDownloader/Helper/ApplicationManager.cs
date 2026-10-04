using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using FluentIcons.Common;
using ScratchDownloader.Localization;
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
                    Name = Strings.Get(Language.DefaultCategory.Documents),
                    Icon = Icon.Document,
                    Folder = Path.Combine(home, "Documents"),
                    Extension = "pdf, doc, docx, txt, rtf, xls, xlsx, ppt, pptx, odt, csv",
                    Description = Strings.Get(Language.DefaultCategory.DocumentsDescription)
                }
            },
            {
                "Videos",
                new Category
                {
                    Id = "Videos",
                    Name = Strings.Get(Language.DefaultCategory.Videos),
                    Icon = Icon.Video,
                    Folder = Path.Combine(home, "Videos"),
                    Extension = "mp4, mkv, avi, mov, wmv, flv, webm, m4v, 3gp, ts",
                    Description = Strings.Get(Language.DefaultCategory.VideosDescription)
                }
            },
            {
                "Music",
                new Category
                {
                    Id = "Music",
                    Name = Strings.Get(Language.DefaultCategory.Music),
                    Icon = Icon.MusicNote1,
                    Folder = Path.Combine(home, "Music"),
                    Extension = "mp3, wav, flac, aac, ogg, m4a, wma, opus, alac",
                    Description = Strings.Get(Language.DefaultCategory.MusicDescription)
                }
            },
            {
                "Images",
                new Category
                {
                    Id = "Images",
                    Name = Strings.Get(Language.DefaultCategory.Images),
                    Icon = Icon.Image,
                    Folder = Path.Combine(home, "Pictures"),
                    Extension = "jpg, jpeg, png, gif, bmp, webp, svg, ico, tiff, psd",
                    Description = Strings.Get(Language.DefaultCategory.ImagesDescription)
                }
            },
            {
                "Archives",
                new Category
                {
                    Id = "Archives",
                    Name = Strings.Get(Language.DefaultCategory.Archives),
                    Icon = Icon.FolderZip,
                    Folder = Path.Combine(home, "Downloads", "Archives"),
                    Extension = "zip, rar, 7z, tar, gz, bz2, xz, zst, lz, lzma, tgz, tbz2, txz",
                    Description = Strings.Get(Language.DefaultCategory.ArchivesDescription)
                }
            },
            {
                "Applications",
                new Category
                {
                    Id = "Applications",
                    Name = Strings.Get(Language.DefaultCategory.Applications),
                    Icon = Icon.AppFolder,
                    Folder = Path.Combine(home, "Downloads", "Applications"),
                    Extension = "exe, msi, deb, rpm, AppImage, dmg, pkg, apk, iso, bin, sh",
                    Description = Strings.Get(Language.DefaultCategory.ApplicationsDescription)
                }
            },
            {
                "Other",
                new Category
                {
                    Id = "Other",
                    Name = Strings.Get(Language.DefaultCategory.Other),
                    Icon = Icon.DocumentBorder,
                    Folder = Path.Combine(home, "Downloads", "Other"),
                    Extension = string.Empty,
                    Description = Strings.Get(Language.DefaultCategory.OtherDescription)
                }
            }
        };
    }

    public static void LocalizeDefaults()
    {
        foreach (var (id, nameKey, descriptionKey) in new[]
                 {
                     ("Documents", Language.DefaultCategory.Documents, Language.DefaultCategory.DocumentsDescription),
                     ("Videos", Language.DefaultCategory.Videos, Language.DefaultCategory.VideosDescription),
                     ("Music", Language.DefaultCategory.Music, Language.DefaultCategory.MusicDescription),
                     ("Images", Language.DefaultCategory.Images, Language.DefaultCategory.ImagesDescription),
                     ("Archives", Language.DefaultCategory.Archives, Language.DefaultCategory.ArchivesDescription),
                     ("Applications", Language.DefaultCategory.Applications, Language.DefaultCategory.ApplicationsDescription),
                     ("Other", Language.DefaultCategory.Other, Language.DefaultCategory.OtherDescription)
                 })
        {
            if (!Categories.TryGetValue(id, out var category))
                continue;

            category.Name = Strings.Get(nameKey);
            category.Description = Strings.Get(descriptionKey);
        }

        LocalizeDefaultQueue("Main", Language.DefaultCategory.MainQueue);
        LocalizeDefaultQueue("Secondary", Language.DefaultCategory.SecondaryQueue);
    }

    private static void LocalizeDefaultQueue(string id, string nameKey)
    {
        if (!Queues.TryGetValue(id, out var queue))
            return;

        if (Strings.IsTranslation(nameKey, queue.Name))
            queue.Name = Strings.Get(nameKey);
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

    public static Dictionary<string, Queue> Queues { get; set; } = new()
    {
        { "Main", new Queue { Id = "Main", Name = Strings.Get(Language.DefaultCategory.MainQueue) } },
        { "Secondary", new Queue { Id = "Secondary", Name = Strings.Get(Language.DefaultCategory.SecondaryQueue) } }
    };

    public static DownloadManager DownloadManager { get; set; } = new();

    public static Dictionary<string, Category> Categories { get; }

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

    public static async Task<string?> SaveFileDialog(string? title = null, string?
            startPath = null,
        string? suggestedFileName = null,
        bool allowMultiple = false,
        List<FilePickerFileType>? fileTypeFilter = null)
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider == null)
            return null;

        title ??= Strings.Get(Language.FileDialogs.SelectFile);
        suggestedFileName ??= Strings.Get(Language.FileDialogs.Untitled);
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
                new(Strings.Get(Language.Common.AllFiles))
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
            Title = Strings.Get(Language.FileDialogs.SelectSaveFolder),
            AllowMultiple = false,
            SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(path)
        });

        if (folders.Count > 0) return folders[0].Path.LocalPath;
        // Save settings here if applicable: SettingsService.Save();
        return null;
    }
}