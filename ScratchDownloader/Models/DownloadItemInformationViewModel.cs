using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using ScratchDownloader.Services;
using ScratchDownloader.Localization;

namespace ScratchDownloader.Models;

public partial class DownloadItemInformationViewModel : ObservableObject
{
    [ObservableProperty] private bool _loading = false;

    public DownloadItemInformationViewModel()
    {
        Loading = true;
    }

    [ObservableProperty] public partial string FileName { get; set; } = string.Empty;
    [ObservableProperty] public partial string SavedFileName { get; set; } = string.Empty;
    [ObservableProperty] public partial string FileExtension { get; set; } = string.Empty;
    [ObservableProperty] public partial string FileHost { get; set; } = string.Empty;
    [ObservableProperty] public partial long FileSizeBytes { get; set; }
    [ObservableProperty] public partial string FileSizeDisplay { get; set; } = "0 MB";
    [ObservableProperty] public partial string? SavePath { get; set; } = string.Empty;
    [ObservableProperty] public partial Category Category { get; set; } = SettingsService.Settings.Categories["Other"];
    [ObservableProperty] public partial Queue Queue { get; set; } = SettingsService.Settings.Queues["Main"];
    [ObservableProperty] public partial int Segments { get; set; } = 8;
    [ObservableProperty] public partial string? Checksum { get; set; } = string.Empty;
    [ObservableProperty] public partial bool StartImmediately { get; set; } = true;
    [ObservableProperty] public partial DownloadType DownloadType { get; set; } = DownloadType.Direct;
    
    public Uri Uri { get; set; }

    public async Task GetDataFromUrl(string uri, IDownloadService downloadService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Loading = false;
            var data = await downloadService.GetFileDataInformation(uri, cancellationToken);

            var settingsCategory = SettingsService.Settings.Categories["Other"];

            var category = SettingsService.Settings.Categories.Values.FirstOrDefault(q =>
                q.Extension.Contains(data.FileExtension, StringComparison.OrdinalIgnoreCase));

            Uri = data.FinalUri;
            FileName = data.FileName;
            FileExtension = data.FileExtension;
            FileHost = data.FinalUri.Host;
            FileSizeBytes = data.FileSizeBytes;
            FileSizeDisplay = data.FileSizeBytes != 0
                ? FormatFileSize(data.FileSizeBytes)
                : Strings.Get(Language.Common.UnknownSize);
            Category = category ?? settingsCategory;
            SavePath = Path.Combine(Category.Folder, FileName);
            DownloadType = data.DownloadType;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    private static string FormatFileSize(long bytes)
    {
        return ByteSize.FromBytes(bytes).Humanize("#.##");
    }

    partial void OnCategoryChanged(Category value)
    {
        if (Loading)
            return;
        SavePath = Path.Combine(Category.Folder, FileName);
        var settingsQueue = SettingsService.Settings.Queues["Main"];
        Queue = Category.QueueId is not null ? SettingsService.Settings.Queues[Category.QueueId] : settingsQueue;
        Segments = Queue.Segments;
    }


    partial void OnQueueChanged(Queue value)
    {
        if (Loading)
            return;
        Segments = Queue.Segments;
    }

    partial void OnSavePathChanged(string? value)
    {
        if (Loading)
            return;
        if (string.IsNullOrWhiteSpace(value))
            return;
        SavedFileName = Path.GetFileName(value);
    }
}