using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class LeaderboardServiceTests
{
    [Fact]
    public void Create_OrdinaPerPuntiEMostraLeParitaExAequo()
    {
        var leaderboard = LeaderboardService.Create(
        [
            new LeaderboardTeamScore(1, "Delta", -100),
            new LeaderboardTeamScore(2, "Beta", 50),
            new LeaderboardTeamScore(3, "Alpha", 100),
            new LeaderboardTeamScore(4, "Gamma", 50)
        ]);

        Assert.Equal(
        [
            new LeaderboardEntry(1, "Alpha", 100),
            new LeaderboardEntry(2, "Beta", 50),
            new LeaderboardEntry(2, "Gamma", 50),
            new LeaderboardEntry(4, "Delta", -100)
        ],
        leaderboard.Entries);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LoadAsync_RicostruisceIValoriPersistitiEIgnoraLeOccorrenzeAnnullate()
    {
        await using var test = await TestDatabase.CreateAsync();

        var leaderboard = await LeaderboardService.LoadAsync(test.Database, test.PartitaId);

        Assert.Equal(
        [
            new LeaderboardEntry(1, "Alpha", 100),
            new LeaderboardEntry(1, "Beta", 100),
            new LeaderboardEntry(3, "Gamma", 0)
        ],
        leaderboard.Entries);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private TestDatabase(string path, ArciQuizDbContext database)
        {
            _path = path;
            Database = database;
        }

        public ArciQuizDbContext Database { get; }
        public int PartitaId { get; private set; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-leaderboard-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new TestDatabase(path, database);

            var partita = new Partita { Titolo = "Partita classifica", Stato = PartitaStato.InCorso, Fase = GamePhase.Leaderboard };
            var manche = new Manche { Partita = partita, Ordine = 1 };
            var domandaValida = new MancheDomanda { Manche = manche, Index = 1, Domanda = CreateQuestion("Valida") };
            var domandaAnnullata = new MancheDomanda { Manche = manche, Index = 2, IsAnnullata = true, Domanda = CreateQuestion("Annullata") };
            var domandaConRispostaAnnullata = new MancheDomanda { Manche = manche, Index = 3, Domanda = CreateQuestion("Risposta annullata") };
            var alpha = new Player { Partita = partita, NomeSquadra = "Alpha", Password = "p" };
            var beta = new Player { Partita = partita, NomeSquadra = "Beta", Password = "p" };
            var gamma = new Player { Partita = partita, NomeSquadra = "Gamma", Password = "p" };

            database.AddRange(partita, manche, domandaValida, domandaAnnullata, domandaConRispostaAnnullata, alpha, beta, gamma);
            await database.SaveChangesAsync();
            database.ManchesRisposte.AddRange(
                new MancheRispostaRicevuta { PlayerId = alpha.Id, MancheDomandaId = domandaValida.Id, PuntiAssegnati = 100 },
                new MancheRispostaRicevuta { PlayerId = alpha.Id, MancheDomandaId = domandaAnnullata.Id, PuntiAssegnati = 1000 },
                new MancheRispostaRicevuta { PlayerId = beta.Id, MancheDomandaId = domandaValida.Id, PuntiAssegnati = 100 },
                new MancheRispostaRicevuta { PlayerId = beta.Id, MancheDomandaId = domandaConRispostaAnnullata.Id, PuntiAssegnati = 500, IsAnnullata = true });
            await database.SaveChangesAsync();

            test.PartitaId = partita.Id;
            return test;
        }

        private static Domanda CreateQuestion(string text) => new()
        {
            Testo = text,
            RispostaA = "A",
            RispostaB = "B",
            RispostaC = "C",
            RispostaD = "D",
            RispostaEsatta = 'A'
        };

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(_path);
            File.Delete($"{_path}-shm");
            File.Delete($"{_path}-wal");
        }
    }
}
