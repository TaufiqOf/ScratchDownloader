using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace ScratchDownloader.Models;

public interface IDownloadService
{ 
    event EventHandler<DownloadProgress> ProgressChanged;
    event EventHandler Completed;
    int SegmentCount { get; set; }
    Uri Uri { get; set; }
    string? DestinationFilePath { get; set; }
    void Start(CancellationToken cancellationToken = default);
    void Resume(CancellationToken cancellationToken = default);
    void Pause();
    void Stop();
}   