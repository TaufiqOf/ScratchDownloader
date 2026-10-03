using Avalonia.Controls;
using Avalonia.Input;
using ScratchDownloader.Models;

namespace ScratchDownloader.Views.PageControls;

public partial class HomePageView : UserControl
{
    public HomePageView()
    {
        InitializeComponent();
    }

    private void DataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is DownloadItemViewModel vm)
        {
            if (vm.ShowWidgetCommand.CanExecute(null))
            {
                vm.ShowWidgetCommand.Execute(null);
            }
        }
    }
}