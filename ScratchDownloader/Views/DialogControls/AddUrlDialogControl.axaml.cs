using Avalonia.Controls;
using Avalonia.Input;

namespace ScratchDownloader.Views.DialogControls;

public partial class AddUrlDialogControl : UserControl
{
    public AddUrlDialogControl()
    {
        InitializeComponent();
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if(e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            e.Handled = true;
        }
    }
}