using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using ScratchDownloader.ViewModels;

namespace ScratchDownloader.Models;

public partial class ADialogViewModel : ViewModelBase
{
    [ObservableProperty] public partial ICommand? NegativeCommand { get; set; }
    [ObservableProperty] public partial ICommand? PositiveCommand { get; set; }
}