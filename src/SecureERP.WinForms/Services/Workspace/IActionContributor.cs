using DevExpress.XtraBars.Ribbon;

namespace SecureERP.WinForms.Services.Workspace;

public interface IActionContributor
{
    void BuildRibbon(RibbonPage page);
}
