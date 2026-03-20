using System.Threading;

namespace SecureERP.WinForms.Services.Workspace;

public interface ICardWorkspacePage<T> : IWorkspacePage where T : class
{
    T? CurrentItem { get; }
    bool IsEditing { get; }

    Task LoadAsync(T? item, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task CancelAsync();
}
