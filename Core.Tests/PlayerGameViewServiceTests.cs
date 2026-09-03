using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class PlayerGameViewServiceTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 18, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadAsync_ShowingQuestion_ProvidesOptionsWithoutSolution()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingQuestion, NowUtc.AddSeconds(20));

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.True(result.SessionValid);
        Assert.Equal(PlayerGameScreen.Question, result.View!.Screen);
        Assert.Equal(4, result.View.Options.Count);
        Assert.Null(result.View.CorrectAnswer);
        Assert.Equal(NowUtc.AddSeconds(20), result.View.DeadlineUtc);
        Assert.Equal(20, result.View.DurationSeconds);
    }

    [Fact]
    public async Task LoadAsync_ExpiredQuestion_StopsInputWithoutExposingSolution()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingQuestion, NowUtc);

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(PlayerGameScreen.TimeExpired, result.View!.Screen);
        Assert.Null(result.View.CorrectAnswer);
        Assert.Null(result.View.DeadlineUtc);
    }

    [Fact]
    public async Task LoadAsync_ShowingQuestion_UsesQuestionDurationOverrideForCountdown()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingQuestion, NowUtc.AddSeconds(45), durationOverrideSeconds: 45);

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(PlayerGameScreen.Question, result.View!.Screen);
        Assert.Equal(45, result.View.DurationSeconds);
    }

    [Fact]
    public async Task LoadAsync_ShowingAnswers_ExposesSolution()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingAnswers, NowUtc);
        test.Database.ManchesRisposte.Add(new MancheRispostaRicevuta
        {
            PlayerId = test.PlayerId,
            MancheDomandaId = test.QuestionId,
            Risposta = 'B',
            IsCorrect = true,
            PuntiAssegnati = 1500
        });
        await test.Database.SaveChangesAsync();

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(PlayerGameScreen.Solution, result.View!.Screen);
        Assert.Equal('B', result.View.CorrectAnswer);
        Assert.Equal('B', result.View.RecordedAnswer);
        Assert.Equal(new PlayerAnswerFeedback(PlayerAnswerOutcome.Correct, 1500), result.View.AnswerFeedback);
    }

    [Theory]
    [InlineData('A', false, false, -500, PlayerAnswerOutcome.Incorrect)]
    [InlineData('\0', true, false, 0, PlayerAnswerOutcome.Abstained)]
    public async Task LoadAsync_ShowingAnswers_ShowsPersonalFeedback(
        char answer,
        bool isAbstained,
        bool isCorrect,
        int pointsChange,
        PlayerAnswerOutcome expectedOutcome)
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingAnswers, NowUtc);
        test.Database.ManchesRisposte.Add(new MancheRispostaRicevuta
        {
            PlayerId = test.PlayerId,
            MancheDomandaId = test.QuestionId,
            Risposta = answer == '\0' ? null : answer,
            IsAstenuto = isAbstained,
            IsCorrect = isCorrect,
            PuntiAssegnati = pointsChange
        });
        await test.Database.SaveChangesAsync();

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(new PlayerAnswerFeedback(expectedOutcome, pointsChange), result.View!.AnswerFeedback);
        Assert.Equal(answer == '\0' ? null : answer, result.View.RecordedAnswer);
    }

    [Fact]
    public async Task LoadAsync_ShowingAnswers_LateRegisteredTeamHasNoQuestionResult()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.ShowingAnswers, NowUtc);

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(PlayerGameScreen.Solution, result.View!.Screen);
        Assert.Null(result.View.AnswerFeedback);
    }

    [Fact]
    public void CreateAnswerDistribution_CountsOnlySupportedOptions()
    {
        var distribution = ProjectorAnswerDistributionService.Create(['A', 'b', 'B', 'C', 'D', 'D', 'X']);

        Assert.Equal(new ProjectorAnswerDistribution(1, 2, 1, 2), distribution);
    }

    [Fact]
    public void CreateQuestionResults_CountsOutcomesAndReportsFastestAnswerTie()
    {
        var results = ProjectorQuestionResultsService.Create(
        [
            new ProjectorQuestionAnswer("Falchi", false, true, 'B', 1300),
            new ProjectorQuestionAnswer("Aquile", false, false, 'A', 900),
            new ProjectorQuestionAnswer("Lupi", false, true, 'C', 900),
            new ProjectorQuestionAnswer("Orsi", true, false, null, null),
            new ProjectorQuestionAnswer("Volpi", false, true, 'B', 500)
        ]);

        Assert.Equal(3, results.CorrectAnswers);
        Assert.Equal(1, results.IncorrectAnswers);
        Assert.Equal(1, results.Abstentions);
        Assert.NotNull(results.FastestAnswer);
        Assert.False(results.FastestAnswer.IsTie);
        Assert.Equal(["Volpi"], results.FastestAnswer.TeamNames);
        Assert.Equal(500, results.FastestAnswer.ElapsedMilliseconds);
    }

    [Fact]
    public void CreateQuestionResults_ReportsTeamsWithSameFastestTime()
    {
        var results = ProjectorQuestionResultsService.Create(
        [
            new ProjectorQuestionAnswer("Falchi", false, true, 'B', 900),
            new ProjectorQuestionAnswer("Aquile", false, false, 'A', 900),
            new ProjectorQuestionAnswer("Orsi", true, false, null, null)
        ]);

        Assert.NotNull(results.FastestAnswer);
        Assert.True(results.FastestAnswer.IsTie);
        Assert.Equal(["Aquile", "Falchi"], results.FastestAnswer.TeamNames);
    }

    [Theory]
    [InlineData(GamePhase.Leaderboard, PlayerGameScreen.Leaderboard)]
    [InlineData(GamePhase.RoundEnded, PlayerGameScreen.RoundEnded)]
    [InlineData(GamePhase.Waiting, PlayerGameScreen.Waiting)]
    public async Task LoadAsync_NonQuestionPhase_HidesQuestionData(GamePhase phase, PlayerGameScreen expectedScreen)
    {
        await using var test = await TestDatabase.CreateAsync(phase, null);

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(expectedScreen, result.View!.Screen);
        Assert.Null(result.View.QuestionText);
        Assert.Empty(result.View.Options);
        Assert.Null(result.View.CorrectAnswer);
    }

    [Fact]
    public async Task LoadAsync_ClosedGame_ShowsFinalState()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.Closed, null, PartitaStato.Conclusa);

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, test.SessionToken, new FixedTimeProvider(NowUtc));

        Assert.Equal(PlayerGameScreen.GameEnded, result.View!.Screen);
    }

    [Fact]
    public async Task LoadAsync_ReplacedSession_IsRejected()
    {
        await using var test = await TestDatabase.CreateAsync(GamePhase.Waiting, null);

        var result = await PlayerGameViewService.LoadAsync(test.Database, test.PlayerId, "token-precedente", new FixedTimeProvider(NowUtc));

        Assert.False(result.SessionValid);
        Assert.Null(result.View);
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
        public int PlayerId { get; private set; }
        public int QuestionId { get; private set; }
        public string SessionToken { get; } = "sessione-corrente";

        public static async Task<TestDatabase> CreateAsync(
            GamePhase phase,
            DateTimeOffset? deadlineUtc,
            PartitaStato state = PartitaStato.InCorso,
            int? durationOverrideSeconds = null)
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-player-view-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new TestDatabase(path, database);

            var game = new Partita { Titolo = "Serata test", Stato = state, Fase = phase };
            var round = new Manche { Partita = game, Ordine = 1 };
            var question = new MancheDomanda
            {
                Manche = round,
                Index = 3,
                ScadenzaUtc = deadlineUtc,
                DurataSecondiOverride = durationOverrideSeconds,
                Domanda = new Domanda
                {
                    Testo = "Qual è la risposta?",
                    RispostaA = "Alfa",
                    RispostaB = "Beta",
                    RispostaC = "Gamma",
                    RispostaD = "Delta",
                    RispostaEsatta = 'B'
                }
            };
            var player = new Player
            {
                Partita = game,
                NomeSquadra = "Falchi",
                Password = "segreta",
                SessionToken = test.SessionToken
            };
            var registration = new PlayerPartita
            {
                Player = player,
                Partita = game,
                SessionToken = test.SessionToken
            };

            database.AddRange(game, round, question, player, registration);
            await database.SaveChangesAsync();
            game.CurrentMancheId = round.Id;
            game.CurrentMancheDomandaId = question.Id;
            await database.SaveChangesAsync();
            test.PlayerId = player.Id;
            test.QuestionId = question.Id;
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
