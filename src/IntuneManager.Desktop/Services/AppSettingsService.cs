using System;
using System.IO;
using System.Text.Json;
using IntuneManager.Desktop.Models;

namespace IntuneManager.Desktop.Services;

public static class AppSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IntuneManager",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* fall through to default */ }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = Path.Combine(directory, Path.GetRandomFileName());

            File.WriteAllText(tempPath, json);

            if (File.Exists(SettingsPath))
            {
                // Atomically replace existing settings with the new file.
                File.Replace(tempPath, SettingsPath, destinationBackupFileName: null);
            }
            else
            {
                // First-time save: move the temp file into place.
                File.Move(tempPath, SettingsPath);
            }
        }
        catch
        {
            // best effort
        }
    }
}
