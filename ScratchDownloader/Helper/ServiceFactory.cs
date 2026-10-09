using System;
using System.Collections.Generic;
using ScratchDownloader.Models;
using ScratchDownloader.Services;

namespace ScratchDownloader.Helper;

public static class ServiceFactory
{
    private static List<Type> _downloadServices;
    static ServiceFactory()
    {
        _downloadServices = new List<Type>
        {
            typeof(YoutubeDownloadService),
            typeof(DirectDownloadService),
            typeof(BitTorrentDownloadService),
        };
    }
    public static IDownloadService CreateDownloadService(string url)
    {
        try
        {
            foreach (var serviceType in _downloadServices)
            {
                if (Activator.CreateInstance(serviceType) is IDownloadService service)
                {
                    if (service.CanHandle(url))
                    {
                        return service;
                    }
                }
            }
            throw new NotSupportedException($"No download service available for URL: {url}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
    }
}