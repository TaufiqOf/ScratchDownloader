using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using ScratchDownloader.Models;

namespace ScratchDownloader.ViewModels.DialogControlViewModel;

public partial class MessageDialogControlViewModel : ViewModelBase
{
    [ObservableProperty] public partial Icon Icon { get; set; }
    [ObservableProperty] public partial string? Message { get; set; }
    [ObservableProperty] public partial string? CancelButtonText { get; set; }
    [ObservableProperty] public partial string? OkButtonText { get; set; }
    [ObservableProperty] public partial bool IsCancelButtonVisible { get; set; }
    [ObservableProperty] public partial ICommand? CancelCommand { get; set; }
    [ObservableProperty] public partial ICommand? OkCommand { get; set; }

    public MessageDialogControlViewModel(MessageDialogType messageDialogType, 
        string? message,
        string? cancelButtonText = "Cancel", 
        string? okButtonText = "OK", 
        bool isCancelButtonVisible = false)
    {
        Message = message;
        CancelButtonText = cancelButtonText;
        OkButtonText = okButtonText;
        IsCancelButtonVisible = isCancelButtonVisible;

        switch (messageDialogType)
        {
            case MessageDialogType.Warning:
                Icon = Icon.Warning;
                // Set warning icon or style
                break;
            case MessageDialogType.Error:
                Icon = Icon.ErrorCircle;
                // Set error icon or style
                break;
            case MessageDialogType.Information:
                Icon = Icon.Info;
                // Set information icon or style
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(messageDialogType), messageDialogType, null);
        }
    }
}