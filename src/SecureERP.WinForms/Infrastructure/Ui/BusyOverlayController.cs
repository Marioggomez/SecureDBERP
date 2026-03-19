using DevExpress.XtraEditors;
using DevExpress.XtraWaitForm;

namespace SecureERP.WinForms.Infrastructure.Ui;

public sealed class BusyOverlayController
{
    private readonly PanelControl _overlayHost;
    private readonly LabelControl _messageLabel;
    private readonly ProgressPanel _progressPanel;

    public BusyOverlayController(PanelControl overlayHost, LabelControl messageLabel, ProgressPanel progressPanel)
    {
        _overlayHost = overlayHost;
        _messageLabel = messageLabel;
        _progressPanel = progressPanel;
        Hide();
    }

    public void Show(string message)
    {
        _messageLabel.Text = message;
        _overlayHost.Visible = true;
        _progressPanel.Visible = true;
        _overlayHost.BringToFront();
        Application.UseWaitCursor = true;
    }

    public void Hide()
    {
        _overlayHost.Visible = false;
        _progressPanel.Visible = false;
        Application.UseWaitCursor = false;
    }

    public async Task RunAsync(Func<Task> action, string message)
    {
        Show(message);
        try
        {
            await action();
        }
        finally
        {
            Hide();
        }
    }
}
