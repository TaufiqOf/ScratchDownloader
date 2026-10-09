using System;

namespace ScratchDownloader.Models;

public class FileDataInformation
{
    public Uri Uri { get; set; }
    public Uri FinalUri { get; set; }
    public string FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileExtension { get; set; }
}