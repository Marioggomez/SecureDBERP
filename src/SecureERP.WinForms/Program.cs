using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraEditors;
using SecureERP.WinForms.Services.Navigation;
using SecureERP.WinForms.Shell;
using SecureERP.WinForms.Splash;
using SecureERP.WinForms.Themes;

namespace SecureERP.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        SkinManager.EnableFormSkins();
        WindowsFormsSettings.SetPerMonitorDpiAware();

        IThemePreferenceService themePreferenceService = new FileThemePreferenceService();

        using ErpSplashForm splash = new();
        splash.Show();
        splash.UpdateStatus("Cargando preferencias de apariencia...");
        Application.DoEvents();

        ThemePreference preference = themePreferenceService.Load();
        UserLookAndFeel.Default.SetSkinStyle(preference.SkinName);

        splash.UpdateStatus("Construyendo navegación modular...");
        Application.DoEvents();

        NavigationModuleCatalog moduleCatalog = new(themePreferenceService);
        IReadOnlyList<NavigationItemDefinition> items = moduleCatalog.BuildItems();

        splash.UpdateStatus("Iniciando shell principal...");
        Application.DoEvents();

        MainShellForm shell = new(items, themePreferenceService);

        splash.Close();
        Application.Run(shell);
    }
}

