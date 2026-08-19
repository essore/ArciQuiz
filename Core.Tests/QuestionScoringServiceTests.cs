using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class QuestionScoringServiceTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 18, 18, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, 20, 1.0)]
    [InlineData(10000, 20, 0.75)]
    [InlineData(20000, 20, 0.5)]
    [InlineData(25000, 20, 0.5)]
    [InlineData(0, 0, 1.0)]
    public void CalcolaCoefficienteTempo_CalcolaCorrettamente(int tempoImpiegatoMs, int durataSecondi, double expectedCoeff)
    {
        var coeff = QuestionScoringService.CalcolaCoefficienteTempo(tempoImpiegatoMs, durataSecondi);
        Assert.Equal(expectedCoeff, coeff, precision: 4);
    }

    [Theory]
    [InlineData(2000, 1, 1, 1.0, 2000)]
    [InlineData(2000, 1, 1, 0.75, 1500)]
    [InlineData(2000, 1, 1, 0.5, 1000)]
    [InlineData(2000, 2, 3, 0.75, 9000)]
    [InlineData(1000, 1, 1, 0.5555, 556)]
    public void CalcolaPuntiCorretti_CalcolaEArrotonda(int puntiBase, int moltiplicatoreManche, int modificatoreDomanda, double coeff, int expectedPunti)
    {
        var punti = QuestionScoringService.CalcolaPuntiCorretti(puntiBase, moltiplicatoreManche, modificatoreDomanda, coeff);
        Assert.Equal(expectedPunti, punti);
    }

    [Theory]
    [InlineData(500, 1, 1, true, -500)]
    [InlineData(500, 2, 3, true, -3000)]
    [InlineData(500, 1, 1, false, 0)]
    public void CalcolaMalus_CalcolaCorrettamente(int malusBase, int moltiplicatoreManche, int modificatoreDomanda, bool penalitaErrore, int expectedMalus)
    {
        var malus = QuestionScoringService.CalcolaMalus(malusBase, moltiplicatoreManche, modificatoreDomanda, penalitaErrore);
        Assert.Equal(expectedMalus, malus);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CalcolaEPersistiAsync_AssegnaPuntiCorrettiErratiEAstensioni()
    {
        await using var test = await ScoringTestDatabase.CreateAsync();

        // Alpha risponde corretto subito
        test.Database.ManchesRisposte.Add(new MancheRispostaRicevuta
        {
            PlayerId = test.PlayerAlphaId,
            MancheDomandaId = test.Question1Id,
            Risposta = 'A',
            TempoImpiegatoMs = 0
        });
        // Beta risponde errato a 10s
        test.Database.ManchesRisposte.Add(new MancheRispostaRicevuta
        {
            PlayerId = test.PlayerBetaId,
            MancheDomandaId = test.Question1Id,
            Risposta = 'B',
            TempoImpiegatoMs = 10000
        });
        // Gamma non risponde
        await test.Database.SaveChangesAsync();

        await QuestionScoringService.CalcolaEPersistiAsync(test.Database, test.Question1Id, new FixedTimeProvider(NowUtc));

        var risposte = await test.Database.ManchesRisposte
            .Where(r => r.MancheDomandaId == test.Question1Id)
            .ToDictionaryAsync(r => r.PlayerId);

        Assert.Equal(3, risposte.Count);

        // Alpha: corretta, 2000 punti, coeff 1.0, non astenuto
        var alpha = risposte[test.PlayerAlphaId];
        Assert.True(alpha.IsCorrect);
        Assert.False(alpha.IsAstenuto);
        Assert.Equal(2000, alpha.PuntiAssegnati);
        Assert.Equal(1.0, alpha.CoefficienteTempo!.Value, precision: 4);

        // Beta: errata, malus -500, non astenuto
        var beta = risposte[test.PlayerBetaId];
        Assert.False(beta.IsCorrect);
        Assert.False(beta.IsAstenuto);
        Assert.Equal(-500, beta.PuntiAssegnati);

        // Gamma: astenuta, 0 punti (soglia 2 astensioni), AstensionePenalizzata false
        var gamma = risposte[test.PlayerGammaId];
        Assert.False(gamma.IsCorrect);
        Assert.True(gamma.IsAstenuto);
        Assert.Equal(0, gamma.PuntiAssegnati);
        Assert.False(gamma.AstensionePenalizzata);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CalcolaEPersistiAsync_SuperamentoSogliaAstensioni_ApplicaMalus()
    {
        await using var test = await ScoringTestDatabase.CreateAsync(maxAstensioni: 1);

        // Domanda 1: Gamma si astiene (prima astensione su MaxAstensioni = 1) -> 0 punti
        await QuestionScoringService.CalcolaEPersistiAsync(test.Database, test.Question1Id, new FixedTimeProvider(NowUtc));
        var gammaQ1 = await test.Database.ManchesRisposte
            .SingleAsync(r => r.MancheDomandaId == test.Question1Id && r.PlayerId == test.PlayerGammaId);

        Assert.True(gammaQ1.IsAstenuto);
        Assert.Equal(0, gammaQ1.PuntiAssegnati);
        Assert.False(gammaQ1.AstensionePenalizzata);

        // Domanda 2: Gamma si astiene di nuovo (seconda astensione, supera MaxAstensioni = 1) -> malus -500 e AstensionePenalizzata true
        await QuestionScoringService.CalcolaEPersistiAsync(test.Database, test.Question2Id, new FixedTimeProvider(NowUtc));
        var gammaQ2 = await test.Database.ManchesRisposte
            .SingleAsync(r => r.MancheDomandaId == test.Question2Id && r.PlayerId == test.PlayerGammaId);

        Assert.True(gammaQ2.IsAstenuto);
        Assert.Equal(-500, gammaQ2.PuntiAssegnati);
        Assert.True(gammaQ2.AstensionePenalizzata);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CalcolaEPersistiAsync_Idempotente_EsecuzioneMultipla()
    {
        await using var test = await ScoringTestDatabase.CreateAsync();

        await QuestionScoringService.CalcolaEPersistiAsync(test.Database, test.Question1Id, new FixedTimeProvider(NowUtc));
        var countFirstRun = await test.Database.ManchesRisposte.CountAsync(r => r.MancheDomandaId == test.Question1Id);

        // Riesecuzione
        await QuestionScoringService.CalcolaEPersistiAsync(test.Database, test.Question1Id, new FixedTimeProvider(NowUtc));
        var countSecondRun = await test.Database.ManchesRisposte.CountAsync(r => r.MancheDomandaId == test.Question1Id);

        Assert.Equal(3, countFirstRun);
        Assert.Equal(countFirstRun, countSecondRun);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class ScoringTestDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private ScoringTestDatabase(string path, ArciQuizDbContext database)
        {
            _path = path;
            Database = database;
        }

        public ArciQuizDbContext Database { get; }
        public int PartitaId { get; private set; }
        public int Question1Id { get; private set; }
        public int Question2Id { get; private set; }
        public int PlayerAlphaId { get; private set; }
        public int PlayerBetaId { get; private set; }
        public int PlayerGammaId { get; private set; }

        public static async Task<ScoringTestDatabase> CreateAsync(int maxAstensioni = 2)
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-scoring-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new ScoringTestDatabase(path, database);

            var partita = new Partita { Titolo = "Partita Scoring", Stato = PartitaStato.InCorso, Fase = GamePhase.ShowingQuestion };
            var manche = new Manche
            {
                Partita = partita,
                Ordine = 1,
                PuntiBase = 2000,
                MalusBase = 500,
                Moltiplicatore = 1,
                TempoRispostaSecondi = 20,
                MaxAstensioni = maxAstensioni,
                PenalitaErrore = true
            };

            var domanda1 = new Domanda
            {
                Testo = "Domanda 1",
                RispostaA = "A",
                RispostaB = "B",
                RispostaC = "C",
                RispostaD = "D",
                RispostaEsatta = 'A'
            };
            var question1 = new MancheDomanda
            {
                Manche = manche,
                Index = 1,
                ModificatorePunti = 1,
                DtStart = NowUtc.UtcDateTime,
                ScadenzaUtc = NowUtc.AddSeconds(20),
                Domanda = domanda1
            };

            var domanda2 = new Domanda
            {
                Testo = "Domanda 2",
                RispostaA = "A",
                RispostaB = "B",
                RispostaC = "C",
                RispostaD = "D",
                RispostaEsatta = 'C'
            };
            var question2 = new MancheDomanda
            {
                Manche = manche,
                Index = 2,
                ModificatorePunti = 1,
                DtStart = NowUtc.AddSeconds(30).UtcDateTime,
                ScadenzaUtc = NowUtc.AddSeconds(50),
                Domanda = domanda2
            };

            var pAlpha = new Player { Partita = partita, NomeSquadra = "Alpha", Password = "p", SessionToken = "s1" };
            var pBeta = new Player { Partita = partita, NomeSquadra = "Beta", Password = "p", SessionToken = "s2" };
            var pGamma = new Player { Partita = partita, NomeSquadra = "Gamma", Password = "p", SessionToken = "s3" };

            database.AddRange(partita, manche, question1, question2, pAlpha, pBeta, pGamma);
            await database.SaveChangesAsync();

            partita.CurrentMancheId = manche.Id;
            partita.CurrentMancheDomandaId = question1.Id;
            await database.SaveChangesAsync();

            test.PartitaId = partita.Id;
            test.Question1Id = question1.Id;
            test.Question2Id = question2.Id;
            test.PlayerAlphaId = pAlpha.Id;
            test.PlayerBetaId = pBeta.Id;
            test.PlayerGammaId = pGamma.Id;

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
}
