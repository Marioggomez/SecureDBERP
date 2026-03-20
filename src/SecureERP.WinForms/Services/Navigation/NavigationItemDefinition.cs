using DevExpress.XtraEditors;
using SecureERP.WinForms.Services.Navigation;

namespace SecureERP.WinForms.Services.Navigation;

public sealed record NavigationItemDefinition(
    string Key,
    string Caption,
    string Group,
    Func<XtraForm> CreateView,
    bool OpenOnStartup = false) : IWorkspaceModule;

