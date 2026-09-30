namespace ScratchDownloader.Models;

public interface IAutoStartManager
{
    bool IsEnabled();
    void SetEnabled(bool enable);
}