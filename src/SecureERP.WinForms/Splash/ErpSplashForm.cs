using DevExpress.XtraEditors;
using DevExpress.XtraWaitForm;

namespace SecureERP.WinForms.Splash;

public sealed class ErpSplashForm : XtraForm
{
    private readonly LabelControl _statusLabel;

    public ErpSplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 280);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;

        PanelControl root = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(24)
        };

        LabelControl title = new()
        {
            Dock = DockStyle.Top,
            Height = 52,
            Text = "SecureERP",
            Appearance = { Font = new Font("Segoe UI", 28, FontStyle.Bold) }
        };

        LabelControl subtitle = new()
        {
            Dock = DockStyle.Top,
            Height = 36,
            Text = "Cliente Desktop Enterprise",
            Appearance = { Font = new Font("Segoe UI", 11, FontStyle.Regular) }
        };

        ProgressPanel progress = new()
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            Caption = "Inicializando",
            Description = "Preparando shell principal y módulos..."
        };

        _statusLabel = new LabelControl
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Text = "Iniciando..."
        };

        root.Controls.Add(progress);
        root.Controls.Add(_statusLabel);
        root.Controls.Add(subtitle);
        root.Controls.Add(title);

        Controls.Add(root);
    }

    public void UpdateStatus(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.Refresh();
    }
}
