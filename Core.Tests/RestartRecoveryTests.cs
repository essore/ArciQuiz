using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class RestartRecoveryTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 19, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RiavvioDallaLobby_RipristinaAttesaESessioneSquadra()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.Lobby, null);
        await test.RestartAsync();

        var state = await PartitaStateMachineService.CaricaStatoAsync(test.Database, test.PartitaId, new FixedTimeProvider(NowUtc));
        var playerView = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(GamePhase.Lobby, state!.Phase);
        Assert.True(playerView.SessionValid);
        Assert.Equal(PlayerGameScreen.Waiting, playerView.View!.Screen);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RiavvioConDomandaAperta_RipristinaTempoESessioneSquadra()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingQuestion, NowUtc.AddSeconds(20));
        await test.RestartAsync();

        var state = await PartitaStateMachineService.CaricaStatoAsync(test.Database, test.PartitaId, new FixedTimeProvider(NowUtc));
        var playerView = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(GamePhase.ShowingQuestion, state!.Phase);
        Assert.Equal(20, state.SecondsRemainingHint);
        Assert.True(state.AcceptingAnswers);
        Assert.True(playerView.SessionValid);
        Assert.Equal(PlayerGameScreen.Question, playerView.View!.Screen);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RiavvioConDomandaScaduta_ChiudeESegnaAstensioniUnaSolaVolta()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingQuestion, NowUtc.AddSeconds(-1));
        var clock = new FixedTimeProvider(NowUtc);
        await test.RestartAsync();

        var firstClose = await GameTimerService.ChiudiDomandeScaduteAsync(test.Database, clock);
        test.Database.ChangeTracker.Clear();
        var repeatedClose = await GameTimerService.ChiudiDomandeScaduteAsync(test.Database, clock);
        var state = await PartitaStateMachineService.CaricaStatoAsync(test.Database, test.PartitaId, clock);
        var answers = await test.Database.ManchesRisposte
            .Where(item => item.MancheDomandaId == test.QuestionId)
            .ToListAsync();

        Assert.Equal([test.PartitaId], firstClose);
        Assert.Empty(repeatedClose);
        Assert.Equal(GamePhase.ShowingAnswers, state!.Phase);
        Assert.All(answers, item =>
        {
            Assert.True(item.IsAstenuto);
            Assert.Equal(0, item.PuntiAssegnati);
        });
        Assert.Equal(2, answers.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RiavvioDuranteSoluzione_MantieneSessioniERicostruisceClassifica()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingAnswers, NowUtc);
        test.Database.ManchesRisposte.AddRange(
            new MancheRispostaRicevuta
            {
                PlayerId = test.PlayerId,
                MancheDomandaId = test.QuestionId,
                Risposta = 'B',
                IsCorrect = true,
                PuntiAssegnati = 1500
            },
            new MancheRispostaRicevuta
            {
                PlayerId = test.SecondPlayerId,
                MancheDomandaId = test.QuestionId,
                Risposta = 'A',
                PuntiAssegnati = -500
            });
        await test.Database.SaveChangesAsync();
        await test.RestartAsync();

        var playerView = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));
        var secondSessionValid = await PlayerSessionService.SessioneValidaAsync(test.Database, test.SecondPlayerId, test.SecondSessionToken);
        var leaderboard = await LeaderboardService.LoadAsync(test.Database, test.PartitaId);

        Assert.True(playerView.SessionValid);
        Assert.True(secondSessionValid);
        Assert.Equal(PlayerGameScreen.Solution, playerView.View!.Screen);
        Assert.Equal(new PlayerAnswerFeedback(PlayerAnswerOutcome.Correct, 1500), playerView.View.AnswerFeedback);
        Assert.Equal(
        [
            new LeaderboardEntry(1, "Falchi", 1500),
            new LeaderboardEntry(2, "Orsi", -500)
        ],
        leaderboard.Entries);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RiavvioDopoFineManche_RipristinaLaFaseSenzaDomandaCorrente()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.RoundEnded, null);
        await test.RestartAsync();

        var state = await PartitaStateMachineService.CaricaStatoAsync(test.Database, test.PartitaId, new FixedTimeProvider(NowUtc));
        var playerView = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(GamePhase.RoundEnded, state!.Phase);
        Assert.Equal(test.RoundId, state.MancheId);
        Assert.Null(state.MancheDomandaId);
        Assert.Equal(PlayerGameScreen.RoundEnded, playerView.View!.Screen);
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

        public ArciQuizDbContext Database { get; private set; }
        public DbContextOptions<ArciQuizDbContext> Options { get; }
        public int PartitaId { get; private set; }
        public int RoundId { get; private set; }
        public int QuestionId { get; private set; }
        public int PlayerId { get; private set; }
        public int SecondPlayerId { get; private set; }
        public string SessionToken { get; } = "sessione-falchi";
        public string SecondSessionToken { get; } = "sessione-orsi";

        // Prepara una fotografia persistita della partita per simulare l'arresto in una fase precisa.
        public static async Task<TestDatabase> CreateAsync(GamePhase phase, DateTimeOffset? deadlineUtc)
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-restart-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new TestDatabase(path, database, options);

            var partita = new Partita
            {
                Titolo = "Partita da riprendere",
                Stato = phase == GamePhase.Lobby ? PartitaStato.Pronta : PartitaStato.InCorso,
                Fase = phase
            };
            var manche = new Manche
            {
                Partita = partita,
                Ordine = 1,
                Stato = phase == GamePhase.RoundEnded ? MancheStato.Conclusa : MancheStato.InGioco
            };
            var domanda = new MancheDomanda
            {
                Manche = manche,
                Index = 1,
                ScadenzaUtc = deadlineUtc,
                DtEnd = phase is GamePhase.ShowingAnswers or GamePhase.RoundEnded ? NowUtc.UtcDateTime : null,
                Domanda = new Domanda
                {
                    Testo = "Quale opzione è corretta?",
                    RispostaA = "Alfa",
                    RispostaB = "Beta",
                    RispostaC = "Gamma",
                    RispostaD = "Delta",
                    RispostaEsatta = 'B'
                }
            };
            var firstPlayer = new Player
            {
                Partita = partita,
                NomeSquadra = "Falchi",
                Password = "password",
                SessionToken = test.SessionToken
            };
            var secondPlayer = new Player
            {
                Partita = partita,
                NomeSquadra = "Orsi",
                Password = "password",
                SessionToken = test.SecondSessionToken
            };

            database.AddRange(partita, manche, domanda, firstPlayer, secondPlayer);
            await database.SaveChangesAsync();

            database.PlayersPartite.AddRange(
                new PlayerPartita { PlayerId = firstPlayer.Id, PartitaId = partita.Id, SessionToken = test.SessionToken },
                new PlayerPartita { PlayerId = secondPlayer.Id, PartitaId = partita.Id, SessionToken = test.SecondSessionToken });
            partita.CurrentMancheId = manche.Id;
            partita.CurrentMancheDomandaId = phase is GamePhase.Lobby or GamePhase.RoundEnded ? null : domanda.Id;
            await database.SaveChangesAsync();

            test.PartitaId = partita.Id;
            test.RoundId = manche.Id;
            test.QuestionId = domanda.Id;
            test.PlayerId = firstPlayer.Id;
            test.SecondPlayerId = secondPlayer.Id;
            return test;
        }

        public async Task RestartAsync()
        {
            await Database.DisposeAsync();
            SqliteConnection.ClearAllPools();
            Database = new ArciQuizDbContext(Options);
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
