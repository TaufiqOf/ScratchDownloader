using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Models;
using ScratchDownloader.ViewModels;
using ScratchDownloader.ViewModels.DialogControlViewModel;
using ScratchDownloader.Views.Windows;

namespace ScratchDownloader.Helper;

public static class DialogManager
{
    private static Window MainWindow { get; set; } 
    
    public static void Initialize(Window mainWindow)
    {
        MainWindow = mainWindow;
    }
    
    public static void ShowMessage(MessageDialogType type, string title, string message)
    {
        var dialogControlViewModel = new MessageDialogControlViewModel(type, message);

        var dialog = new DialogWindow()
        {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DialogControl = dialogControlViewModel
        };
        // Wire up commands to close the dialog window
        dialogControlViewModel.OkCommand = new RelayCommand(() => dialog.Close());
        dialogControlViewModel.CancelCommand = new RelayCommand(() => dialog.Close());

        dialog.ShowDialog(MainWindow);
    }
    
    public static async Task ShowMessage(ADialogViewModel viewModel, string title)
    {
        var dialogControlViewModel = viewModel;

        var dialog = new DialogWindow()
        {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DialogControl = dialogControlViewModel
        };
        // Wire up commands to close the dialog window
        dialogControlViewModel.OkCommand = new RelayCommand(() => dialog.Close());
        dialogControlViewModel.CancelCommand = new RelayCommand(() => dialog.Close());

        await dialog.ShowDialog(MainWindow);
    }
}