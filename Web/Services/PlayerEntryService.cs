using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public enum PlayerEntryDestination
{
    NoGame,
    RegistrationOrLogin,
    Login,
    TeamArea
}

public sealed record PlayerEntryResult(PlayerEntryDestination Destination, int? PartitaId);

public static class PlayerEntryService
{
    // Risolve l'unico ingresso pubblico usando prima la sessione e poi lo stato della partita.
    public static async Task<PlayerEntryResult> ResolveAsync(
        ArciQuizDbContext database,
        int? playerId,
        string? sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (playerId.HasValue
            && await PlayerSessionService.SessioneValidaAsync(database, playerId.Value, sessionToken))
        {
            var playerGameId = await database.Players
                .AsNoTracking()
                .Where(item => item.Id == playerId.Value)
                .Select(item => item.PartitaId)
                .FirstOrDefaultAsync(cancellationToken);
            if (playerGameId.HasValue)
                return new PlayerEntryResult(PlayerEntryDestination.TeamArea, playerGameId);
        }

        var game = await database.Partite
            .AsNoTracking()
            .Where(item => item.IsAttiva
                && (item.Stato == PartitaStato.Pronta || item.Stato == PartitaStato.InCorso))
            .Select(item => new { item.Id, item.Stato })
            .FirstOrDefaultAsync(cancellationToken);
        if (game is null)
            return new PlayerEntryResult(PlayerEntryDestination.NoGame, null);

        return game.Stato == PartitaStato.Pronta
            ? new PlayerEntryResult(PlayerEntryDestination.RegistrationOrLogin, game.Id)
            : new PlayerEntryResult(PlayerEntryDestination.Login, game.Id);
    }
}
