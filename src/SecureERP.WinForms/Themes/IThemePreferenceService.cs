namespace SecureERP.WinForms.Themes;

public interface IThemePreferenceService
{
    ThemePreference Load();
    void Save(ThemePreference preference);
}

