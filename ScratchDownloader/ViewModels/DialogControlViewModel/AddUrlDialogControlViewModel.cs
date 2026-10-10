using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Localization;
using ScratchDownloader.Models;
using ScratchDownloader.Services;
using Timer = System.Timers.Timer;

namespace ScratchDownloader.ViewModels.DialogControlViewModel;

public partial class AddUrlDialogControlViewModel : ADialogViewModel
{
    private CancellationTokenSource? _detectCancellation;
    private readonly Timer _detectingAnimationTimer = new(500);
    [ObservableProperty] private DownloadItemInformationViewModel _downloadItemInformation = new();
    [ObservableProperty] private string _detectingAnimationText = "•••";

    public AddUrlDialogControlViewModel(Window? owner = null) : base(owner)
    {
        DownloadItemInformation.Category = SettingsService.Settings.Categories["Other"];
        DownloadItemInformation.Queue = SettingsService.Settings.Queues["Main"];
        DownloadItemInformation.Segments = 8;
        _detectingAnimationTimer.Elapsed += (s, e) =>
        {
            if (DetectingAnimationText.Length >= 3)
                DetectingAnimationText = "•";
            else
                DetectingAnimationText += "•";
        };
        _detectingAnimationTimer.Start();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDownload))]
    [NotifyPropertyChangedFor(nameof(HasFileInfo))]
    public partial string? Url { get; set; }

    [ObservableProperty] public partial bool IsDetecting { get; set; }

    [ObservableProperty] public partial bool HasFileInfo { get; set; }



    [ObservableProperty] public partial bool UseServerFilename { get; set; } = true;


    public bool CanDownload =>
        !string.IsNullOrWhiteSpace(Url) &&
        HasFileInfo &&
        !IsDetecting;


    public ObservableCollection<Category> Categories { get; } = new(SettingsService.Settings.Categories.Values);

    public ObservableCollection<Queue> Queues { get; } = new(SettingsService.Settings.Queues.Values);

    public ObservableCollection<int> SegmentOptions { get; } =
    [
        1,
        2,
        4,
        8,
        16,
        32
    ];


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
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        try
        {
            DownloadItemInformation =
                await ApplicationManager.DownloadManager.GetDownloadInfoFromUrl(url, cancellationToken);
            if (File.Exists(DownloadItemInformation.SavePath))
            {
                RenameFile();
            }

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
        if (clipboard != null) Url = await clipboard.TryGetTextAsync();
    }


    [RelayCommand]
    private async Task Browse()
    {
        DownloadItemInformation.SavePath = await ApplicationManager.SaveFileDialog(
            Strings.Get(Language.AddUrlDialog.SelectSaveLocation),
            Path.GetDirectoryName(DownloadItemInformation.SavePath) ??
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            DownloadItemInformation.SavePath ?? "download",
            false,
            new List<FilePickerFileType>
            {
                new(Strings.Get(Language.Common.AllFiles))
                {
                    Patterns = new List<string> { "*" }
                }
            });
    }

    [RelayCommand]
    private async Task StartDownload()
    {
        if (File.Exists(DownloadItemInformation.SavePath))
        {
          await DialogManager.ShowMessage(MessageDialogType.Warning, Strings.Get(Language.AddUrlDialog.FileExists),
                Strings.Format(Language.AddUrlDialog.FileExistsMessage, Path.GetFileName(DownloadItemInformation.SavePath))
                , Strings.Get(Language.AddUrlDialog.RenameAndContinue),
                new RelayCommand(() =>
                {
                    RenameFile();
                    PositiveCommand?.Execute(null);
                }),
                Strings.Get(Language.AddUrlDialog.Overwrite),
                new RelayCommand(() =>
                {
                    // Do nothing on overwrite
                    PositiveCommand?.Execute(null);
                }),
                Owner
                );
            return;
        }

        PositiveCommand?.Execute(null);
    }

    private void RenameFile()
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(DownloadItemInformation.SavePath);
        var extension = Path.GetExtension(DownloadItemInformation.SavePath);
        var directory = Path.GetDirectoryName(DownloadItemInformation.SavePath) ??
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var counter = 1;
        string newSavePath;
        do
        {
            newSavePath = Path.Combine(directory, $"{fileNameWithoutExtension} ({counter}){extension}");
            counter++;
        } while (File.Exists(newSavePath));

        DownloadItemInformation.SavePath = newSavePath;
    }
}