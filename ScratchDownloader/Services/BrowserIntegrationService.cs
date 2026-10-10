using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Localization;
using ScratchDownloader.ViewModels.DialogControlViewModel;

namespace ScratchDownloader.Services;

/// <summary>
///     Local HTTP endpoint (127.0.0.1 only) used by the browser extension to hand over download links.
/// </summary>
public static class BrowserIntegrationService
{
    public const int Port = 47653;
    private static HttpListener? _listener;
    private static readonly CancellationTokenSource Cts = new();

    public static void Start()
    {
        if (_listener != null) return;
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _ = Task.Run(LoopAsync);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Browser integration disabled: {e.Message}");
            _listener = null;
        }
    }

    public static void Stop()
    {
        Cts.Cancel();
        try { _listener?.Close(); } catch { /* ignored */ }
        _listener = null;
    }

    private static async Task LoopAsync()
    {
        while (_listener is { IsListening: true } && !Cts.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { break; }
            _ = Task.Run(() => HandleAsync(ctx));
        }
    }

    private static async Task HandleAsync(HttpListenerContext ctx)
    {
        var res = ctx.Response;
        try
        {
            var origin = ctx.Request.Headers["Origin"];
            var trusted = !string.IsNullOrEmpty(origin) &&
                          (origin.StartsWith("moz-extension://", StringComparison.Ordinal) ||
                           origin.StartsWith("chrome-extension://", StringComparison.Ordinal));
            if (!trusted)
            {
                res.StatusCode = 403;
                return;
            }

            res.AddHeader("Access-Control-Allow-Origin", origin!);
            res.AddHeader("Access-Control-Allow-Headers", "Content-Type");
            res.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            if (ctx.Request.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 204;
            }
            else if (ctx.Request.HttpMethod == "GET" && path == "/ping")
            {
                await WriteAsync(res, 200, "{\"ok\":true}");
            }
            else if (ctx.Request.HttpMethod == "POST" && path == "/add")
            {
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                using var doc = JsonDocument.Parse(await reader.ReadToEndAsync());
                var url = doc.RootElement.TryGetProperty("url", out var u) ? u.GetString() : null;
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    uri.Scheme is not ("http" or "https" or "ftp" or "magnet"))
                {
                    await WriteAsync(res, 400, "{\"ok\":false,\"error\":\"invalid url\"}");
                    return;
                }

                Dispatcher.UIThread.Post(() => _ = ShowAddDialogAsync(uri.AbsoluteUri));
                await WriteAsync(res, 200, "{\"ok\":true}");
            }
            else
            {
                res.StatusCode = 404;
            }
        }
        catch
        {
            res.StatusCode = 400;
        }
        finally
        {
            try { res.Close(); } catch { /* ignored */ }
        }
    }

    private static async Task WriteAsync(HttpListenerResponse res, int status, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        res.StatusCode = status;
        res.ContentType = "application/json";
        await res.OutputStream.WriteAsync(bytes);
    }

    private static async Task ShowAddDialogAsync(string url)
    {
        var window = ApplicationManager.MainWindow;
        if (window != null)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        }

        try
        {
            var dialog = new AddUrlDialogControlViewModel { Url = url };
            var ok = new RelayCommand(() => ApplicationManager.DownloadManager.Add(dialog.DownloadItemInformation));
            await DialogManager.ShowMessage(dialog, Strings.Get(Language.HomePage.NewDownload), ok);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
