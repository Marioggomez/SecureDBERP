namespace SecureERP.Desktop.Infrastructure.SecureApi.Configuration;

public sealed class SecureErpApiOptions
{
    public Uri BaseUri { get; set; } = new("https://localhost:5001");

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public string LoginPath { get; set; } = "/api/iam/login";

    public string CountriesPath { get; set; } = "/api/catalog/countries";
}