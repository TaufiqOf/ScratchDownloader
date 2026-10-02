using Avalonia;
using Avalonia.Controls;
using ScratchDownloader.Models;

namespace ScratchDownloader.Views.Windows;

public partial class DialogWindow : Window
{
    public static readonly StyledProperty<ViewModelBase?> DialogControlProperty =
        AvaloniaProperty.Register<DialogWindow, ViewModelBase?>(
            nameof(DialogControl));

    static DialogWindow()
    {
        DialogControlProperty.Changed.AddClassHandler<DialogWindow>((sender, e) =>
        {
            if (e.NewValue is ViewModelBase newContent)
                sender.DialogContentControl.Content = newContent;
            else
                sender.DialogContentControl.Content = null;
        });
    }

    public DialogWindow()
    {
        InitializeComponent();
    }

    public ViewModelBase? DialogControl
    {
        get => GetValue(DialogControlProperty);
        set => SetValue(DialogControlProperty, value);
    }
}