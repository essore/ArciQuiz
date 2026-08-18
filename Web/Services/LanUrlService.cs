namespace Web.Services;

public interface ILanUrlService
{
    string? BuildUrl(string relativePath);
}

public sealed class LanUrlService(IConfiguration configuration, ILanAddressService lan) : ILanUrlService
{
    public string? BuildUrl(string relativePath)
    {
        var configuredBaseUrl = configuration["Lobby:PublicBaseUrl"];
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var publicBaseUrl))
            return new Uri(publicBaseUrl, NormalizePath(relativePath)).ToString();

        var lanAddress = lan.GetLanIPv4();
        var endpointUrl = configuration["Kestrel:Endpoints:HttpLan:Url"];
        if (string.IsNullOrWhiteSpace(lanAddress)
            || !Uri.TryCreate(endpointUrl, UriKind.Absolute, out var endpoint))
            return null;

        var builder = new UriBuilder(endpoint) { Host = lanAddress };
        return new Uri(builder.Uri, NormalizePath(relativePath)).ToString();
    }

    private static string NormalizePath(string relativePath) => relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";
}
