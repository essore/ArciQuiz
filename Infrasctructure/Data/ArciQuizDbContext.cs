
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrasctructure.Data;

public class ArciQuizDbContext : DbContext
{
    public ArciQuizDbContext(DbContextOptions<ArciQuizDbContext> options)
        : base(options)
    {
    }

    public DbSet<Partita> Partite { get; set; }
    public DbSet<Manche> Manches { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<PlayerPartita> PlayersPartite { get; set; }
    public DbSet<Domanda> Domande { get; set; }
    public DbSet<MancheDomanda> ManchesDomande { get; set; }
    public DbSet<MancheRispostaRicevuta> ManchesRisposte { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // chiavi composite
        modelBuilder.Entity<PlayerPartita>()
            .HasKey(pp => new { pp.PlayerId, pp.PartitaId });

        // relazioni
        modelBuilder.Entity<PlayerPartita>()
            .HasOne(pp => pp.Player)
            .WithMany(p => p.IscrizioniPartite)
            .HasForeignKey(pp => pp.PlayerId);

        modelBuilder.Entity<PlayerPartita>()
            .HasOne(pp => pp.Partita)
            .WithMany(p => p.PlayersPartita)
            .HasForeignKey(pp => pp.PartitaId);

        modelBuilder.Entity<Manche>()
            .HasOne(m => m.Partita)
            .WithMany(p => p.Manches)
            .HasForeignKey(m => m.PartitaId);

        modelBuilder.Entity<MancheDomanda>()
            .HasOne(md => md.Manche)
            .WithMany(m => m.Domande)
            .HasForeignKey(md => md.MancheId);

        modelBuilder.Entity<MancheDomanda>()
            .HasOne(md => md.Domanda)
            .WithMany(d => d.Manches)
            .HasForeignKey(md => md.DomandaId);

        modelBuilder.Entity<MancheRispostaRicevuta>()
            .HasOne(r => r.Player)
            .WithMany()
            .HasForeignKey(r => r.PlayerId);

        modelBuilder.Entity<MancheRispostaRicevuta>()
            .HasOne(r => r.MancheDomanda)
            .WithMany(md => md.Risposte)
            .HasForeignKey(r => r.MancheDomandaId);
    }
}