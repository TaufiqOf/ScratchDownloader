using CommunityToolkit.Mvvm.ComponentModel;

namespace ScratchDownloader.Models;

public abstract class ViewModelBase : ObservableObject
{
    public virtual void OnNavigatedTo()
    {
    }

    public virtual void OnNavigatedFrom()
    {
    }
}