using System.Net.Http.Headers;
using System.Net.Http.Json;
using SecureERP.Desktop.Core.Abstractions;
using SecureERP.Desktop.Core.Contracts.Catalog;
using SecureERP.Desktop.Infrastructure.SecureApi.Configuration;

namespace SecureERP.Desktop.Infrastructure.SecureApi.Catalog;

public sealed class SecureErpCountriesApiClient(
    HttpClient httpClient,
    SecureErpApiOptions options,
    ISessionContext sessionContext) : ICountriesApiClient
{
    public async Task<IReadOnlyList<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, options.CountriesPath);
        var token = sessionContext.Current?.AccessToken;

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<List<CountryDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return payload ?? [];
    }
}