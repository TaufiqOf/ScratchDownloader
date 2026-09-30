using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Models;
using ScratchDownloader.ViewModels.DialogControlViewModel;
using ScratchDownloader.Views.Windows;

namespace ScratchDownloader.Helper;

public static class DialogManager
{
    public static Window MainWindow { get; set; }


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
}