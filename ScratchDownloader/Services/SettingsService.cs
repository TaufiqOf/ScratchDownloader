using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Styling;
using ScratchDownloader.Models;

namespace ScratchDownloader.Services;

public static class SettingsService
{
    
    private static readonly string _settingsDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ScratchDownloader");

    private static readonly string _settingsPath =
        Path.Combine(
            _settingsDirectory,
            "settings.json");

    public static AppSettings Settings { get; private set; } = Load();

    static SettingsService()
    {
        Directory.CreateDirectory(_settingsDirectory);
    }

    public static void Save()
    {
        Directory.CreateDirectory(_settingsDirectory);

        var json = JsonSerializer.Serialize(
            Settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_settingsPath, json);
        Settings.Categories.Values.ToList().ForEach(category =>
        {
            Console.WriteLine($" Saving QueueId: {category.QueueId} CategoryId: {category.Id} Name: {category.Name} Folder: {category.Folder} Extension: {category.Extension}");
        });

    }

    private static AppSettings Load()
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);

            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = File.ReadAllText(_settingsPath);

            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}