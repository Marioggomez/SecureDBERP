namespace SecureERP.WinForms.Services.Workspace;

public interface IPageLoadingState
{
    bool IsBusy { get; }
    string? BusyMessage { get; }

    event EventHandler? LoadingStateChanged;
}
