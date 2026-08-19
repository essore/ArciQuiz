using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class SquadreService
{
    // Registra una squadra pubblica solo nella lobby oppure una squadra inserita dall'amministratore.
    public static async Task<SquadraOperationResult> RegistraAsync(
        ArciQuizDbContext db,
        int? partitaId,
        string? nomeSquadra,
        string? password,
        bool isAdmin)
    {
        var nome = nomeSquadra?.Trim();
        if (string.IsNullOrWhiteSpace(nome)) return SquadraOperationResult.Error("Il nome della squadra è obbligatorio.");
        if (string.IsNullOrWhiteSpace(password)) return SquadraOperationResult.Error("La password della squadra è obbligatoria.");

        var partita = partitaId.HasValue
            ? await db.Partite.FirstOrDefaultAsync(item => item.Id == partitaId.Value)
            : await db.Partite
                .Where(item => item.IsAttiva && item.Stato == PartitaStato.Pronta)
                .FirstOrDefaultAsync();

        if (partita is null) return SquadraOperationResult.Error("Non è disponibile una partita in lobby per le iscrizioni.");
        if (!isAdmin && partita.Stato != PartitaStato.Pronta)
            return SquadraOperationResult.Error("Le iscrizioni pubbliche sono chiuse.");
        if (isAdmin && partita.Stato == PartitaStato.Conclusa)
            return SquadraOperationResult.Error("Non è possibile modificare una partita conclusa.");
        if (await db.Players.AnyAsync(item => item.PartitaId == partita.Id && item.NomeSquadra == nome))
            return SquadraOperationResult.Error("Esiste già una squadra con questo nome nella partita.");

        var squadra = new Player
        {
            PartitaId = partita.Id,
            NomeSquadra = nome,
            Password = password.Trim(),
            DtIscrizioneUtc = DateTime.UtcNow
        };
        squadra.IscrizioniPartite.Add(new PlayerPartita
        {
            PartitaId = partita.Id,
            DtIscrizioneUtc = squadra.DtIscrizioneUtc
        });
        db.Players.Add(squadra);
        await db.SaveChangesAsync();

        return SquadraOperationResult.Success(squadra.Id, "Squadra registrata.");
    }

    public static async Task<SquadraOperationResult> AggiornaAsync(ArciQuizDbContext db, int squadraId, string? nomeSquadra, string? password)
    {
        var squadra = await db.Players.FirstOrDefaultAsync(item => item.Id == squadraId);
        if (squadra is null) return SquadraOperationResult.Error("La squadra non è più disponibile.");

        var nome = nomeSquadra?.Trim();
        if (string.IsNullOrWhiteSpace(nome)) return SquadraOperationResult.Error("Il nome della squadra è obbligatorio.");
        if (string.IsNullOrWhiteSpace(password)) return SquadraOperationResult.Error("La password della squadra è obbligatoria.");
        if (squadra.PartitaId is null) return SquadraOperationResult.Error("La squadra non è associata a una partita.");

        var duplicata = await db.Players.AnyAsync(item => item.PartitaId == squadra.PartitaId && item.NomeSquadra == nome && item.Id != squadra.Id);
        if (duplicata) return SquadraOperationResult.Error("Esiste già una squadra con questo nome nella partita.");

        squadra.NomeSquadra = nome;
        squadra.Password = password.Trim();
        await db.SaveChangesAsync();
        return SquadraOperationResult.Success(squadra.Id, "Squadra aggiornata.");
    }

    public static async Task<SquadraOperationResult> EliminaAsync(ArciQuizDbContext db, int squadraId)
    {
        var squadra = await db.Players.Include(item => item.Risposte).FirstOrDefaultAsync(item => item.Id == squadraId);
        if (squadra is null) return SquadraOperationResult.Error("La squadra non è più disponibile.");
        if (squadra.Risposte.Count > 0) return SquadraOperationResult.Error("Non è possibile eliminare una squadra con risposte già registrate.");

        db.Players.Remove(squadra);
        await db.SaveChangesAsync();
        return SquadraOperationResult.Success(squadraId, "Squadra eliminata.");
    }
}

public sealed record SquadraOperationResult(bool IsSuccess, int? SquadraId, string Messaggio)
{
    public static SquadraOperationResult Success(int squadraId, string messaggio) => new(true, squadraId, messaggio);
    public static SquadraOperationResult Error(string messaggio) => new(false, null, messaggio);
}
