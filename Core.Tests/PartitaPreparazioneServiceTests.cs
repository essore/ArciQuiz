using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class PartitaPreparazioneServiceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DichiaraProntaAsync_ConRequisitiMancanti_MantieneLaPartitaInPreparazione()
    {
        await using var test = await TestDatabase.CreateAsync(includeValidQuestion: false);

        var result = await PartitaPreparazioneService.DichiaraProntaAsync(test.Database, test.PartitaId);
        var partita = await test.Database.Partite.AsNoTracking().SingleAsync();

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.RequisitiMancanti);
        Assert.Equal(PartitaStato.Configurazione, partita.Stato);
        Assert.Equal(GamePhase.Idle, partita.Fase);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DichiaraProntaAsync_ConChecklistCompleta_ApreLaLobby()
    {
        await using var test = await TestDatabase.CreateAsync(includeValidQuestion: true);

        var result = await PartitaPreparazioneService.DichiaraProntaAsync(test.Database, test.PartitaId);
        var partita = await test.Database.Partite.AsNoTracking().SingleAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Changed);
        Assert.Equal(PartitaStato.Pronta, partita.Stato);
        Assert.Equal(GamePhase.Lobby, partita.Fase);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TornaInPreparazioneAsync_DallaLobby_ConservaIRequisitiSalvati()
    {
        await using var test = await TestDatabase.CreateAsync(includeValidQuestion: true);
        await PartitaPreparazioneService.DichiaraProntaAsync(test.Database, test.PartitaId);

        var result = await PartitaPreparazioneService.TornaInPreparazioneAsync(test.Database, test.PartitaId);
        var partita = await test.Database.Partite
            .AsNoTracking()
            .Include(item => item.Manches)
                .ThenInclude(manche => manche.Domande)
            .SingleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(PartitaStato.Configurazione, partita.Stato);
        Assert.Equal(GamePhase.Idle, partita.Fase);
        Assert.Single(partita.Manches);
        Assert.Single(partita.Manches.Single().Domande);
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

        public static async Task<TestDatabase> CreateAsync(bool includeValidQuestion)
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-preparation-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();

            var partita = new Partita { Titolo = "Serata di prova", Stato = PartitaStato.Configurazione };
            var manche = new Manche { Partita = partita };
            database.AddRange(partita, manche);

            if (includeValidQuestion)
            {
                var domanda = new Domanda
                {
                    Testo = "Quale risposta è corretta?",
                    RispostaA = "A",
                    RispostaB = "B",
                    RispostaC = "C",
                    RispostaD = "D",
                    RispostaEsatta = 'A',
                    Categoria = "Altro",
                    Difficolta = "Facile"
                };
                database.Add(domanda);
                await database.SaveChangesAsync();
                database.ManchesDomande.Add(new MancheDomanda { MancheId = manche.Id, DomandaId = domanda.Id, Index = 1 });
            }

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
