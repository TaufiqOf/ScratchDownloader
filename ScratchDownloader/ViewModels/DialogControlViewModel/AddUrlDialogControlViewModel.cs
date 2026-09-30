using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;
using Humanizer;

namespace ScratchDownloader.ViewModels.DialogControlViewModel;

public partial class AddUrlDialogControlViewModel : ADialogViewModel
{

    private CancellationTokenSource? _detectCancellation;

    [ObservableProperty]
    private DownloadItemInformationViewModel _downloadItemInformation = new DownloadItemInformationViewModel();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDownload))]
    [NotifyPropertyChangedFor(nameof(HasFileInfo))]
    public partial string? Url { get; set; }

    [ObservableProperty] public partial bool IsDetecting { get; set; }

    [ObservableProperty] public partial bool HasFileInfo { get; set; }

    [ObservableProperty] public partial bool StartImmediately { get; set; }

    [ObservableProperty] public partial bool UseServerFilename { get; set; } = true;


    public bool CanDownload =>
        !string.IsNullOrWhiteSpace(Url) &&
        HasFileInfo &&
        !IsDetecting;


    public ObservableCollection<string> Categories { get; } =
    [
        "Other",
        "Documents",
        "Videos",
        "Music",
        "Images",
        "Archives"
    ];

    public ObservableCollection<string> Queues { get; } =
    [
        "Main",
        "Queue 1",
        "Queue 2"
    ];

    public ObservableCollection<string> SegmentOptions { get; } =
    [
        "1 connection",
        "4 connections",
        "8 connections",
        "16 connections"
    ];


    public AddUrlDialogControlViewModel()
    {
        DownloadItemInformation.Category = "Other";
        DownloadItemInformation.Queue = "Main";
        DownloadItemInformation.Segments = "8 connections";
    }


    partial void OnUrlChanged(string? value)
    {
        _ = DetectUrlAsync(value);
    }


    private async Task DetectUrlAsync(string? url)
    {
        _detectCancellation?.Cancel();
        _detectCancellation?.Dispose();

        _detectCancellation = new CancellationTokenSource();

        var cancellationToken = _detectCancellation.Token;

        HasFileInfo = false;
        DownloadItemInformation = new DownloadItemInformationViewModel();

        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            IsDetecting = false;
            return;
        }

        IsDetecting = true;

        try
        {
            DownloadItemInformation = await ApplicationManager.DownloadManager.GetDownloadInfoFromUrl(url, cancellationToken);
            HasFileInfo = true;
        }
        catch (OperationCanceledException)
        {
            // A newer URL was entered.
        }
        catch
        {
            HasFileInfo = false;
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
                IsDetecting = false;
        }

        OnPropertyChanged(nameof(CanDownload));
    }


   

    [RelayCommand]
    private async Task PasteAsync()
    {
        var clipboard = ApplicationManager.GetClipboard();
        if (clipboard != null)
        {
            Url = await clipboard.TryGetTextAsync();
        }
    }


    [RelayCommand]
    private async Task Browse()
    {
        DownloadItemInformation.SavePath = await ApplicationManager.SaveStorageProvider(
            title: "Select Save Location",
            startPath: Path.GetDirectoryName(DownloadItemInformation.SavePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            suggestedFileName: DownloadItemInformation.SavePath ?? "download",
            allowMultiple: false,
            fileTypeFilter: new List<FilePickerFileType>
            {
                new("All Files")
                {
                    Patterns = new List<string> { "*" }
                }
            });
    }
}