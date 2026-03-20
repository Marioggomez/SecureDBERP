namespace SecureERP.WinForms.Services.Workspace;

public interface IWorkspaceContextPublisher
{
    string? EntityType { get; }
    long? EntityId { get; }
    bool HasSelection { get; }

    event EventHandler? ContextChanged;
}
