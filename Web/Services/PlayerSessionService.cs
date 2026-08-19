using Core.Entities;
using Infrasctructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Web.Services;

public static class PlayerSessionAuthentication
{
    public const string SelectorScheme = "ArciQuiz.Autenticazione";
    public const string Scheme = "ArciQuiz.Squadra";
    public const string CookieName = "ArciQuiz.Squadra";
    public const string AuthorizationPolicy = "ArciQuiz.Squadra.Autenticata";
    public const string PlayerIdClaimType = "arciquiz:player-id";
    public const string SessionTokenClaimType = "arciquiz:session-token";
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(12);

    public static string SelectScheme(PathString path, bool hasPlayerCookie = false) =>
        path.StartsWithSegments("/squadra")
            || (path.StartsWithSegments("/_blazor") && hasPlayerCookie)
            ? Scheme
            : CookieAuthenticationDefaults.AuthenticationScheme;

    public static ClaimsPrincipal CreatePrincipal(int playerId, string sessionToken)
    {
        var claims = new[]
        {
            new Claim(PlayerIdClaimType, playerId.ToString()),
            new Claim(SessionTokenClaimType, sessionToken)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
    }

    public static AuthenticationProperties CreatePersistentProperties(TimeProvider timeProvider) => new()
    {
        IsPersistent = true,
        AllowRefresh = true,
        ExpiresUtc = timeProvider.GetUtcNow().Add(SessionDuration)
    };
}

public static class PlayerSessionService
{
    // Crea una sola sessione persistita per squadra, sostituendo quella del dispositivo precedente.
    public static async Task<PlayerSessionOperationResult> AccediAsync(
        ArciQuizDbContext db,
        int? partitaId,
        string? nomeSquadra,
        string? password)
    {
        var nome = nomeSquadra?.Trim();
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(password))
            return PlayerSessionOperationResult.Error("Inserisci nome squadra e password.");

        var partita = partitaId.HasValue
            ? await db.Partite.FirstOrDefaultAsync(item => item.Id == partitaId.Value)
            : await db.Partite
                .Where(item => item.IsAttiva
                    && (item.Stato == Core.Enums.PartitaStato.Pronta || item.Stato == Core.Enums.PartitaStato.InCorso))
                .FirstOrDefaultAsync();
        if (partita is null)
            return PlayerSessionOperationResult.Error("Non è disponibile una partita per l'accesso.");

        var squadra = await db.Players
            .Include(item => item.IscrizioniPartite)
            .FirstOrDefaultAsync(item => item.PartitaId == partita.Id && item.NomeSquadra == nome);
        if (squadra is null || squadra.Password != password)
            return PlayerSessionOperationResult.Error("Nome squadra o password non validi.");

        var sessione = squadra.IscrizioniPartite.SingleOrDefault(item => item.PartitaId == partita.Id);
        if (sessione is null)
            return PlayerSessionOperationResult.Error("La squadra non è associata alla partita selezionata.");

        var token = Guid.NewGuid().ToString("N");
        var accessoUtc = DateTime.UtcNow;
        squadra.SessionToken = token;
        squadra.DtUltimoAccessoUtc = accessoUtc;
        sessione.SessionToken = token;
        sessione.DtUltimoAccessoUtc = accessoUtc;
        await db.SaveChangesAsync();

        return PlayerSessionOperationResult.Success(squadra.Id, token, "Accesso effettuato.");
    }

    public static async Task<bool> SessioneValidaAsync(ArciQuizDbContext db, int squadraId, string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        return await db.PlayersPartite.AnyAsync(item =>
            item.PlayerId == squadraId &&
            item.SessionToken == token &&
            item.Player != null &&
            item.Player.SessionToken == token);
    }
}

public sealed record PlayerSessionOperationResult(bool IsSuccess, int? SquadraId, string? SessionToken, string Messaggio)
{
    public static PlayerSessionOperationResult Success(int squadraId, string sessionToken, string messaggio) => new(true, squadraId, sessionToken, messaggio);
    public static PlayerSessionOperationResult Error(string messaggio) => new(false, null, null, messaggio);
}
