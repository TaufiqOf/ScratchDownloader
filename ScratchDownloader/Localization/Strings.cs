using System;
using ScratchDownloader.Localization.Languages;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace ScratchDownloader.Localization;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}

public sealed class Strings : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Translations =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en"] = EnglishStrings.Values,
            ["es"] = SpanishStrings.Values,
            ["de"] = GermanStrings.Values,
            ["ar"] = ArabicStrings.Values,
            ["fr"] = FrenchStrings.Values,
            ["ru"] = RussianStrings.Values,
            ["he"] = HebrewStrings.Values,
            ["bn"] = BanglaStrings.Values,
            ["hi"] = HindiStrings.Values,
            ["ja"] = JapaneseStrings.Values,
            ["ko"] = KoreanStrings.Values,
            ["zh"] = ChineseStrings.Values
        };

    private string _language = "en";

    private Strings()
    {
        // Ordered alphabetically by the English name of each language.
        Languages =
        [
            new LanguageOption("ar", "العربية"),
            new LanguageOption("bn", "বাংলা"),
            new LanguageOption("zh", "中文(简体)"),
            new LanguageOption("en", EnglishStrings.Values["English"]),
            new LanguageOption("fr", "Français"),
            new LanguageOption("de", "Deutsch"),
            new LanguageOption("he", "עברית"),
            new LanguageOption("hi", "हिन्दी"),
            new LanguageOption("ja", "日本語"),
            new LanguageOption("ko", "한국어"),
            new LanguageOption("ru", "Русский"),
            new LanguageOption("es", SpanishStrings.Values["Spanish"])
        ];
    }

    public static Strings Instance { get; } = new();
    public IReadOnlyList<LanguageOption> Languages { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public FlowDirection FlowDirection =>
        _language is "ar" or "he" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public string Language
    {
        get => _language;
        set
        {
            var normalized = Translations.ContainsKey(value) ? value : "en";
            var culture = CultureInfo.GetCultureInfo(normalized switch
            {
                "es" => "es-ES",
                "de" => "de-DE",
                "ar" => "ar-AE",
                "fr" => "fr-FR",
                "ru" => "ru-RU",
                "he" => "he-IL",
                "bn" => "bn-BD",
                "hi" => "hi-IN",
                "ja" => "ja-JP",
                "ko" => "ko-KR",
                "zh" => "zh-CN",
                _ => "en-US"
            });
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            if (_language == normalized)
                return;

            _language = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowDirection)));
            RefreshAll();
        }
    }

    public string this[string key] =>
        Translations[_language].TryGetValue(key, out var value)
            ? value
            : EnglishStrings.Values.TryGetValue(key, out value) ? value : key;

    private static readonly Dictionary<string, LocValue> Bound = new();

    internal static LocValue For(string key)
    {
        if (!Bound.TryGetValue(key, out var value))
            Bound[key] = value = new LocValue(key);
        return value;
    }

    private static void RefreshAll()
    {
        foreach (var value in Bound.Values)
            value.Refresh();
    }

    public static string Get(string key) => Instance[key];

    public static bool IsTranslation(string key, string value) =>
        Translations.Values.Any(t => t.TryGetValue(key, out var text) && text == value);

    public static string Format(string key, params object?[] args) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, Get(key), args);
}

public sealed class LocValue : INotifyPropertyChanged
{
    internal LocValue(string key)
    {
        Key = key;
    }

    public string Key { get; }
    public string Value => Strings.Get(Key);
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
}

public sealed class LocExtension : MarkupExtension
{
    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new ReflectionBindingExtension(nameof(LocValue.Value))
        {
            Mode = BindingMode.OneWay,
            Source = Strings.For(Key)
        }.ProvideValue(serviceProvider);
}
