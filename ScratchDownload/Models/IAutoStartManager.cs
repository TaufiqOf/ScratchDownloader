namespace ScratchDownload.Models;

public interface IAutoStartManager
{
    bool IsEnabled();
    void SetEnabled(bool enable);
}