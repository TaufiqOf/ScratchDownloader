using System.Collections.Generic;

namespace ScratchDownloader.Models;
public class SegmentProgress
{
    public int Index { get; set; }
    public double Progress { get; set; }
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double BytesPerSecond { get; set; } // Bytes per second
}
public class DownloadProgress
{
    public double Progress { get; set; }
    public double BytesPerSecond { get; set; } // Bytes per second
    public Dictionary<int, SegmentProgress> SegmentProgress { get; set; } = new Dictionary<int, SegmentProgress>();
}