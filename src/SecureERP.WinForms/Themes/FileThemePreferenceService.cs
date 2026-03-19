using System.Text.Json;

namespace SecureERP.WinForms.Themes;

public sealed class FileThemePreferenceService : IThemePreferenceService
{
    private const string FileName = "theme-preference.json";

    private readonly string _settingsFilePath;

    public FileThemePreferenceService()
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecureERP",
            "WinForms");

        Directory.CreateDirectory(root);
        _settingsFilePath = Path.Combine(root, FileName);
    }

    public ThemePreference Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return ThemePreference.Default;
        }

        try
        {
            string json = File.ReadAllText(_settingsFilePath);
            ThemePreference? preference = JsonSerializer.Deserialize<ThemePreference>(json);
            return preference ?? ThemePreference.Default;
        }
        catch
        {
            return ThemePreference.Default;
        }
    }

    public void Save(ThemePreference preference)
    {
        string json = JsonSerializer.Serialize(preference, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsFilePath, json);
    }
}

