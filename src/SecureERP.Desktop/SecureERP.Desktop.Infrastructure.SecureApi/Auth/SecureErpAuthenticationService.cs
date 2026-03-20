using System.Net.Http.Json;
using SecureERP.Desktop.Core.Abstractions;
using SecureERP.Desktop.Core.Models;
using SecureERP.Desktop.Infrastructure.SecureApi.Configuration;

namespace SecureERP.Desktop.Infrastructure.SecureApi.Auth;

public sealed class SecureErpAuthenticationService(
    HttpClient httpClient,
    SecureErpApiOptions options,
    ISessionContext sessionContext) : IAuthenticationService
{
    public async Task<SessionInfo> SignInAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new LoginApiRequest(request.Username, request.Password, request.Tenant);

        using var response = await httpClient.PostAsJsonAsync(options.LoginPath, payload, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"Login rechazado por SecureERP API: {(int)response.StatusCode} {response.ReasonPhrase}. {content}");
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("SecureERP API no devolvio un cuerpo de login valido.");

        var session = new SessionInfo(
            UserName: loginResponse.UserName,
            AccessToken: loginResponse.AccessToken,
            RefreshToken: loginResponse.RefreshToken,
            Permissions: loginResponse.Permissions ?? [],
            ExpiresAtUtc: loginResponse.ExpiresAtUtc ?? DateTimeOffset.UtcNow.AddHours(8));

        sessionContext.SetSession(session);
        return session;
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        sessionContext.Clear();
        return Task.CompletedTask;
    }

    private sealed record LoginApiRequest(string Username, string Password, string? Tenant);

    private sealed record LoginApiResponse(
        string UserName,
        string AccessToken,
        string? RefreshToken,
        string[]? Permissions,
        DateTimeOffset? ExpiresAtUtc);
}