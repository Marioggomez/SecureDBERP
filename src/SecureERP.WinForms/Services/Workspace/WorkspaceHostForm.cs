using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Services.Workspace;

/// <summary>
/// Formulario MDI que hospeda una página de trabajo y permite al shell gobernar sus comandos.
/// </summary>
public sealed class WorkspaceHostForm : XtraForm
{
    public IWorkspacePage Page { get; }
    private readonly SplitContainerControl _split;
    private readonly IPageLoadingState? _loadingState;

    public WorkspaceHostForm(IWorkspacePage page)
    {
        Page = page;
        _loadingState = page as IPageLoadingState;
        Text = page.Title;
        Tag = page.PageKey;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        WindowState = FormWindowState.Maximized;

        _split = new SplitContainerControl
        {
            Dock = DockStyle.Fill,
            PanelVisibility = SplitPanelVisibility.Panel1,
            SplitterPosition = Width - 340,
            FixedPanel = SplitFixedPanel.Panel2
        };

        _split.Panel1.Controls.Add(page.MainControl);
        page.MainControl.Dock = DockStyle.Fill;

        if (page.AuxiliaryPanel is not null)
        {
            _split.Panel2.Controls.Add(page.AuxiliaryPanel);
            page.AuxiliaryPanel.Dock = DockStyle.Fill;
            _split.PanelVisibility = SplitPanelVisibility.Both;
        }

        Controls.Add(_split);

        if (_loadingState is not null)
        {
            _loadingState.LoadingStateChanged += (_, _) => ApplyLoadingState();
            ApplyLoadingState();
        }
    }

    public void BuildRibbon(RibbonPage page) => Page.BuildRibbon(page);
    public void OnActivated() => Page.OnActivated();

    private void ApplyLoadingState()
    {
        if (_loadingState is null)
        {
            return;
        }

        UseWaitCursor = _loadingState.IsBusy;
    }
}
