using DevExpress.XtraEditors;
using SecureERP.WinForms.Common;

namespace SecureERP.WinForms.Splash;

public partial class ErpSplashForm : XtraForm
{
    private readonly System.Windows.Forms.Timer _animationTimer;
    private string _baseStatus = "Iniciando...";
    private int _animationTick;

    public ErpSplashForm()
    {
        InitializeComponent();

        //_titleLabel.Text = AppBranding.SplashTitle;
        //_subtitleLabel.Text = AppBranding.ApplicationSubtitle;
        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = 300
        };
        _animationTimer.Tick += (_, _) => AnimateStatus();
        _animationTimer.Start();
        FormClosed += (_, _) => DisposeAnimation();
    }

    public void UpdateStatus(string message)
    {
        _baseStatus = message;
        _animationTick = 0;
        ApplyAnimatedStatus();
    }

    private void AnimateStatus()
    {
        _animationTick++;
        ApplyAnimatedStatus();
    }

    private void ApplyAnimatedStatus()
    {
        int dots = (_animationTick % 3) + 1;
        string suffix = new('.', dots);
        string status = $"{_baseStatus}{suffix}";

        _statusLabel.Text = status;
        _statusLabel.Refresh();
        //_progressPanel.Description = status;
        //_progressPanel.Refresh();
    }

    private void DisposeAnimation()
    {
        _animationTimer.Stop();
        _animationTimer.Dispose();
    }
}
