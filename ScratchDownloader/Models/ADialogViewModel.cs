using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScratchDownloader.Models;

public partial class ADialogViewModel : ViewModelBase
{
    public ADialogViewModel(Window? owner = null)
    {
        Owner = owner;
    }

    public Window? Owner { get; set; }
    [ObservableProperty] public partial ICommand? NegativeCommand { get; set; }
    [ObservableProperty] public partial ICommand? PositiveCommand { get; set; }
}