namespace SecureERP.Application.Modules.Organization.DTOs;

public sealed record CreateOrganizationUnitResponse(
    bool IsSuccess,
    long? UnitId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static CreateOrganizationUnitResponse Success(long unitId) => new(true, unitId, null, null);

    public static CreateOrganizationUnitResponse Failure(string errorCode, string errorMessage) =>
        new(false, null, errorCode, errorMessage);
}
