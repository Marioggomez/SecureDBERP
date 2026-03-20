namespace SecureERP.Desktop.Core.Contracts.Catalog;

public sealed record CountryDto(
    Guid Id,
    string Code,
    string Name,
    string? Iso3,
    bool IsActive);