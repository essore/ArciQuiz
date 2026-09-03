using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Core.Tests;

public class PartitaCloneServiceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ClonaAsync_CopiaConfigurazioneEScartaDatiDiSerata()
    {
        await using var test = await TestDatabase.CreateAsync();

        var result = await PartitaCloneService.ClonaAsync(test.Database, test.PartitaSorgenteId, "Nuova serata");
        var partitaClonata = await test.Database.Partite
            .AsNoTracking()
            .Include(partita => partita.Manches)
                .ThenInclude(manche => manche.Domande)
            .SingleAsync(partita => partita.Id == result.PartitaId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nuova serata", partitaClonata.Titolo);
        Assert.Equal(PartitaStato.Nuova, partitaClonata.Stato);
        Assert.Equal(GamePhase.Idle, partitaClonata.Fase);
        Assert.False(partitaClonata.IsAttiva);
        Assert.Null(partitaClonata.CurrentMancheId);
        Assert.Null(partitaClonata.CurrentMancheDomandaId);
        Assert.Null(partitaClonata.DtInizioUtc);
        Assert.Null(partitaClonata.DtFineUtc);

        var manchesClonate = partitaClonata.Manches.OrderBy(manche => manche.Ordine).ToList();
        Assert.Collection(
            manchesClonate,
            prima =>
            {
                Assert.Equal(1, prima.Ordine);
                Assert.Equal(MancheStato.Nuova, prima.Stato);
                Assert.Equal(30, prima.TempoRispostaSecondi);
                Assert.Equal(2500, prima.PuntiBase);
                Assert.Equal(600, prima.MalusBase);
                Assert.Equal(2, prima.Moltiplicatore);
                Assert.True(prima.PenalitaErrore);
                Assert.Equal(2, prima.MaxAstensioni);
                Assert.Null(prima.DtInizioUtc);
                Assert.Null(prima.DtFineUtc);
                Assert.Collection(
                    prima.Domande.OrderBy(domanda => domanda.Index),
                    domanda =>
                    {
                        Assert.Equal(test.PrimaDomandaCatalogoId, domanda.DomandaId);
                        Assert.Equal(1, domanda.Index);
                        Assert.Equal(3, domanda.ModificatorePunti);
                        Assert.Equal(45, domanda.DurataSecondiOverride);
                        Assert.Null(domanda.DtStart);
                        Assert.Null(domanda.DtEnd);
                        Assert.Null(domanda.ScadenzaUtc);
                        Assert.False(domanda.IsAnnullata);
                    });
            },
            seconda =>
            {
                Assert.Equal(2, seconda.Ordine);
                Assert.Equal(MancheStato.Nuova, seconda.Stato);
                Assert.Collection(
                    seconda.Domande.OrderBy(domanda => domanda.Index),
                    domanda => Assert.Equal(test.SecondaDomandaCatalogoId, domanda.DomandaId));
            });

        Assert.Equal(0, await test.Database.Players.CountAsync(player => player.PartitaId == partitaClonata.Id));
        Assert.Equal(0, await test.Database.PlayersPartite.CountAsync(item => item.PartitaId == partitaClonata.Id));
        Assert.Equal(1, await test.Database.ManchesRisposte.CountAsync());
        Assert.Equal(2, await test.Database.Domande.CountAsync());

        var partitaSorgente = await test.Database.Partite
            .AsNoTracking()
            .SingleAsync(partita => partita.Id == test.PartitaSorgenteId);
        Assert.Equal(PartitaStato.InCorso, partitaSorgente.Stato);
        Assert.True(partitaSorgente.IsAttiva);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ClonaAsync_RollbackQuandoIlSalvataggioFallisce()
    {
        await using var test = await TestDatabase.CreateAsync();
        await test.Database.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER BloccaClone
            BEFORE INSERT ON Partite
            WHEN NEW.Titolo = 'Errore clone'
            BEGIN
                SELECT RAISE(ABORT, 'Errore di prova');
            END;
            """);

        var result = await PartitaCloneService.ClonaAsync(test.Database, test.PartitaSorgenteId, "Errore clone");

        Assert.False(result.IsSuccess);
        Assert.Null(result.PartitaId);
        Assert.Equal(1, await test.Database.Partite.CountAsync());
        Assert.Equal(2, await test.Database.Manches.CountAsync());
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
        public int PartitaSorgenteId { get; private set; }
        public int PrimaDomandaCatalogoId { get; private set; }
        public int SecondaDomandaCatalogoId { get; private set; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"arciquiz-clone-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>().UseSqlite($"Data Source={path}").Options;
            var database = new ArciQuizDbContext(options);
            await database.Database.MigrateAsync();

            var partita = new Partita
            {
                Titolo = "Serata sorgente",
                Stato = PartitaStato.InCorso,
                Fase = GamePhase.ShowingQuestion,
                IsAttiva = true,
                DtInizioUtc = DateTime.UtcNow.AddHours(-1)
            };
            var primaManche = new Manche
            {
                Partita = partita,
                Ordine = 1,
                Stato = MancheStato.InGioco,
                TempoRispostaSecondi = 30,
                PuntiBase = 2500,
                MalusBase = 600,
                Moltiplicatore = 2,
                PenalitaErrore = true,
                MaxAstensioni = 2,
                DtInizioUtc = DateTime.UtcNow.AddMinutes(-30)
            };
            var secondaManche = new Manche { Partita = partita, Ordine = 2, Stato = MancheStato.Pronta };
            var primaDomandaCatalogo = CreateQuestion("Prima domanda");
            var secondaDomandaCatalogo = CreateQuestion("Seconda domanda");
            var primaDomanda = new MancheDomanda
            {
                Manche = primaManche,
                Domanda = primaDomandaCatalogo,
                Index = 1,
                ModificatorePunti = 3,
                DurataSecondiOverride = 45,
                DtStart = DateTime.UtcNow.AddMinutes(-1),
                ScadenzaUtc = DateTimeOffset.UtcNow.AddMinutes(1),
                IsAnnullata = true,
                MotivoAnnullamento = "Errore",
                DtAnnullamentoUtc = DateTime.UtcNow
            };
            var secondaDomanda = new MancheDomanda
            {
                Manche = secondaManche,
                Domanda = secondaDomandaCatalogo,
                Index = 1,
                ModificatorePunti = 1
            };
            var squadra = new Player { Partita = partita, NomeSquadra = "Falchi", Password = "segreto" };

            database.AddRange(partita, primaManche, secondaManche, primaDomandaCatalogo, secondaDomandaCatalogo, primaDomanda, secondaDomanda, squadra);
            await database.SaveChangesAsync();
            database.PlayersPartite.Add(new PlayerPartita { PlayerId = squadra.Id, PartitaId = partita.Id, SessionToken = "sessione" });
            database.ManchesRisposte.Add(new MancheRispostaRicevuta { PlayerId = squadra.Id, MancheDomandaId = primaDomanda.Id, Risposta = 'A' });
            partita.CurrentMancheId = primaManche.Id;
            partita.CurrentMancheDomandaId = primaDomanda.Id;
            await database.SaveChangesAsync();

            return new TestDatabase(path, database)
            {
                PartitaSorgenteId = partita.Id,
                PrimaDomandaCatalogoId = primaDomandaCatalogo.Id,
                SecondaDomandaCatalogoId = secondaDomandaCatalogo.Id
            };
        }

        private static Domanda CreateQuestion(string testo) => new()
        {
            Testo = testo,
            RispostaA = "A",
            RispostaB = "B",
            RispostaC = "C",
            RispostaD = "D",
            RispostaEsatta = 'A',
            Categoria = "Altro",
            Difficolta = "Facile"
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
