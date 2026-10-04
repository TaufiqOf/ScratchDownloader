using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using ScratchDownloader.Localization;
using ScratchDownloader.Models;

namespace ScratchDownloader.ViewModels.DialogControlViewModel;

public partial class MessageDialogControlViewModel : ADialogViewModel
{
    public MessageDialogControlViewModel(MessageDialogType messageDialogType,
        string? message,
        string? positiveButtonText = null,
        string? negativeButtonText = null,
        bool isCancelButtonVisible = false)
    {
        Message = message;
        NegativeButtonText = negativeButtonText ?? Strings.Get(Language.Common.Cancel);
        PositiveButtonText = positiveButtonText ?? Strings.Get(Language.Common.Ok);
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

    [ObservableProperty] public partial Icon Icon { get; set; }
    [ObservableProperty] public partial string? Message { get; set; }
    [ObservableProperty] public partial string? NegativeButtonText { get; set; }
    [ObservableProperty] public partial string? PositiveButtonText { get; set; }
    [ObservableProperty] public partial bool IsCancelButtonVisible { get; set; }
}