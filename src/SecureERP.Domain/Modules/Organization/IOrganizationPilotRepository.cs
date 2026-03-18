namespace SecureERP.Domain.Modules.Organization;

public interface IOrganizationPilotRepository
{
    Task<IReadOnlyList<OrganizationUnitSnapshot>> ListUnitsAsync(CancellationToken cancellationToken = default);

    Task<long> CreateUnitAsync(
        OrganizationUnitToCreate unit,
        CancellationToken cancellationToken = default);
}
