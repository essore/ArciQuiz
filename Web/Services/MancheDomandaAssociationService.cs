using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class MancheDomandaAssociationService
{
    // Aggiunge una domanda alla manche richiesta solo quando non è già usata dalla stessa partita.
    public static async Task<MancheDomandaAssociationResult> AggiungiAsync(
        ArciQuizDbContext database,
        int mancheId,
        int domandaId,
        CancellationToken cancellationToken = default)
    {
        var manche = await database.Manches
            .Include(item => item.Partita)
            .FirstOrDefaultAsync(item => item.Id == mancheId, cancellationToken);
        if (manche is null)
            return MancheDomandaAssociationResult.Error("La manche non è più disponibile.");
        if (manche.Partita?.Stato is not (PartitaStato.Nuova or PartitaStato.Configurazione))
            return MancheDomandaAssociationResult.Error("Per modificare le domande, riporta prima la partita in preparazione.");

        var domandaEsiste = await database.Domande
            .AnyAsync(item => item.Id == domandaId && !item.FlgDeleted, cancellationToken);
        if (!domandaEsiste)
            return MancheDomandaAssociationResult.Error("La domanda non è più disponibile.");

        var giaAssociata = await database.ManchesDomande
            .AnyAsync(
                item => item.DomandaId == domandaId && item.Manche!.PartitaId == manche.PartitaId,
                cancellationToken);
        if (giaAssociata)
            return MancheDomandaAssociationResult.Error("La domanda è già associata a un'altra manche di questa partita.");

        var indiceSuccessivo = (await database.ManchesDomande
            .Where(item => item.MancheId == mancheId)
            .Select(item => (int?)item.Index)
            .MaxAsync(cancellationToken) ?? 0) + 1;
        database.ManchesDomande.Add(new MancheDomanda
        {
            MancheId = mancheId,
            DomandaId = domandaId,
            Index = indiceSuccessivo,
            ModificatorePunti = 1
        });
        await database.SaveChangesAsync(cancellationToken);

        return MancheDomandaAssociationResult.Success("Domanda aggiunta alla manche.");
    }
}

public sealed record MancheDomandaAssociationResult(bool IsSuccess, string Messaggio)
{
    public static MancheDomandaAssociationResult Success(string messaggio) => new(true, messaggio);
    public static MancheDomandaAssociationResult Error(string messaggio) => new(false, messaggio);
}
