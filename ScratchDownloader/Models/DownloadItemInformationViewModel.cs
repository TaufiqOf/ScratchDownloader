using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using ScratchDownloader.Helper;
using ScratchDownloader.Services;

namespace ScratchDownloader.Models;

public partial class DownloadItemInformationViewModel : ObservableObject
{
    private readonly HttpClient _httpClient = new();
    [ObservableProperty] public partial string FileName { get; set; } = string.Empty;

    [ObservableProperty] public partial string FileExtension { get; set; } = string.Empty;

    [ObservableProperty] public partial string FileHost { get; set; } = string.Empty;

    [ObservableProperty] public partial long FileSizeBytes { get; set; }

    [ObservableProperty] public partial string FileSizeDisplay { get; set; } = "0 MB";

    [ObservableProperty] public partial string? SavePath { get; set; } = string.Empty;
    [ObservableProperty] public partial Category Category { get; set; } = SettingsService.Settings.Categories["Other"];
    [ObservableProperty] public partial Queue Queue { get; set; } = SettingsService.Settings.Queues["Main"];
    [ObservableProperty] public partial int Segments { get; set; } = 8;
    public Uri Uri { get; set; }
    public async Task GetDataFromUrl(string uri, CancellationToken cancellationToken = default)
    {
        
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            uri);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var finalUri = response.RequestMessage?.RequestUri ?? new Uri(uri);
        Uri = finalUri;
        var fileName = GetFileName(response, finalUri);

        FileName = fileName;
        FileExtension = GetExtension(fileName);
        FileHost = finalUri.Host;

        // Extract content length header
        if (response.Content.Headers.ContentLength.HasValue)
        {
            var bytes = response.Content.Headers.ContentLength.Value;
            FileSizeBytes = bytes;
            FileSizeDisplay = FormatFileSize(bytes);
        }
        else
        {
            FileSizeBytes = 0;
            FileSizeDisplay = "Unknown size";
        }
        var category = SettingsService.Settings.Categories.Values.FirstOrDefault(q => q.Extension.Contains(FileExtension,StringComparison.OrdinalIgnoreCase));
        Category = category ?? SettingsService.Settings.Categories["Other"];
        Queue = SettingsService.Settings.Queues[Category.QueueId];
        SavePath = Path.Combine(Category.Folder,FileName);
    }




    private static string GetFileName(
        HttpResponseMessage response,
        Uri uri)
    {
        var contentDisposition =
            response.Content.Headers.ContentDisposition;

        var name = contentDisposition?.FileNameStar ??
                   contentDisposition?.FileName;

        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim('"');

        var pathName = Path.GetFileName(uri.LocalPath);

        if (!string.IsNullOrWhiteSpace(pathName))
            return pathName;

        return "download";
    }


    private static string GetExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
            return "FILE";

        return extension.TrimStart('.').ToUpperInvariant();
    }


    private static string FormatFileSize(long bytes)
    {
        return ByteSize.FromBytes(bytes).Humanize("#.##");
    }

    partial void OnCategoryChanged(Category value)
    {
        SavePath = Path.Combine(Category.Folder,FileName);
        Queue = SettingsService.Settings.Queues[Category.QueueId];
    }
    

    partial void OnQueueChanged(Queue value)
    {
        Segments = Queue.MaxConcurrentDownloads;
    }
}