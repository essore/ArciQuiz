using System.Diagnostics;
using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class FiftyTeamsLoadTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CinquantaSquadre_InvianoInConcorrenza_ChiudonoEDeterminanoLaClassifica()
    {
        await using var test = await TestDatabase.CreateAsync();
        var stopwatch = Stopwatch.StartNew();

        var results = await Task.WhenAll(test.Players.Select(async player =>
        {
            await using var database = new ArciQuizDbContext(test.Options);
            return await PlayerAnswerService.InviaAsync(
                database,
                player.PlayerId,
                player.SessionToken,
                test.PartitaId,
                test.MancheDomandaId,
                'A',
                new FixedTimeProvider(NowUtc));
        }));

        await using (var closingDatabase = new ArciQuizDbContext(test.Options))
        {
            var closedPartite = await GameTimerService.ChiudiDomandeScaduteAsync(
                closingDatabase,
                new FixedTimeProvider(NowUtc.AddSeconds(20)));

            Assert.Equal([test.PartitaId], closedPartite);
        }

        await using var verificationDatabase = new ArciQuizDbContext(test.Options);
        var answers = await verificationDatabase.ManchesRisposte
            .Where(item => item.MancheDomandaId == test.MancheDomandaId)
            .ToListAsync();
        var leaderboard = await LeaderboardService.LoadAsync(verificationDatabase, test.PartitaId);
        stopwatch.Stop();

        Assert.All(results, result => Assert.True(result.IsAccepted, result.Message));
        Assert.Equal(50, answers.Count);
        Assert.All(answers, answer =>
        {
            Assert.Equal('A', answer.Risposta);
            Assert.True(answer.IsCorrect);
            Assert.Equal(2000, answer.PuntiAssegnati);
        });
        Assert.Equal(50, leaderboard.Entries.Count);
        Assert.All(leaderboard.Entries, entry => Assert.Equal(2000, entry.Points));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Flusso da 50 squadre completato in {stopwatch.Elapsed.TotalSeconds:F2} secondi.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private TestDatabase(string path, ArciQuizDbContext database, DbContextOptions<ArciQuizDbContext> options)
        {
            _path = path;
            Database = database;
            Options = options;
        }

        public ArciQuizDbContext Database { get; }
        public DbContextOptions<ArciQuizDbContext> Options { get; }
        public int PartitaId { get; private set; }
        public int MancheDomandaId { get; private set; }
        public IReadOnlyList<TestPlayer> Players { get; private set; } = [];

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-fifty-teams-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new TestDatabase(path, database, options);

            var partita = new Partita { Titolo = "Carico 50 squadre", Stato = PartitaStato.InCorso, Fase = GamePhase.ShowingQuestion };
            var manche = new Manche { Partita = partita, Ordine = 1, TempoRispostaSecondi = 20, PuntiBase = 2000 };
            var domanda = new MancheDomanda
            {
                Manche = manche,
                Index = 1,
                DtStart = NowUtc.UtcDateTime,
                ScadenzaUtc = NowUtc.AddSeconds(20),
                Domanda = new Domanda
                {
                    Testo = "Domanda di carico",
                    RispostaA = "A",
                    RispostaB = "B",
                    RispostaC = "C",
                    RispostaD = "D",
                    RispostaEsatta = 'A'
                }
            };

            database.AddRange(partita, manche, domanda);
            var players = Enumerable.Range(1, 50)
                .Select(number => new Player
                {
                    Partita = partita,
                    NomeSquadra = $"Squadra {number:00}",
                    Password = "test",
                    SessionToken = $"sessione-{number:00}"
                })
                .ToList();
            var registrations = players.Select(player => new PlayerPartita
            {
                Player = player,
                Partita = partita,
                SessionToken = player.SessionToken
            });
            database.AddRange(players);
            database.AddRange(registrations);
            await database.SaveChangesAsync();

            partita.CurrentMancheId = manche.Id;
            partita.CurrentMancheDomandaId = domanda.Id;
            await database.SaveChangesAsync();

            test.PartitaId = partita.Id;
            test.MancheDomandaId = domanda.Id;
            test.Players = players
                .Select(player => new TestPlayer(player.Id, player.SessionToken!))
                .ToList();
            return test;
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(_path);
            File.Delete($"{_path}-shm");
            File.Delete($"{_path}-wal");
        }
    }

    private sealed record TestPlayer(int PlayerId, string SessionToken);
}
