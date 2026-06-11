using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Storage;

internal sealed class AppSettingsStore
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AiGameBuilder",
        "settings.json");

    public AppSettings LoadOrCreate()
    {
        if (File.Exists(SettingsPath))
        {
            try
            {
                var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (settings != null)
                {
                    Normalize(settings);
                    return settings;
                }
            }
            catch
            {
                // Broken settings should not block app startup.
            }
        }

        var created = new AppSettings();
        Normalize(created);
        Save(created);
        return created;
    }

    public void Save(AppSettings settings)
    {
        Normalize(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(SettingsPath, json, new UTF8Encoding(false));
    }

    private static void Normalize(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.GamesRootPath))
        {
            settings.GamesRootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AiGameBuilder",
                "Games");
        }

        settings.LmStudio ??= new();
        settings.Fooocus ??= new();
        settings.Generation ??= new();
        new LmStudioProfileService().NormalizeProfiles(settings);
    }
}
