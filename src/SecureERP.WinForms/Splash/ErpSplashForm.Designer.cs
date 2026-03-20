#nullable disable
using DevExpress.XtraEditors;
using DevExpress.XtraWaitForm;

namespace SecureERP.WinForms.Splash;

partial class ErpSplashForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _statusLabel = new LabelControl();
        marqueeProgressBarControl1 = new MarqueeProgressBarControl();
        _rootPanel = new PanelControl();
        ((System.ComponentModel.ISupportInitialize)marqueeProgressBarControl1.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_rootPanel).BeginInit();
        _rootPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _statusLabel
        // 
        _statusLabel.Appearance.Font = new Font("Segoe UI", 9F);
        _statusLabel.Appearance.Options.UseFont = true;
        _statusLabel.Location = new Point(31, 202);
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(58, 15);
        _statusLabel.TabIndex = 1;
        _statusLabel.Text = "Iniciando...";
        // 
        // marqueeProgressBarControl1
        // 
        marqueeProgressBarControl1.EditValue = 0;
        marqueeProgressBarControl1.Location = new Point(4, 252);
        marqueeProgressBarControl1.Name = "marqueeProgressBarControl1";
        marqueeProgressBarControl1.Size = new Size(554, 6);
        marqueeProgressBarControl1.TabIndex = 4;
        // 
        // _rootPanel
        // 
        _rootPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _rootPanel.Controls.Add(marqueeProgressBarControl1);
        _rootPanel.Controls.Add(_statusLabel);
        _rootPanel.Location = new Point(0, 0);
        _rootPanel.Name = "_rootPanel";
        _rootPanel.Padding = new Padding(28);
        _rootPanel.Size = new Size(560, 300);
        _rootPanel.TabIndex = 0;
        // 
        // ErpSplashForm
        // 
        AutoScaleDimensions = new SizeF(6F, 13F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(560, 262);
        Controls.Add(_rootPanel);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ErpSplashForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        ((System.ComponentModel.ISupportInitialize)marqueeProgressBarControl1.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_rootPanel).EndInit();
        _rootPanel.ResumeLayout(false);
        _rootPanel.PerformLayout();
        ResumeLayout(false);
    }
    private LabelControl _statusLabel;
    private MarqueeProgressBarControl marqueeProgressBarControl1;
    private PanelControl _rootPanel;
}
