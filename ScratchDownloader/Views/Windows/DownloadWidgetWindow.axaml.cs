using Avalonia.Controls;
using Avalonia.Input;
using ScratchDownloader.Models;

namespace ScratchDownloader.Views.Windows;

public partial class DownloadWidgetWindow : Window
{
    private readonly DownloadItemViewModel _downloadItemViewModel;

    public DownloadWidgetWindow(DownloadItemViewModel downloadItemViewModel)
    {
        this.DataContext = downloadItemViewModel;
        _downloadItemViewModel = downloadItemViewModel;
        InitializeComponent();
    }

    public DownloadItemViewModel ItemViewModel => _downloadItemViewModel;

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        this.Close();
    }
}