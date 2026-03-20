using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraWaitForm;

namespace SecureERP.WinForms.Infrastructure.Ui;

/// <summary>
/// Panel reutilizable para mostrar estado de carga no bloqueante.
/// </summary>
public sealed class BusyIndicatorPanel : XtraUserControl
{
    private readonly ProgressPanel _progress;
    private readonly LabelControl _message;

    public BusyIndicatorPanel()
    {
        Dock = DockStyle.Fill;
        BorderStyle = BorderStyle.None;
        BackColor = Color.FromArgb(245, 245, 245);

        _progress = new ProgressPanel
        {
            Caption = "Procesando",
            Description = "Trabajando...",
            Visible = false
        };

        _message = new LabelControl
        {
            Text = string.Empty,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Visible = false
        };

        Controls.Add(_progress);
        Controls.Add(_message);

        Resize += (_, _) => LayoutControls();
    }

    public void ShowBusy(string message)
    {
        _message.Text = message;
        _message.Visible = true;
        _progress.Visible = true;
        _progress.BringToFront();
        _message.BringToFront();
        Application.UseWaitCursor = true;
        LayoutControls();
    }

    public void HideBusy()
    {
        _message.Visible = false;
        _progress.Visible = false;
        Application.UseWaitCursor = false;
    }

    private void LayoutControls()
    {
        int centerX = Width / 2;
        int centerY = Height / 2;

        _progress.Left = Math.Max(8, centerX - (_progress.Width / 2));
        _progress.Top = Math.Max(8, centerY - (_progress.Height / 2) - 12);

        _message.Left = Math.Max(8, centerX - (_message.Width / 2));
        _message.Top = _progress.Bottom + 8;
    }
}
