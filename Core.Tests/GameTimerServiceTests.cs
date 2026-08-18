using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class GameTimerServiceTests
{
    private static readonly DateTimeOffset StartUtc = new(2026, 8, 18, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AvviaPartita_UsaDurataStandardDellaManche()
    {
        await using var test = await TestDatabase.CreateAsync(20, null);
        var clock = new ManualTimeProvider(StartUtc);

        await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId, clock);
        var state = await PartitaStateMachineService.CaricaStatoAsync(test.Database, test.PartitaId, clock);

        Assert.Equal(StartUtc.AddSeconds(20), state!.PhaseEndsAtUtc);
        Assert.Equal(20, state.SecondsRemainingHint);
        Assert.True(state.AcceptingAnswers);
    }

    [Fact]
    public async Task AvviaPartita_OverrideDomandaSostituisceDurataStandard()
    {
        await using var test = await TestDatabase.CreateAsync(20, 35);
        var clock = new ManualTimeProvider(StartUtc);

        await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId, clock);

        var question = await test.Database.ManchesDomande.AsNoTracking().SingleAsync();
        Assert.Equal(StartUtc.AddSeconds(35), question.ScadenzaUtc);
    }

    [Fact]
    public async Task VerificaRisposta_PrimaDellaScadenzaAccetta_AlLimiteRifiutaEChiude()
    {
        await using var test = await TestDatabase.CreateAsync(20, null);
        var clock = new ManualTimeProvider(StartUtc);
        await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId, clock);

        clock.Advance(TimeSpan.FromMilliseconds(19_999));
        var accepted = await GameTimerService.VerificaRispostaAsync(
            test.Database,
            test.PartitaId,
            test.QuestionId,
            clock);
        Assert.True(accepted.IsAccepted);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        var rejected = await GameTimerService.VerificaRispostaAsync(
            test.Database,
            test.PartitaId,
            test.QuestionId,
            clock);

        Assert.False(rejected.IsAccepted);
        Assert.Contains("non accetta più risposte", rejected.Messaggio);
        var partita = await test.Database.Partite.AsNoTracking().SingleAsync();
        var question = await test.Database.ManchesDomande.AsNoTracking().SingleAsync();
        Assert.Equal(GamePhase.ShowingAnswers, partita.Fase);
        Assert.Equal(StartUtc.AddSeconds(20).UtcDateTime, question.DtEnd);
    }

    [Fact]
    public async Task RiavvioDopoScadenza_ChiudeLaDomandaPersistita()
    {
        await using var test = await TestDatabase.CreateAsync(10, null);
        var clock = new ManualTimeProvider(StartUtc);
        await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId, clock);
        clock.Advance(TimeSpan.FromSeconds(12));

        await using var reopened = new ArciQuizDbContext(test.Options);
        var changed = await GameTimerService.ChiudiDomandeScaduteAsync(reopened, clock);
        var state = await PartitaStateMachineService.CaricaStatoAsync(reopened, test.PartitaId, clock);

        Assert.Contains(test.PartitaId, changed);
        Assert.Equal(GamePhase.ShowingAnswers, state!.Phase);
        Assert.Equal(0, state.SecondsRemainingHint);
        Assert.False(state.AcceptingAnswers);
    }

    [Fact]
    public async Task AvviaPartita_DurataNonValida_RifiutaIlComando()
    {
        await using var test = await TestDatabase.CreateAsync(0, null);
        var clock = new ManualTimeProvider(StartUtc);

        var result = await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId, clock);

        Assert.False(result.IsSuccess);
        Assert.Null((await test.Database.ManchesDomande.AsNoTracking().SingleAsync()).ScadenzaUtc);
        Assert.Equal(PartitaStato.Pronta, (await test.Database.Partite.AsNoTracking().SingleAsync()).Stato);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
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

        public static async Task<TestDatabase> CreateAsync(int standardDurationSeconds, int? overrideDurationSeconds)
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-timer-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();

            var partita = new Partita { Titolo = "Timer test", Stato = PartitaStato.Pronta, Fase = GamePhase.Lobby };
            var manche = new Manche
            {
                Partita = partita,
                Ordine = 1,
                Stato = MancheStato.Pronta,
                TempoRispostaSecondi = standardDurationSeconds
            };
            var question = new MancheDomanda
            {
                Manche = manche,
                Index = 1,
                DurataSecondiOverride = overrideDurationSeconds,
                Domanda = new Domanda
                {
                    Testo = "Domanda timer",
                    RispostaA = "A",
                    RispostaB = "B",
                    RispostaC = "C",
                    RispostaD = "D",
                    RispostaEsatta = 'A'
                }
            };

            database.AddRange(partita, manche, question);
            await database.SaveChangesAsync();
            return new TestDatabase(path, database, options)
            {
                PartitaId = partita.Id,
                QuestionId = question.Id
            };
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
