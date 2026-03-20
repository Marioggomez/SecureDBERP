using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Services.Navigation;

public interface IWorkspaceModule
{
    string Key { get; }
    string Caption { get; }
    string Group { get; }
    Func<XtraForm> CreateView { get; }
    bool OpenOnStartup { get; }
}
