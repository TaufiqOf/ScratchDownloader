using Avalonia.Controls;
using Avalonia.Input;
using ScratchDownloader.Models;

namespace ScratchDownloader.Views.Windows;

public partial class DownloadWidgetWindow : Window
{
    private readonly DownloadItemViewModel _downloadItemViewModel;

    public DownloadWidgetWindow(DownloadItemViewModel downloadItemViewModel)
    {
        _downloadItemViewModel = downloadItemViewModel;
        InitializeComponent();
        FileNameTextBlock.Text = _downloadItemViewModel.DownloadItemInformation?.SavedFileName;

    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        this.Close();
    }
}