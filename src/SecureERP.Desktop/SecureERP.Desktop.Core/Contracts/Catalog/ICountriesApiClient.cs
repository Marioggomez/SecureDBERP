namespace SecureERP.Desktop.Core.Contracts.Catalog;

public interface ICountriesApiClient
{
    Task<IReadOnlyList<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default);
}