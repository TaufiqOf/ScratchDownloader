using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using ScratchDownloader.ViewModels;

namespace ScratchDownloader.Models;

public partial class ADialogViewModel : ViewModelBase
{
    [ObservableProperty] public partial ICommand? CancelCommand { get; set; }
    [ObservableProperty] public partial ICommand? OkCommand { get; set; }
}