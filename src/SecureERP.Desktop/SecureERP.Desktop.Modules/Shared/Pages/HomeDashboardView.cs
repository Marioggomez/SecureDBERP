using DevExpress.XtraEditors;

namespace SecureERP.Desktop.Modules.Shared.Pages;

public sealed class HomeDashboardView : XtraUserControl
{
    public HomeDashboardView()
    {
        Dock = DockStyle.Fill;

        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        };

        var title = new LabelControl
        {
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.None,
            Height = 44,
            Padding = new Padding(16, 14, 0, 0),
            Text = "Role Center"
        };

        var subtitle = new LabelControl
        {
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.None,
            Height = 34,
            Padding = new Padding(16, 6, 0, 0),
            Text = "Shell base SecureERP listo para modulos operativos."
        };

        panel.Controls.Add(subtitle);
        panel.Controls.Add(title);
        Controls.Add(panel);
    }
}