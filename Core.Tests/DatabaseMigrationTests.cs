using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests;

public class DatabaseMigrationTests
{
    [Fact]
    public void Migrate_CreatesDatabaseWithLatestSchema()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"arciquiz-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using (var database = new ArciQuizDbContext(options))
            {
                database.Database.Migrate();

                Assert.True(database.Database.CanConnect());
                var applied = database.Database.GetAppliedMigrations().ToList();
                Assert.Contains(applied, m => m.EndsWith("InitialCreate", StringComparison.Ordinal));
                Assert.Contains(applied, m => m.EndsWith("AddPersistenceModelExtensions", StringComparison.Ordinal));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }

    [Fact]
    public void NomeSquadra_IsUniquePerPartita_ThrowsOnDuplicate()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"arciquiz-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var database = new ArciQuizDbContext(options);
            database.Database.Migrate();

            var partita = new Partita { Titolo = "Partita Test" };
            database.Partite.Add(partita);
            database.SaveChanges();

            var squadra1 = new Player { NomeSquadra = "Aquile", Password = "p1", PartitaId = partita.Id };
            database.Players.Add(squadra1);
            database.SaveChanges();

            var squadra2 = new Player { NomeSquadra = "Aquile", Password = "p2", PartitaId = partita.Id };
            database.Players.Add(squadra2);

            Assert.Throws<DbUpdateException>(() => database.SaveChanges());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }

    [Fact]
    public void NomeSquadra_SameNameInDifferentPartite_Succeeds()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"arciquiz-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var database = new ArciQuizDbContext(options);
            database.Database.Migrate();

            var partita1 = new Partita { Titolo = "Partita 1" };
            var partita2 = new Partita { Titolo = "Partita 2" };
            database.Partite.AddRange(partita1, partita2);
            database.SaveChanges();

            var squadra1 = new Player { NomeSquadra = "Aquile", Password = "p1", PartitaId = partita1.Id };
            var squadra2 = new Player { NomeSquadra = "Aquile", Password = "p2", PartitaId = partita2.Id };
            database.Players.AddRange(squadra1, squadra2);
            database.SaveChanges();

            Assert.Equal(2, database.Players.Count());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }

    [Fact]
    public void MancheRisposta_OneAnswerPerPlayerAndDomanda_ThrowsOnDuplicate()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"arciquiz-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var database = new ArciQuizDbContext(options);
            database.Database.Migrate();

            var partita = new Partita { Titolo = "Partita Test" };
            var manche = new Manche { Partita = partita, TempoRispostaSecondi = 30, MalusBase = 500, Moltiplicatore = 1, MaxAstensioni = 2 };
            var domanda = new Domanda { Testo = "Qual è la capitale?", RispostaA = "Roma", RispostaB = "Parigi", RispostaC = "Madrid", RispostaD = "Berlino", RispostaEsatta = 'A' };
            var md = new MancheDomanda { Manche = manche, Domanda = domanda, Index = 1, ModificatorePunti = 1, DurataSecondiOverride = 40 };
            var player = new Player { NomeSquadra = "Tigri", Password = "pass", Partita = partita };

            database.Partite.Add(partita);
            database.Manches.Add(manche);
            database.Domande.Add(domanda);
            database.ManchesDomande.Add(md);
            database.Players.Add(player);
            database.SaveChanges();

            var risposta1 = new MancheRispostaRicevuta
            {
                PlayerId = player.Id,
                MancheDomandaId = md.Id,
                Risposta = 'A',
                IsCorrect = true,
                PuntiAssegnati = 1500,
                TempoImpiegatoMs = 5000,
                CoefficienteTempo = 0.75
            };
            database.ManchesRisposte.Add(risposta1);
            database.SaveChanges();

            var risposta2 = new MancheRispostaRicevuta
            {
                PlayerId = player.Id,
                MancheDomandaId = md.Id,
                Risposta = 'B',
                IsCorrect = false,
                PuntiAssegnati = -500
            };
            database.ManchesRisposte.Add(risposta2);

            Assert.Throws<DbUpdateException>(() => database.SaveChanges());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }

    [Fact]
    public void MancheDomanda_And_Risposta_PersistExtendedProperties()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"arciquiz-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using (var database = new ArciQuizDbContext(options))
            {
                database.Database.Migrate();

                var partita = new Partita { Titolo = "Partita Estesa", Stato = PartitaStato.Pronta, CurrentMancheId = 1 };
                var manche = new Manche
                {
                    Partita = partita,
                    Ordine = 1,
                    TempoRispostaSecondi = 25,
                    PuntiBase = 3000,
                    MalusBase = 600,
                    Moltiplicatore = 2,
                    MaxAstensioni = 4
                };
                var domanda = new Domanda
                {
                    Categoria = "Storia",
                    Difficolta = "Media",
                    Testo = "Anno scoperta America?",
                    RispostaA = "1492",
                    RispostaB = "1500",
                    RispostaC = "1450",
                    RispostaD = "1550",
                    RispostaEsatta = 'A',
                    FlagErrore = true
                };
                var scadenza = DateTimeOffset.UtcNow.AddSeconds(25);
                var md = new MancheDomanda
                {
                    Manche = manche,
                    Domanda = domanda,
                    Index = 1,
                    ModificatorePunti = 2,
                    DurataSecondiOverride = 35,
                    ScadenzaUtc = scadenza,
                    IsAnnullata = true,
                    MotivoAnnullamento = "Errore testo",
                    DtAnnullamentoUtc = DateTime.UtcNow
                };
                var player = new Player
                {
                    NomeSquadra = "Lupi",
                    Password = "lupi",
                    Partita = partita,
                    SessionToken = "sess-12345"
                };
                var risposta = new MancheRispostaRicevuta
                {
                    Player = player,
                    MancheDomanda = md,
                    IsAstenuto = true,
                    Risposta = null,
                    IsCorrect = false,
                    PuntiAssegnati = -600,
                    AstensionePenalizzata = true,
                    IsAnnullata = true
                };

                database.Partite.Add(partita);
                database.Manches.Add(manche);
                database.Domande.Add(domanda);
                database.ManchesDomande.Add(md);
                database.Players.Add(player);
                database.ManchesRisposte.Add(risposta);
                database.SaveChanges();
            }

            using (var verifyDb = new ArciQuizDbContext(options))
            {
                var loadedMd = verifyDb.ManchesDomande
                    .Include(x => x.Manche)
                    .Include(x => x.Domanda)
                    .Include(x => x.Risposte)
                    .First();

                Assert.Equal(35, loadedMd.DurataSecondiOverride);
                Assert.True(loadedMd.IsAnnullata);
                Assert.Equal("Errore testo", loadedMd.MotivoAnnullamento);
                Assert.NotNull(loadedMd.ScadenzaUtc);
                Assert.Equal(600, loadedMd.Manche!.MalusBase);
                Assert.Equal(2, loadedMd.Manche.Moltiplicatore);
                Assert.Equal(4, loadedMd.Manche.MaxAstensioni);

                var loadedRisposta = loadedMd.Risposte.First();
                Assert.True(loadedRisposta.IsAstenuto);
                Assert.Null(loadedRisposta.Risposta);
                Assert.True(loadedRisposta.AstensionePenalizzata);
                Assert.True(loadedRisposta.IsAnnullata);
                Assert.Equal(-600, loadedRisposta.PuntiAssegnati);

                var loadedPlayer = verifyDb.Players.First();
                Assert.Equal("sess-12345", loadedPlayer.SessionToken);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }
}

