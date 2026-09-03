using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class PartitaAttivaServiceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ImpostaAttivaAsync_MantieneUnaSolaPartitaAttivaEIngressoCoerente()
    {
        await using var test = await TestDatabase.CreateAsync();

        var firstActivation = await PartitaAttivaService.ImpostaAttivaAsync(test.Database, test.FirstGameId);
        var secondActivation = await PartitaAttivaService.ImpostaAttivaAsync(test.Database, test.SecondGameId);
        var activeGames = await test.Database.Partite
            .AsNoTracking()
            .Where(item => item.IsAttiva)
            .Select(item => item.Id)
            .ToListAsync();
        var entry = await PlayerEntryService.ResolveAsync(test.Database, null, null);

        Assert.True(firstActivation.IsSuccess);
        Assert.True(secondActivation.IsSuccess);
        Assert.Equal([test.SecondGameId], activeGames);
        Assert.Equal(PlayerEntryDestination.RegistrationOrLogin, entry.Destination);
        Assert.Equal(test.SecondGameId, entry.PartitaId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ImpostaAttivaAsync_RifiutaUnaPartitaConclusaESalvaLoStorico()
    {
        await using var test = await TestDatabase.CreateAsync();

        var result = await PartitaAttivaService.ImpostaAttivaAsync(test.Database, test.ConcludedGameId);
        var concludedGame = await test.Database.Partite
            .AsNoTracking()
            .SingleAsync(item => item.Id == test.ConcludedGameId);

        Assert.False(result.IsSuccess);
        Assert.Equal(PartitaStato.Conclusa, concludedGame.Stato);
        Assert.False(concludedGame.IsAttiva);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task VerificaAttivazioneAsync_RichiedeConfermaSenzaModificareLaPartitaAttiva()
    {
        await using var test = await TestDatabase.CreateAsync();
        await PartitaAttivaService.ImpostaAttivaAsync(test.Database, test.FirstGameId);

        var result = await PartitaAttivaService.VerificaAttivazioneAsync(test.Database, test.SecondGameId);
        var activeGames = await test.Database.Partite
            .AsNoTracking()
            .Where(item => item.IsAttiva)
            .Select(item => item.Id)
            .ToListAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.RequiresConfirmation);
        Assert.Equal("Prima partita", result.PartitaAttivaTitolo);
        Assert.Equal([test.FirstGameId], activeGames);
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
        public int FirstGameId { get; private set; }
        public int SecondGameId { get; private set; }
        public int ConcludedGameId { get; private set; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-active-game-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new TestDatabase(path, database);
            var firstGame = new Partita { Titolo = "Prima partita", Stato = PartitaStato.Pronta };
            var secondGame = new Partita { Titolo = "Seconda partita", Stato = PartitaStato.Pronta };
            var concludedGame = new Partita { Titolo = "Storico", Stato = PartitaStato.Conclusa };

            database.Partite.AddRange(firstGame, secondGame, concludedGame);
            await database.SaveChangesAsync();
            test.FirstGameId = firstGame.Id;
            test.SecondGameId = secondGame.Id;
            test.ConcludedGameId = concludedGame.Id;
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
