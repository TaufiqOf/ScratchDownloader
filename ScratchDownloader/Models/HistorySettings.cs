using System.Collections.Generic;

namespace ScratchDownloader.Models;

public class HistorySettings
{
    public List<DownloadItemViewModel> DownloadItems { get; set; } = new();
}