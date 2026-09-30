using System.Collections.ObjectModel;
using ScratchDownloader.ViewModels;
using DownloadItemViewModel = ScratchDownloader.Models.DownloadItemViewModel;

namespace ScratchDownloader.Helper;

public static class DownloadManager
{
    public  static ObservableCollection<DownloadItemViewModel> Downloads { get; } = new();
}