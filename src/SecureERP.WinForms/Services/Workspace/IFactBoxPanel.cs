namespace SecureERP.WinForms.Services.Workspace;

public interface IFactBoxPanel
{
    string? EntityType { get; set; }
    long? EntityId { get; set; }

    void BindContext(string? entityType, long? entityId);
}
