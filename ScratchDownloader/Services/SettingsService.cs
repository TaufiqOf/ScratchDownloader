using System;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    private static readonly string _historySettingsPath =
        Path.Combine(
            _settingsDirectory,
            "historySettings.json");

    static SettingsService()
    {
        Directory.CreateDirectory(_settingsDirectory);
    }

    public static AppSettings Settings { get; } = Load();
    public static HistorySettings HistorySettings { get; } = LoadHistory();

    


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
            Console.WriteLine(
                $" Saving QueueId: {category.QueueId} CategoryId: {category.Id} Name: {category.Name} Folder: {category.Folder} Extension: {category.Extension}");
        });
 
        var jsonH = JsonSerializer.Serialize(
            HistorySettings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_historySettingsPath, jsonH);
    }

    private static AppSettings Load()
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);

            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = File.ReadAllText(_settingsPath);

            var settings = JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
            settings.Categories.Values.ToList().ForEach(category =>
            {
                if (string.IsNullOrEmpty(category.QueueId))
                {
                    category.QueueId = "Main";
                }

                category.SelectedQueue = settings.Queues[category.QueueId];
             });
            return settings;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new AppSettings();
        }
    }
    private static HistorySettings LoadHistory()
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);

            if (!File.Exists(_historySettingsPath))
                return new HistorySettings();

            var json = File.ReadAllText(_historySettingsPath);

            var historySettings = JsonSerializer.Deserialize<HistorySettings>(json)
                                  ?? new HistorySettings();
            foreach (var items in historySettings.DownloadItems)
            {
            }
            
            return historySettings;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new HistorySettings();
        }
    }
}