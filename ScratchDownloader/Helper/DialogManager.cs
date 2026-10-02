using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Models;
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

    public static void ShowMessage(
        MessageDialogType type,
        string title,
        string message,
        string positiveText = "OK",
        IRelayCommand? positiveCommand = null,
        string negativeText = "",
        IRelayCommand? negativeCommand = null)
    {
        var dialogControlViewModel = new MessageDialogControlViewModel(type, message, positiveText, negativeText,
            !string.IsNullOrWhiteSpace(negativeText));
        var dialog = new DialogWindow
        {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DialogControl = dialogControlViewModel
        };
        // Wire up commands to close the dialog window
        dialogControlViewModel.PositiveCommand = new RelayCommand(() =>
        {
            if (positiveCommand?.CanExecute(null) == true) positiveCommand.Execute(null);

            dialog.Close();
        });
        dialogControlViewModel.NegativeCommand = new RelayCommand(() =>
        {
            if (negativeCommand?.CanExecute(null) == true) negativeCommand.Execute(null);

            dialog.Close();
        });

        dialog.ShowDialog(MainWindow);
    }

    public static async Task ShowMessage(
        ADialogViewModel viewModel,
        string title,
        IRelayCommand? positiveCommand = null,
        IRelayCommand? negativeCommand = null)
    {
        var dialogControlViewModel = viewModel;

        var dialog = new DialogWindow
        {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DialogControl = dialogControlViewModel
        };

        // Wire up commands to close the dialog and execute the passed RelayCommand
        dialogControlViewModel.PositiveCommand = new RelayCommand(() =>
        {
            if (positiveCommand?.CanExecute(null) == true) positiveCommand.Execute(null);

            dialog.Close();
        });

        dialogControlViewModel.NegativeCommand = new RelayCommand(() =>
        {
            if (negativeCommand?.CanExecute(null) == true) negativeCommand.Execute(null);

            dialog.Close();
        });
        await dialog.ShowDialog(MainWindow);
    }
}