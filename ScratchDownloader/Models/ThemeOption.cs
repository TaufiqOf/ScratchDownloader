using Avalonia.Styling;

namespace ScratchDownloader.Models;

public class ThemeOption
{
    public ThemeOption(string name, ThemeVariant variant)
    {
        Name = name;
        Variant = variant;
    }

    public string Name { get; }
    public ThemeVariant Variant { get; }

    public override string ToString()
    {
        return Name;
    }
}