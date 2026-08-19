using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public sealed record LeaderboardTeamScore(int PlayerId, string TeamName, int Points);

public sealed record LeaderboardEntry(int Position, string TeamName, int Points);

public sealed record LeaderboardView(IReadOnlyList<LeaderboardEntry> Entries);

public static class LeaderboardService
{
    // Ricostruisce la classifica dai dettagli persistiti, ignorando le occorrenze annullate.
    public static async Task<LeaderboardView> LoadAsync(
        ArciQuizDbContext database,
        int partitaId,
        CancellationToken cancellationToken = default)
    {
        var teams = await database.Players
            .AsNoTracking()
            .Where(item => item.PartitaId == partitaId)
            .Select(item => new LeaderboardTeamScore(item.Id, item.NomeSquadra, 0))
            .ToListAsync(cancellationToken);

        var pointsByPlayer = await database.ManchesRisposte
            .AsNoTracking()
            .Where(item => !item.IsAnnullata
                && !item.MancheDomanda!.IsAnnullata
                && item.MancheDomanda.Manche!.PartitaId == partitaId)
            .GroupBy(item => item.PlayerId)
            .Select(group => new { PlayerId = group.Key, Points = group.Sum(item => item.PuntiAssegnati) })
            .ToDictionaryAsync(item => item.PlayerId, item => item.Points, cancellationToken);

        var scores = teams
            .Select(team => team with { Points = pointsByPlayer.GetValueOrDefault(team.PlayerId) })
            .ToList();

        return Create(scores);
    }

    public static LeaderboardView Create(IEnumerable<LeaderboardTeamScore> scores)
    {
        var orderedScores = scores
            .OrderByDescending(item => item.Points)
            .ThenBy(item => item.TeamName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var entries = new List<LeaderboardEntry>(orderedScores.Count);
        int? previousPoints = null;
        var position = 0;

        for (var index = 0; index < orderedScores.Count; index++)
        {
            var score = orderedScores[index];
            if (previousPoints != score.Points)
                position = index + 1;

            entries.Add(new LeaderboardEntry(position, score.TeamName, score.Points));
            previousPoints = score.Points;
        }

        return new LeaderboardView(entries);
    }
}
