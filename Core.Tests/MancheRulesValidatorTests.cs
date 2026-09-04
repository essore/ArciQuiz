using Core.Entities;
using Core.Services;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests;

public class MancheRulesValidatorTests
{
    [Fact]
    public void MancheNuova_UsaMalusPredefinitoEPenalitaAttiva()
    {
        var manche = new Manche();

        Assert.Equal(500, manche.MalusBase);
        Assert.True(manche.PenalitaErrore);
    }

    [Theory]
    [InlineData(4, 2000, 500, 1, 3, "tempo di risposta")]
    [InlineData(20, -1, 500, 1, 3, "punti base")]
    [InlineData(20, 2000, -1, 1, 3, "malus base")]
    [InlineData(20, 2000, 500, 0, 3, "moltiplicatore")]
    [InlineData(20, 2000, 500, 1, -1, "astensioni")]
    public void Validate_ValoreFuoriLimite_RestituisceErroreItaliano(
        int tempoRispostaSecondi,
        int puntiBase,
        int malusBase,
        int moltiplicatore,
        int maxAstensioni,
        string testoErrore)
    {
        var result = MancheRulesValidator.Validate(new Manche
        {
            TempoRispostaSecondi = tempoRispostaSecondi,
            PuntiBase = puntiBase,
            MalusBase = malusBase,
            Moltiplicatore = moltiplicatore,
            MaxAstensioni = maxAstensioni
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errori, errore => errore.Contains(testoErrore));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegoleManche_SalvateERicaricate_SonoConservate()
    {
        await using var test = await TestDatabase.CreateAsync();
        var manche = new Manche
        {
            PartitaId = test.PartitaId,
            TempoRispostaSecondi = 45,
            PuntiBase = 3500,
            MalusBase = 750,
            Moltiplicatore = 3,
            MaxAstensioni = 4
        };

        Assert.True(MancheRulesValidator.Validate(manche).IsValid);

        test.Database.Manches.Add(manche);
        await test.Database.SaveChangesAsync();
        test.Database.ChangeTracker.Clear();

        var mancheRicaricata = await test.Database.Manches.SingleAsync(item => item.Id == manche.Id);

        Assert.Equal(45, mancheRicaricata.TempoRispostaSecondi);
        Assert.Equal(3500, mancheRicaricata.PuntiBase);
        Assert.Equal(750, mancheRicaricata.MalusBase);
        Assert.Equal(3, mancheRicaricata.Moltiplicatore);
        Assert.Equal(4, mancheRicaricata.MaxAstensioni);
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

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-manche-rules-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();

            var partita = new Partita { Titolo = "Partita regole" };
            database.Partite.Add(partita);
            await database.SaveChangesAsync();

            return new TestDatabase(path, database) { PartitaId = partita.Id };
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
