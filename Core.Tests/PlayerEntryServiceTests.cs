using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class PlayerEntryServiceTests
{
    [Fact]
    public async Task ResolveAsync_WithoutGame_ReturnsNoGame()
    {
        await using var test = await TestDatabase.CreateAsync(null);

        var result = await PlayerEntryService.ResolveAsync(test.Database, null, null);

        Assert.Equal(PlayerEntryDestination.NoGame, result.Destination);
    }

    [Fact]
    public async Task ResolveAsync_LobbyWithoutSession_OffersRegistrationOrLogin()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Pronta);

        var result = await PlayerEntryService.ResolveAsync(test.Database, null, null);

        Assert.Equal(PlayerEntryDestination.RegistrationOrLogin, result.Destination);
    }

    [Fact]
    public async Task ResolveAsync_GameStartedWithoutSession_RequiresLogin()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.InCorso);

        var result = await PlayerEntryService.ResolveAsync(test.Database, null, null);

        Assert.Equal(PlayerEntryDestination.Login, result.Destination);
    }

    [Fact]
    public async Task ResolveAsync_ConcludedGameWithoutSession_RequiresLogin()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Conclusa);

        var result = await PlayerEntryService.ResolveAsync(test.Database, null, null);

        Assert.Equal(PlayerEntryDestination.Login, result.Destination);
    }

    [Fact]
    public async Task ResolveAsync_ValidSession_OpensTeamArea()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.Pronta);
        await SquadreService.RegistraAsync(test.Database, test.PartitaId, "Falchi", "segreta", isAdmin: true);
        var login = await PlayerSessionService.AccediAsync(test.Database, test.PartitaId, "Falchi", "segreta");

        var result = await PlayerEntryService.ResolveAsync(test.Database, login.SquadraId, login.SessionToken);

        Assert.Equal(PlayerEntryDestination.TeamArea, result.Destination);
        Assert.Equal(test.PartitaId, result.PartitaId);
    }

    [Fact]
    public async Task ResolveAsync_ReplacedSessionDuringGame_RequiresLogin()
    {
        await using var test = await TestDatabase.CreateAsync(PartitaStato.InCorso);
        await SquadreService.RegistraAsync(test.Database, test.PartitaId, "Falchi", "segreta", isAdmin: true);
        var oldLogin = await PlayerSessionService.AccediAsync(test.Database, test.PartitaId, "Falchi", "segreta");
        await PlayerSessionService.AccediAsync(test.Database, test.PartitaId, "Falchi", "segreta");

        var result = await PlayerEntryService.ResolveAsync(test.Database, oldLogin.SquadraId, oldLogin.SessionToken);

        Assert.Equal(PlayerEntryDestination.Login, result.Destination);
    }

    [Fact]
    public void CreatePersistentProperties_PersistsOnlyForTheEventWindow()
    {
        var start = new DateTimeOffset(2026, 8, 18, 18, 0, 0, TimeSpan.Zero);
        var timeProvider = new FixedTimeProvider(start);

        var properties = PlayerSessionAuthentication.CreatePersistentProperties(timeProvider);

        Assert.True(properties.IsPersistent);
        Assert.True(properties.AllowRefresh);
        Assert.Equal(start.AddHours(12), properties.ExpiresUtc);
    }

    [Theory]
    [InlineData("/squadra")]
    [InlineData("/squadra/accesso")]
    public void SelectScheme_TeamPath_UsesPlayerCookie(string path)
    {
        Assert.Equal(PlayerSessionAuthentication.Scheme, PlayerSessionAuthentication.SelectScheme(path));
    }

    [Theory]
    [InlineData("/gioca")]
    [InlineData("/admin")]
    public void SelectScheme_NonTeamPath_UsesAdminCookie(string path)
    {
        Assert.Equal("Cookies", PlayerSessionAuthentication.SelectScheme(path));
    }

    [Fact]
    public void SelectScheme_BlazorConnectionWithPlayerCookie_UsesPlayerCookie()
    {
        Assert.Equal(
            PlayerSessionAuthentication.Scheme,
            PlayerSessionAuthentication.SelectScheme("/_blazor", hasPlayerCookie: true));
    }

    [Fact]
    public void SelectScheme_BlazorConnectionWithoutPlayerCookie_UsesAdminCookie()
    {
        Assert.Equal("Cookies", PlayerSessionAuthentication.SelectScheme("/_blazor"));
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

        public static async Task<TestDatabase> CreateAsync(PartitaStato? stato)
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-entry-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();
            var test = new TestDatabase(path, database);

            if (stato.HasValue)
            {
                var partita = new Partita { Titolo = "Ingresso test", Stato = stato.Value };
                database.Partite.Add(partita);
                await database.SaveChangesAsync();
                test.PartitaId = partita.Id;
            }

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
