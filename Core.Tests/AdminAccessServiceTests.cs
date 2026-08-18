using Microsoft.AspNetCore.Http;
using Web.Services;

namespace Core.Tests;

public class AdminAccessServiceTests
{
    private static readonly AdminCredentials Credentials = new()
    {
        Username = "admin",
        Password = "password-di-prova"
    };

    [Theory]
    [InlineData("/admin", true)]
    [InlineData("/domande", true)]
    [InlineData("/partita/1", true)]
    [InlineData("/manche/1/domande", true)]
    [InlineData("/regia/1", true)]
    [InlineData("/api/domande/export", true)]
    [InlineData("/proiettore/1", false)]
    [InlineData("/registrazione", false)]
    public void RequiresAuthentication_ProtectsOnlyAdminPaths(string path, bool expected)
    {
        Assert.Equal(expected, AdminAccessService.RequiresAuthentication(new PathString(path)));
    }

    [Fact]
    public void IsValid_AcceptsOnlyConfiguredCredentials()
    {
        Assert.True(AdminAccessService.IsValid(Credentials, "admin", "password-di-prova"));
        Assert.False(AdminAccessService.IsValid(Credentials, "admin", "errata"));
        Assert.False(AdminAccessService.IsValid(Credentials, "altro", "password-di-prova"));
    }
}
