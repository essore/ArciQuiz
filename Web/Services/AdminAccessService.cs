using System.Security.Cryptography;
using System.Text;

namespace Web.Services;

public sealed class AdminCredentials
{
    public const string SectionName = "Admin";

    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public static class AdminAccessService
{
    public static bool IsValid(AdminCredentials credentials, string? username, string? password)
    {
        return !string.IsNullOrWhiteSpace(credentials.Username)
            && !string.IsNullOrWhiteSpace(credentials.Password)
            && Matches(credentials.Username, username)
            && Matches(credentials.Password, password);
    }

    public static bool RequiresAuthentication(PathString path)
    {
        return path == "/"
            || path.StartsWithSegments("/admin")
            || path.StartsWithSegments("/domande")
            || path.StartsWithSegments("/nuova-partita")
            || path.StartsWithSegments("/partita")
            || path.StartsWithSegments("/manche")
            || path.StartsWithSegments("/regia")
            || path.StartsWithSegments("/api/domande");
    }

    private static bool Matches(string expected, string? actual)
    {
        if (actual is null) return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual));
    }
}
