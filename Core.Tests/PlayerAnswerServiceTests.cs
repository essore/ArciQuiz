using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class PlayerAnswerServiceTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 18, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task InviaAsync_PrimaRisposta_ConfermaESalvaUnaSolaOccorrenza()
    {
        await using var test = await TestDatabase.CreateAsync(NowUtc.AddSeconds(20));

        var result = await PlayerAnswerService.InviaAsync(
            test.Database, test.PlayerId, test.SessionToken, test.PartitaId, test.QuestionId, 'b', new FixedTimeProvider(NowUtc));

        Assert.True(result.IsAccepted);
        Assert.False(result.AlreadySubmitted);
        Assert.Equal('B', result.RecordedAnswer);
        var answer = await test.Database.ManchesRisposte.SingleAsync();
        Assert.Equal('B', answer.Risposta);
        Assert.Equal(NowUtc.UtcDateTime, answer.DtRisposta);
    }

    [Fact]
    public async Task InviaAsync_RetryConRispostaDiversa_MantieneLaPrimaRisposta()
    {
        await using var test = await TestDatabase.CreateAsync(NowUtc.AddSeconds(20));
        var clock = new FixedTimeProvider(NowUtc);

        await PlayerAnswerService.InviaAsync(
            test.Database, test.PlayerId, test.SessionToken, test.PartitaId, test.QuestionId, 'A', clock);
        var retry = await PlayerAnswerService.InviaAsync(
            test.Database, test.PlayerId, test.SessionToken, test.PartitaId, test.QuestionId, 'D', clock);

        Assert.True(retry.IsAccepted);
        Assert.True(retry.AlreadySubmitted);
        Assert.Equal('A', retry.RecordedAnswer);
        Assert.Equal(1, await test.Database.ManchesRisposte.CountAsync());
    }

    [Fact]
    public async Task InviaAsync_InviiConcorrenti_ConfermaUnaSolaRisposta()
    {
        await using var test = await TestDatabase.CreateAsync(NowUtc.AddSeconds(20));
        var clock = new FixedTimeProvider(NowUtc);
        await using var firstDatabase = new ArciQuizDbContext(test.Options);
        await using var secondDatabase = new ArciQuizDbContext(test.Options);

        var results = await Task.WhenAll(
            PlayerAnswerService.InviaAsync(firstDatabase, test.PlayerId, test.SessionToken, test.PartitaId, test.QuestionId, 'A', clock),
            PlayerAnswerService.InviaAsync(secondDatabase, test.PlayerId, test.SessionToken, test.PartitaId, test.QuestionId, 'D', clock));

        Assert.All(results, result => Assert.True(result.IsAccepted));
        Assert.Single(await test.Database.ManchesRisposte.ToListAsync());
    }

    [Fact]
    public async Task InviaAsync_SessioneSostituita_RifiutaLaRisposta()
    {
        await using var test = await TestDatabase.CreateAsync(NowUtc.AddSeconds(20));

        var result = await PlayerAnswerService.InviaAsync(
            test.Database, test.PlayerId, "token-precedente", test.PartitaId, test.QuestionId, 'A', new FixedTimeProvider(NowUtc));

        Assert.False(result.IsAccepted);
        Assert.False(result.SessionValid);
        Assert.Empty(test.Database.ManchesRisposte);
    }

    [Fact]
    public async Task InviaAsync_AlMomentoDellaScadenza_RifiutaEChiudeLaDomanda()
    {
        await using var test = await TestDatabase.CreateAsync(NowUtc);

        var result = await PlayerAnswerService.InviaAsync(
            test.Database, test.PlayerId, test.SessionToken, test.PartitaId, test.QuestionId, 'A', new FixedTimeProvider(NowUtc));

        Assert.False(result.IsAccepted);
        Assert.Equal(GamePhase.ShowingAnswers, (await test.Database.Partite.SingleAsync()).Fase);
        var abstention = Assert.Single(await test.Database.ManchesRisposte.ToListAsync());
        Assert.True(abstention.IsAstenuto);
        Assert.Null(abstention.Risposta);
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
        public int QuestionId { get; private set; }
        public int PlayerId { get; private set; }
        public string SessionToken { get; } = "sessione-corrente";

        public static async Task<TestDatabase> CreateAsync(DateTimeOffset deadlineUtc)
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-player-answer-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new TestDatabase(path, database, options);

            var partita = new Partita { Titolo = "Risposte test", Stato = PartitaStato.InCorso, Fase = GamePhase.ShowingQuestion };
            var manche = new Manche { Partita = partita, Ordine = 1 };
            var question = new MancheDomanda
            {
                Manche = manche,
                Index = 1,
                DtStart = NowUtc.UtcDateTime,
                ScadenzaUtc = deadlineUtc,
                Domanda = new Domanda
                {
                    Testo = "Domanda",
                    RispostaA = "A",
                    RispostaB = "B",
                    RispostaC = "C",
                    RispostaD = "D",
                    RispostaEsatta = 'A'
                }
            };
            var player = new Player
            {
                Partita = partita,
                NomeSquadra = "Falchi",
                Password = "segreta",
                SessionToken = test.SessionToken
            };
            var registration = new PlayerPartita
            {
                Player = player,
                Partita = partita,
                SessionToken = test.SessionToken
            };

            database.AddRange(partita, manche, question, player, registration);
            await database.SaveChangesAsync();
            partita.CurrentMancheId = manche.Id;
            partita.CurrentMancheDomandaId = question.Id;
            await database.SaveChangesAsync();
            test.PartitaId = partita.Id;
            test.QuestionId = question.Id;
            test.PlayerId = player.Id;
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
