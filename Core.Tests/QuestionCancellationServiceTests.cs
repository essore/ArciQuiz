using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class QuestionCancellationServiceTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 19, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AnnullaAsync_PrimaDelSuperamentoSoglia_RicalcolaAstensioneSuccessiva()
    {
        await using var test = await TestDatabase.CreateAsync();
        await test.CalcolaAstensioniAsync();

        var primaRisposta = await test.Database.ManchesRisposte.SingleAsync(item => item.MancheDomandaId == test.PrimaDomandaId);
        var secondaRisposta = await test.Database.ManchesRisposte.SingleAsync(item => item.MancheDomandaId == test.SecondaDomandaId);
        Assert.Equal(0, primaRisposta.PuntiAssegnati);
        Assert.Equal(-500, secondaRisposta.PuntiAssegnati);
        Assert.True(secondaRisposta.AstensionePenalizzata);

        var result = await QuestionCancellationService.AnnullaAsync(
            test.Database,
            test.PartitaId,
            test.PrimaDomandaId,
            "Testo errato",
            new FixedTimeProvider(NowUtc));

        Assert.True(result.Changed);
        var domandaAnnullata = await test.Database.ManchesDomande.Include(item => item.Domanda).SingleAsync(item => item.Id == test.PrimaDomandaId);
        primaRisposta = await test.Database.ManchesRisposte.SingleAsync(item => item.MancheDomandaId == test.PrimaDomandaId);
        secondaRisposta = await test.Database.ManchesRisposte.SingleAsync(item => item.MancheDomandaId == test.SecondaDomandaId);
        Assert.True(domandaAnnullata.IsAnnullata);
        Assert.Equal("Testo errato", domandaAnnullata.MotivoAnnullamento);
        Assert.Equal(NowUtc.UtcDateTime, domandaAnnullata.DtAnnullamentoUtc);
        Assert.True(domandaAnnullata.Domanda!.FlagErrore);
        Assert.True(primaRisposta.IsAnnullata);
        Assert.Equal(0, secondaRisposta.PuntiAssegnati);
        Assert.False(secondaRisposta.AstensionePenalizzata);
        var leaderboard = await LeaderboardService.LoadAsync(test.Database, test.PartitaId);
        Assert.Equal(new LeaderboardEntry(1, "Falchi", 0), Assert.Single(leaderboard.Entries));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AnnullaAsync_DopoIlSuperamentoSoglia_EIdempotente()
    {
        await using var test = await TestDatabase.CreateAsync();
        await test.CalcolaAstensioniAsync();

        var firstResult = await QuestionCancellationService.AnnullaAsync(
            test.Database,
            test.PartitaId,
            test.SecondaDomandaId,
            "Risposta corretta ambigua",
            new FixedTimeProvider(NowUtc));
        var secondResult = await QuestionCancellationService.AnnullaAsync(
            test.Database,
            test.PartitaId,
            test.SecondaDomandaId,
            "Altro motivo",
            new FixedTimeProvider(NowUtc));

        var primaRisposta = await test.Database.ManchesRisposte.SingleAsync(item => item.MancheDomandaId == test.PrimaDomandaId);
        var secondaRisposta = await test.Database.ManchesRisposte.SingleAsync(item => item.MancheDomandaId == test.SecondaDomandaId);
        Assert.True(firstResult.Changed);
        Assert.False(secondResult.Changed);
        Assert.False(primaRisposta.IsAnnullata);
        Assert.Equal(0, primaRisposta.PuntiAssegnati);
        Assert.True(secondaRisposta.IsAnnullata);
        Assert.Equal(-500, secondaRisposta.PuntiAssegnati);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
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
        public int PrimaDomandaId { get; private set; }
        public int SecondaDomandaId { get; private set; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-cancellation-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new TestDatabase(path, database);

            var partita = new Partita { Titolo = "Partita annullamento", Stato = PartitaStato.InCorso, Fase = GamePhase.Leaderboard };
            var manche = new Manche
            {
                Partita = partita,
                Ordine = 1,
                MaxAstensioni = 1,
                MalusBase = 500,
                Moltiplicatore = 1,
                PenalitaErrore = true,
                TempoRispostaSecondi = 20
            };
            var primaDomanda = CreateQuestion(manche, 1, "Prima domanda");
            var secondaDomanda = CreateQuestion(manche, 2, "Seconda domanda");
            var squadra = new Player { Partita = partita, NomeSquadra = "Falchi", Password = "p" };

            database.AddRange(partita, manche, primaDomanda, secondaDomanda, squadra);
            await database.SaveChangesAsync();
            partita.CurrentMancheId = manche.Id;
            partita.CurrentMancheDomandaId = secondaDomanda.Id;
            await database.SaveChangesAsync();

            test.PartitaId = partita.Id;
            test.PrimaDomandaId = primaDomanda.Id;
            test.SecondaDomandaId = secondaDomanda.Id;
            return test;
        }

        public async Task CalcolaAstensioniAsync()
        {
            await QuestionScoringService.CalcolaEPersistiAsync(Database, PrimaDomandaId, new FixedTimeProvider(NowUtc));
            await QuestionScoringService.CalcolaEPersistiAsync(Database, SecondaDomandaId, new FixedTimeProvider(NowUtc));
        }

        private static MancheDomanda CreateQuestion(Manche manche, int index, string text) => new()
        {
            Manche = manche,
            Index = index,
            DtStart = NowUtc.UtcDateTime,
            DtEnd = NowUtc.UtcDateTime,
            ScadenzaUtc = NowUtc,
            Domanda = new Domanda
            {
                Testo = text,
                RispostaA = "A",
                RispostaB = "B",
                RispostaC = "C",
                RispostaD = "D",
                RispostaEsatta = 'A'
            }
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
