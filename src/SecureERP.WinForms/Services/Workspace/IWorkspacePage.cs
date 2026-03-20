using DevExpress.XtraBars.Ribbon;
using System.Windows.Forms;

namespace SecureERP.WinForms.Services.Workspace;

/// <summary>
/// Contrato base para una página de trabajo gobernada por el shell ERP.
/// </summary>
public interface IWorkspacePage : IActionContributor, IWorkspaceContextPublisher, IPageLoadingState
{
    string PageKey { get; }
    string Title { get; }
    Control MainControl { get; }
    Control? AuxiliaryPanel { get; }

    /// <summary>
    /// Notifica que la página fue activada por el shell; útil para refrescar comandos o datos.
    /// </summary>
    void OnActivated();
}
