using Core.Entities;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests;

public class PartitaConfigSummaryTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CaricamentoRiepilogo_MostraLeDomandeDellaPartitaCorrente()
    {
        await using var test = await TestDatabase.CreateAsync();

        var partitaVuota = await test.Database.Partite
            .AsNoTracking()
            .Include(partita => partita.Manches)
                .ThenInclude(manche => manche.Domande)
            .SingleAsync(partita => partita.Id == test.EmptyGameId);
        var partitaPopolata = await test.Database.Partite
            .AsNoTracking()
            .Include(partita => partita.Manches)
                .ThenInclude(manche => manche.Domande)
            .SingleAsync(partita => partita.Id == test.PopulatedGameId);

        Assert.Empty(partitaVuota.Manches.Single().Domande);
        Assert.Single(partitaPopolata.Manches.Single().Domande);
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
        public int EmptyGameId { get; private set; }
        public int PopulatedGameId { get; private set; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-game-summary-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();

            var partitaVuota = new Partita { Titolo = "Partita vuota" };
            var partitaPopolata = new Partita { Titolo = "Partita popolata" };
            var mancheVuota = new Manche { Partita = partitaVuota };
            var manchePopolata = new Manche { Partita = partitaPopolata };
            var domanda = new Domanda
            {
                Testo = "Domanda associata",
                RispostaA = "A",
                RispostaB = "B",
                RispostaC = "C",
                RispostaD = "D",
                RispostaEsatta = 'A',
                Categoria = "Altro",
                Difficolta = "Facile"
            };

            database.AddRange(partitaVuota, partitaPopolata, mancheVuota, manchePopolata, domanda);
            await database.SaveChangesAsync();
            database.ManchesDomande.Add(new MancheDomanda
            {
                MancheId = manchePopolata.Id,
                DomandaId = domanda.Id,
                Index = 1
            });
            await database.SaveChangesAsync();

            return new TestDatabase(path, database)
            {
                EmptyGameId = partitaVuota.Id,
                PopulatedGameId = partitaPopolata.Id
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
