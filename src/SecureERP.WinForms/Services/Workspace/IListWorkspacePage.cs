using System.Threading;

namespace SecureERP.WinForms.Services.Workspace;

public interface IListWorkspacePage<T> : IWorkspacePage where T : class
{
    IReadOnlyList<T> Items { get; }
    T? SelectedItem { get; }
    string SearchText { get; set; }
    string? SelectedFilter { get; set; }
    int PageSize { get; set; }
    int CurrentPage { get; }
    int TotalPages { get; }

    Task RefreshAsync(bool resetPage = false, CancellationToken cancellationToken = default);
}
