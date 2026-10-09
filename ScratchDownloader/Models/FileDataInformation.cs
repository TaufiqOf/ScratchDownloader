using System;

namespace ScratchDownloader.Models;

public enum DownloadType
{
    Unknown = 0,
    Direct = 1,
    Youtube = 2,
    BitTorrent = 3,
}

public class FileDataInformation
{
    public Uri Uri { get; set; }
    public Uri FinalUri { get; set; }
    public string FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileExtension { get; set; }

    public DownloadType DownloadType { get; set; } = DownloadType.Unknown;
}