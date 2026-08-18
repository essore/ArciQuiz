using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class SquadreServiceTests
{
    [Fact]
    public async Task RegistraAsync_PublicaInLobby_CreaSquadraEIscrizione()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Pronta);

        var result = await SquadreService.RegistraAsync(test.Database, null, "Le Aquile", "segreta", isAdmin: false);

        Assert.True(result.IsSuccess);
        Assert.Single(test.Database.Players);
        Assert.Single(test.Database.PlayersPartite);
    }

    [Fact]
    public async Task RegistraAsync_PubblicaFuoriLobby_RifiutaLaRichiesta()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Configurazione);

        var result = await SquadreService.RegistraAsync(test.Database, null, "Le Aquile", "segreta", isAdmin: false);

        Assert.False(result.IsSuccess);
        Assert.Empty(test.Database.Players);
    }

    [Fact]
    public async Task RegistraAsync_NomeDuplicato_RifiutaLaRichiesta()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Pronta);
        await SquadreService.RegistraAsync(test.Database, null, "Le Aquile", "segreta", isAdmin: false);

        var result = await SquadreService.RegistraAsync(test.Database, null, "Le Aquile", "altra", isAdmin: false);

        Assert.False(result.IsSuccess);
        Assert.Single(test.Database.Players);
    }

    [Fact]
    public async Task RegistraAsync_AdminInRitardo_DopoAvvioCreaLaSquadra()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.InCorso);
        var partitaId = (await test.Database.Partite.SingleAsync()).Id;

        var result = await SquadreService.RegistraAsync(test.Database, partitaId, "Le Aquile", "segreta", isAdmin: true);

        Assert.True(result.IsSuccess);
        Assert.Single(test.Database.Players);
    }

    [Fact]
    public async Task AccediAsync_NuovoDispositivo_InvalidaLaSessionePrecedente()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Pronta);
        await SquadreService.RegistraAsync(test.Database, null, "Le Aquile", "segreta", isAdmin: false);
        var partitaId = (await test.Database.Partite.SingleAsync()).Id;

        var primoAccesso = await PlayerSessionService.AccediAsync(test.Database, partitaId, "Le Aquile", "segreta");
        var secondoAccesso = await PlayerSessionService.AccediAsync(test.Database, partitaId, "Le Aquile", "segreta");

        Assert.True(primoAccesso.IsSuccess);
        Assert.True(secondoAccesso.IsSuccess);
        Assert.False(await PlayerSessionService.SessioneValidaAsync(test.Database, primoAccesso.SquadraId!.Value, primoAccesso.SessionToken));
        Assert.True(await PlayerSessionService.SessioneValidaAsync(test.Database, secondoAccesso.SquadraId!.Value, secondoAccesso.SessionToken));
    }

    [Fact]
    public async Task AccediAsync_RefreshDelDispositivoAttivo_MantieneValidaLaSessione()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Pronta);
        await SquadreService.RegistraAsync(test.Database, null, "Le Aquile", "segreta", isAdmin: false);
        var partitaId = (await test.Database.Partite.SingleAsync()).Id;

        var accesso = await PlayerSessionService.AccediAsync(test.Database, partitaId, "Le Aquile", "segreta");
        test.Database.ChangeTracker.Clear();

        Assert.True(await PlayerSessionService.SessioneValidaAsync(test.Database, accesso.SquadraId!.Value, accesso.SessionToken));
    }

    [Fact]
    public async Task AccediAsync_DopoNuovoDbContext_RecuperaLaSessionePersistita()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Pronta);
        await SquadreService.RegistraAsync(test.Database, null, "Le Aquile", "segreta", isAdmin: false);
        var partitaId = (await test.Database.Partite.SingleAsync()).Id;
        var accesso = await PlayerSessionService.AccediAsync(test.Database, partitaId, "Le Aquile", "segreta");

        await using var riaperto = new ArciQuizDbContext(test.Options);
        Assert.True(await PlayerSessionService.SessioneValidaAsync(riaperto, accesso.SquadraId!.Value, accesso.SessionToken));
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
        public DbContextOptions<ArciQuizDbContext> Options { get; private init; } = null!;

        public static async Task<TestDatabase> CreateAsync(PartitaStato stato)
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-squadre-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            database.Partite.Add(new Partita { Titolo = "Partita test", Stato = stato });
            await database.SaveChangesAsync();
            return new TestDatabase(path, database) { Options = options };
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
