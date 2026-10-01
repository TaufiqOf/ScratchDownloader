using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDownloader.Models;

public interface IDownloadService
{ 
    DownloadProgress Progress { get; set; }
    event EventHandler Completed;
    event EventHandler Initializing;
    event EventHandler? Downloading;
    int SegmentCount { get; set; }
    Uri Uri { get; set; }
    string? DestinationFilePath { get; set; }
    void Start(CancellationToken cancellationToken = default);
    void Resume(CancellationToken cancellationToken = default);
    void Pause();
    void Stop();
}   