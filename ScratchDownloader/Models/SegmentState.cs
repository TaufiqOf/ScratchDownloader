namespace ScratchDownloader.Models;

public class SegmentState
{
    public int Index { get; set; }
    public long StartByte { get; set; }
    public long EndByte { get; set; }
    public long BytesDownloaded { get; set; }
}