using Core.Entities;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class MancheDomandaAssociationServiceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AggiungiAsync_AssociaUnaDomandaNonUsataAllaPartita()
    {
        await using var test = await TestDatabase.CreateAsync();

        var result = await MancheDomandaAssociationService.AggiungiAsync(
            test.Database,
            test.FirstRoundId,
            test.QuestionId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await test.Database.ManchesDomande.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AggiungiAsync_RifiutaDuplicatiNellaStessaPartitaMaLiConsenteInUnAltra()
    {
        await using var test = await TestDatabase.CreateAsync();

        var firstResult = await MancheDomandaAssociationService.AggiungiAsync(
            test.Database,
            test.FirstRoundId,
            test.QuestionId);
        var duplicateResult = await MancheDomandaAssociationService.AggiungiAsync(
            test.Database,
            test.SecondRoundId,
            test.QuestionId);
        var otherGameResult = await MancheDomandaAssociationService.AggiungiAsync(
            test.Database,
            test.OtherGameRoundId,
            test.QuestionId);

        Assert.True(firstResult.IsSuccess);
        Assert.False(duplicateResult.IsSuccess);
        Assert.Contains("già associata", duplicateResult.Messaggio);
        Assert.True(otherGameResult.IsSuccess);
        Assert.Equal(2, await test.Database.ManchesDomande.CountAsync());
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
        public int FirstRoundId { get; private set; }
        public int SecondRoundId { get; private set; }
        public int OtherGameRoundId { get; private set; }
        public int QuestionId { get; private set; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-association-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();

            var partita = new Partita { Titolo = "Partita principale" };
            var altraPartita = new Partita { Titolo = "Altra partita" };
            var primaManche = new Manche { Partita = partita };
            var secondaManche = new Manche { Partita = partita };
            var mancheAltraPartita = new Manche { Partita = altraPartita };
            var domanda = new Domanda
            {
                Testo = "Domanda condivisa",
                RispostaA = "A",
                RispostaB = "B",
                RispostaC = "C",
                RispostaD = "D",
                RispostaEsatta = 'A',
                Categoria = "Altro",
                Difficolta = "Facile"
            };

            database.AddRange(partita, altraPartita, primaManche, secondaManche, mancheAltraPartita, domanda);
            await database.SaveChangesAsync();

            return new TestDatabase(path, database)
            {
                FirstRoundId = primaManche.Id,
                SecondRoundId = secondaManche.Id,
                OtherGameRoundId = mancheAltraPartita.Id,
                QuestionId = domanda.Id
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
