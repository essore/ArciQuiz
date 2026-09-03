using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class PartitaStateMachineServiceTests
{
    [Fact]
    public async Task TransizioniComplete_RispettanoIlFlussoDellaPartita()
    {
        await using var test = await TestDatabase.CreateAsync();

        Assert.True((await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId)).IsSuccess);
        var firstState = await PartitaStateMachineService.CaricaStatoAsync(test.Database, test.PartitaId);
        Assert.Equal(GamePhase.ShowingQuestion, firstState!.Phase);
        Assert.Equal(test.PrimaDomandaId, firstState.MancheDomandaId);

        Assert.True((await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.PrimaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.AvviaDomandaSuccessivaAsync(test.Database, test.PartitaId, test.PrimaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.SecondaDomandaId)).IsSuccess);

        var prematureEnd = await PartitaStateMachineService.ConcludiMancheAsync(test.Database, test.PartitaId, test.PrimaMancheId);
        Assert.False(prematureEnd.IsSuccess);

        Assert.True((await PartitaStateMachineService.MostraClassificaAsync(test.Database, test.PartitaId, test.SecondaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ConcludiMancheAsync(test.Database, test.PartitaId, test.PrimaMancheId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.AvviaMancheSuccessivaAsync(test.Database, test.PartitaId, test.PrimaMancheId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.TerzaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.MostraClassificaAsync(test.Database, test.PartitaId, test.TerzaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ConcludiMancheAsync(test.Database, test.PartitaId, test.SecondaMancheId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ConcludiPartitaAsync(test.Database, test.PartitaId)).IsSuccess);

        var partita = await test.Database.Partite.SingleAsync();
        Assert.Equal(PartitaStato.Conclusa, partita.Stato);
        Assert.Equal(GamePhase.Closed, partita.Fase);
        Assert.False(partita.IsAttiva);
        Assert.NotNull(partita.DtFineUtc);
    }

    [Fact]
    public async Task TransizioneNonValida_NonModificaLoStatoPersistito()
    {
        await using var test = await TestDatabase.CreateAsync();

        var close = await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.PrimaDomandaId);
        var next = await PartitaStateMachineService.AvviaDomandaSuccessivaAsync(test.Database, test.PartitaId, test.PrimaDomandaId);
        var leaderboard = await PartitaStateMachineService.MostraClassificaAsync(test.Database, test.PartitaId, test.PrimaDomandaId);
        var roundEnd = await PartitaStateMachineService.ConcludiMancheAsync(test.Database, test.PartitaId, test.PrimaMancheId);
        var gameEnd = await PartitaStateMachineService.ConcludiPartitaAsync(test.Database, test.PartitaId);

        Assert.False(close.IsSuccess);
        Assert.False(next.IsSuccess);
        Assert.False(leaderboard.IsSuccess);
        Assert.False(roundEnd.IsSuccess);
        Assert.False(gameEnd.IsSuccess);
        var partita = await test.Database.Partite.AsNoTracking().SingleAsync();
        Assert.Equal(PartitaStato.Pronta, partita.Stato);
        Assert.Equal(GamePhase.Lobby, partita.Fase);
        Assert.Null(partita.CurrentMancheDomandaId);
    }

    [Fact]
    public async Task ComandiRipetuti_NonAvanzanoDueVolte()
    {
        await using var test = await TestDatabase.CreateAsync();

        var firstStart = await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId);
        var repeatedStart = await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId);
        Assert.True(firstStart.Changed);
        Assert.False(repeatedStart.Changed);

        var firstClose = await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.PrimaDomandaId);
        var repeatedClose = await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.PrimaDomandaId);
        Assert.True(firstClose.Changed);
        Assert.False(repeatedClose.Changed);

        var firstNext = await PartitaStateMachineService.AvviaDomandaSuccessivaAsync(test.Database, test.PartitaId, test.PrimaDomandaId);
        var repeatedNext = await PartitaStateMachineService.AvviaDomandaSuccessivaAsync(test.Database, test.PartitaId, test.PrimaDomandaId);
        Assert.True(firstNext.Changed);
        Assert.False(repeatedNext.Changed);

        var state = await PartitaStateMachineService.CaricaStatoAsync(test.Database, test.PartitaId);
        Assert.Equal(test.SecondaDomandaId, state!.MancheDomandaId);
        Assert.Equal(GamePhase.ShowingQuestion, state.Phase);
    }

    [Fact]
    public async Task CaricaStato_DopoNuovoDbContext_RecuperaLaFaseCorrente()
    {
        await using var test = await TestDatabase.CreateAsync();
        await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId);
        await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.PrimaDomandaId);

        await using var reopened = new ArciQuizDbContext(test.Options);
        var state = await PartitaStateMachineService.CaricaStatoAsync(reopened, test.PartitaId);

        Assert.NotNull(state);
        Assert.Equal(GamePhase.ShowingAnswers, state.Phase);
        Assert.Equal(test.PrimaMancheId, state.MancheId);
        Assert.Equal(test.PrimaDomandaId, state.MancheDomandaId);
        Assert.False(state.AcceptingAnswers);
    }

    [Fact]
    public async Task AvviaMancheSuccessiva_ConOrdiniDuplicati_SegueLOrdinamentoStabileEPersistito()
    {
        await using var test = await TestDatabase.CreateAsync();
        var secondaManche = await test.Database.Manches.SingleAsync(item => item.Id == test.SecondaMancheId);
        secondaManche.Ordine = 1;

        var terzaManche = new Manche
        {
            PartitaId = test.PartitaId,
            Ordine = 2,
            Stato = MancheStato.Pronta
        };
        var quartaDomanda = TestDatabase.CreateQuestion(terzaManche, 1, "Quarta domanda");
        test.Database.AddRange(terzaManche, quartaDomanda);
        await test.Database.SaveChangesAsync();

        Assert.True((await PartitaStateMachineService.AvviaPartitaAsync(test.Database, test.PartitaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.PrimaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.AvviaDomandaSuccessivaAsync(test.Database, test.PartitaId, test.PrimaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.SecondaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.MostraClassificaAsync(test.Database, test.PartitaId, test.SecondaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ConcludiMancheAsync(test.Database, test.PartitaId, test.PrimaMancheId)).IsSuccess);

        Assert.True((await PartitaStateMachineService.AvviaMancheSuccessivaAsync(test.Database, test.PartitaId, test.PrimaMancheId)).IsSuccess);
        var secondRoundState = await PartitaStateMachineService.CaricaStatoAsync(test.Database, test.PartitaId);
        Assert.Equal(test.SecondaMancheId, secondRoundState!.MancheId);
        Assert.Equal(test.TerzaDomandaId, secondRoundState.MancheDomandaId);

        Assert.True((await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, test.TerzaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.MostraClassificaAsync(test.Database, test.PartitaId, test.TerzaDomandaId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ConcludiMancheAsync(test.Database, test.PartitaId, test.SecondaMancheId)).IsSuccess);
        Assert.True((await PartitaStateMachineService.AvviaMancheSuccessivaAsync(test.Database, test.PartitaId, test.SecondaMancheId)).IsSuccess);

        await using var reopened = new ArciQuizDbContext(test.Options);
        var persistedState = await PartitaStateMachineService.CaricaStatoAsync(reopened, test.PartitaId);
        Assert.Equal(terzaManche.Id, persistedState!.MancheId);
        Assert.Equal(quartaDomanda.Id, persistedState.MancheDomandaId);
        Assert.Equal(GamePhase.ShowingQuestion, persistedState.Phase);

        Assert.True((await PartitaStateMachineService.ChiudiDomandaAsync(test.Database, test.PartitaId, quartaDomanda.Id)).IsSuccess);
        Assert.True((await PartitaStateMachineService.MostraClassificaAsync(test.Database, test.PartitaId, quartaDomanda.Id)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ConcludiMancheAsync(test.Database, test.PartitaId, terzaManche.Id)).IsSuccess);
        Assert.True((await PartitaStateMachineService.ConcludiPartitaAsync(test.Database, test.PartitaId)).IsSuccess);
    }

    [Fact]
    public async Task Migrazione_FasePartitaEsistente_DerivaLoStatoPersistito()
    {
        var path = Path.Combine(Path.GetTempPath(), $"arciquiz-state-migration-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync("20260818082101_AddPersistenceModelExtensions");
            await database.Database.ExecuteSqlRawAsync(
                "INSERT INTO Partite (DtCreazione, Stato, Titolo) VALUES ({0}, {1}, {2})",
                DateTime.UtcNow,
                (int)PartitaStato.Pronta,
                "Lobby esistente");

            await database.Database.MigrateAsync();
            database.ChangeTracker.Clear();

            Assert.Equal(GamePhase.Lobby, (await database.Partite.SingleAsync()).Fase);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(path);
            File.Delete($"{path}-shm");
            File.Delete($"{path}-wal");
        }
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
        public int PrimaMancheId { get; private set; }
        public int SecondaMancheId { get; private set; }
        public int PrimaDomandaId { get; private set; }
        public int SecondaDomandaId { get; private set; }
        public int TerzaDomandaId { get; private set; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-state-machine-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();

            var partita = new Partita { Titolo = "Partita test", Stato = PartitaStato.Pronta, Fase = GamePhase.Lobby, IsAttiva = true };
            var primaManche = new Manche { Partita = partita, Ordine = 1, Stato = MancheStato.Pronta };
            var secondaManche = new Manche { Partita = partita, Ordine = 2, Stato = MancheStato.Pronta };
            var primaDomanda = CreateQuestion(primaManche, 1, "Prima domanda");
            var secondaDomanda = CreateQuestion(primaManche, 2, "Seconda domanda");
            var terzaDomanda = CreateQuestion(secondaManche, 1, "Terza domanda");

            database.Add(partita);
            database.AddRange(primaManche, secondaManche, primaDomanda, secondaDomanda, terzaDomanda);
            await database.SaveChangesAsync();

            return new TestDatabase(path, database, options)
            {
                PartitaId = partita.Id,
                PrimaMancheId = primaManche.Id,
                SecondaMancheId = secondaManche.Id,
                PrimaDomandaId = primaDomanda.Id,
                SecondaDomandaId = secondaDomanda.Id,
                TerzaDomandaId = terzaDomanda.Id
            };
        }

        public static MancheDomanda CreateQuestion(Manche manche, int index, string text) => new()
        {
            Manche = manche,
            Index = index,
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
