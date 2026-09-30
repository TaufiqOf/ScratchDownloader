using Avalonia.Styling;

namespace ScratchDownloader.Models;

public class ThemeOption
{
    public string Name { get; }
    public ThemeVariant Variant { get; }

    public ThemeOption(string name, ThemeVariant variant)
    {
        Name = name;
        Variant = variant;
    }

    public override string ToString() => Name;
}