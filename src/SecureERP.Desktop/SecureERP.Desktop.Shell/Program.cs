using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using Microsoft.Extensions.DependencyInjection;
using SecureERP.Desktop.Shell.Bootstrap;
using SecureERP.Desktop.Shell.Shell;

namespace SecureERP.Desktop.Shell;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        UserLookAndFeel.Default.SetSkinStyle("Office 2019 Colorful");
        WindowsFormsSettings.DefaultFont = new Font("Segoe UI", 9F);

        var services = DesktopServiceConfigurator.CreateServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();

        using var login = ActivatorUtilities.CreateInstance<LoginForm>(serviceProvider);
        if (login.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        Application.Run(ActivatorUtilities.CreateInstance<MainShellForm>(serviceProvider));
    }
}