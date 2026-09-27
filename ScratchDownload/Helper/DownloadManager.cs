using System.Collections.ObjectModel;
using ScratchDownload.ViewModels;

namespace ScratchDownload.Helper;

public static class DownloadManager
{
    public  static ObservableCollection<DownloadItemViewModel> Downloads { get; } = new();
}