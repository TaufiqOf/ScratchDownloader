using System.Collections.Generic;

namespace ScratchDownloader.Models;

public class DownloadMetadata
{
    public string Url { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public List<SegmentState> Segments { get; set; } = new();
}