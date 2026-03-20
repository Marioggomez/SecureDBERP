using System.Diagnostics;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using SecureERP.WinForms.Common;
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
        SkinManager.EnableMdiFormSkins();
        DefaultLookAndFeel lookAndFeel = new()
        {
            EnableBonusSkins = true
        };
        GC.KeepAlive(lookAndFeel);
        WindowsFormsSettings.AllowRoundedWindowCorners = DefaultBoolean.True;
        WindowsFormsSettings.SetPerMonitorDpiAware();
        SplashScreenManager.RegisterUserSkins(typeof(Program).Assembly);

        AppBranding.ApplicationName = "SecureERP";
        AppBranding.ApplicationSubtitle = "Cliente Desktop Enterprise";
        AppBranding.ShellTitle = $"{AppBranding.ApplicationName} - Desktop";
        AppBranding.SplashTitle = AppBranding.ApplicationName;

        IThemePreferenceService themePreferenceService = new FileThemePreferenceService();

        using ErpSplashForm splash = new();
        splash.Show();
        Stopwatch splashTimer = Stopwatch.StartNew();
        splash.UpdateStatus("Cargando preferencias de apariencia...");
        Application.DoEvents();

        ThemePreference preference = themePreferenceService.Load();
        ApplyTheme(preference);

        splash.UpdateStatus("Construyendo navegación modular...");
        Application.DoEvents();

        NavigationModuleCatalog moduleCatalog = new(themePreferenceService);
        IReadOnlyList<NavigationItemDefinition> items = moduleCatalog.BuildItems();

        splash.UpdateStatus("Iniciando shell principal...");
        Application.DoEvents();

        MainShellForm shell = new(items, themePreferenceService);

        int remaining = UiConstants.SplashMinimumDisplayMilliseconds - (int)splashTimer.ElapsedMilliseconds;
        if (remaining > 0)
        {
            splash.UpdateStatus("Finalizando inicio...");
            DateTime end = DateTime.UtcNow.AddMilliseconds(remaining);
            while (DateTime.UtcNow < end)
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }
        }

        splash.Close();
        Application.Run(shell);
    }

    private static void ApplyTheme(ThemePreference preference)
    {
        if (!string.IsNullOrWhiteSpace(preference.PaletteName))
        {
            UserLookAndFeel.Default.SetSkinStyle(preference.SkinName, preference.PaletteName);
        }
        else
        {
            UserLookAndFeel.Default.SetSkinStyle(preference.SkinName);
        }

        UserLookAndFeel.Default.CompactUIMode = preference.CompactUIMode
            ? DefaultBoolean.True
            : DefaultBoolean.False;

        WindowsFormsSettings.AllowRoundedWindowCorners = preference.RoundedWindowCorners
            ? DefaultBoolean.True
            : DefaultBoolean.False;

        if (!string.IsNullOrWhiteSpace(preference.AccentColorHex))
        {
            System.Drawing.Color accent = System.Drawing.ColorTranslator.FromHtml(preference.AccentColorHex);
            UserLookAndFeel.Default.SkinMaskColor = accent;
            UserLookAndFeel.Default.SkinMaskColor2 = accent;
        }
    }
}
