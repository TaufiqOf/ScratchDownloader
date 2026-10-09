using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDownloader.Models;

public interface IDownloadService
{
    DownloadProgress Progress { get; set; }
    double CapSpeed { get; set; }
    int SegmentCount { get; set; }
    Uri Uri { get; set; }
    string? DestinationFilePath { get; set; }
    event EventHandler<string> ErrorOccurred;
    event EventHandler Completed;
    event EventHandler Initializing;
    event EventHandler? Downloading;

    Task<FileDataInformation> GetFileDataInformation(string uri,
        CancellationToken cancellationToken = default);
    
    void Start(CancellationToken cancellationToken = default);
    void Resume(CancellationToken cancellationToken = default);
    void Pause();
    void Stop();
}