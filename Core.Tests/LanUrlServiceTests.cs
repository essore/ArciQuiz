using Microsoft.Extensions.Configuration;
using Web.Services;

namespace Core.Tests;

public class LanUrlServiceTests
{
    [Fact]
    public void BuildUrl_UsesConfiguredLanEndpointWithoutHardcodedPort()
    {
        var configuration = CreateConfiguration(("Kestrel:Endpoints:HttpLan:Url", "http://0.0.0.0:6123"));
        var service = new LanUrlService(configuration, new FakeLanAddressService("192.168.1.40"));

        var result = service.BuildUrl("registrazione");

        Assert.Equal("http://192.168.1.40:6123/registrazione", result);
    }

    [Fact]
    public void BuildUrl_PrefersConfiguredPublicBaseUrl()
    {
        var configuration = CreateConfiguration(
            ("Lobby:PublicBaseUrl", "https://quiz.example:7443/base/"),
            ("Kestrel:Endpoints:HttpLan:Url", "http://0.0.0.0:5000"));
        var service = new LanUrlService(configuration, new FakeLanAddressService(null));

        var result = service.BuildUrl("/registrazione");

        Assert.Equal("https://quiz.example:7443/registrazione", result);
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values) => new ConfigurationBuilder()
        .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value)))
        .Build();

    private sealed class FakeLanAddressService(string? address) : ILanAddressService
    {
        public string? GetLanIPv4() => address;
    }
}
