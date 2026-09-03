using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class PartitaCloneService
{
    // Crea una nuova partita con la sola configurazione di manche e domande della sorgente.
    public static async Task<PartitaCloneOperationResult> ClonaAsync(
        ArciQuizDbContext database,
        int partitaSorgenteId,
        string? titolo,
        CancellationToken cancellationToken = default)
    {
        var titoloNormalizzato = titolo?.Trim();
        if (string.IsNullOrWhiteSpace(titoloNormalizzato))
            return PartitaCloneOperationResult.Error("Il titolo della nuova partita è obbligatorio.");

        var partitaSorgente = await database.Partite
            .AsNoTracking()
            .Include(partita => partita.Manches)
                .ThenInclude(manche => manche.Domande)
            .FirstOrDefaultAsync(partita => partita.Id == partitaSorgenteId, cancellationToken);
        if (partitaSorgente is null)
            return PartitaCloneOperationResult.Error("La partita da clonare non è più disponibile.");

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var partitaClonata = new Partita
            {
                Titolo = titoloNormalizzato,
                DtCreazione = DateTime.UtcNow,
                Stato = PartitaStato.Nuova,
                Fase = GamePhase.Idle,
                IsAttiva = false
            };
            database.Partite.Add(partitaClonata);

            foreach (var mancheSorgente in partitaSorgente.Manches.OrderBy(manche => manche.Ordine).ThenBy(manche => manche.Id))
            {
                var mancheClonata = new Manche
                {
                    Partita = partitaClonata,
                    Ordine = mancheSorgente.Ordine,
                    Stato = MancheStato.Nuova,
                    TempoRispostaSecondi = mancheSorgente.TempoRispostaSecondi,
                    PuntiBase = mancheSorgente.PuntiBase,
                    MalusBase = mancheSorgente.MalusBase,
                    Moltiplicatore = mancheSorgente.Moltiplicatore,
                    PenalitaErrore = mancheSorgente.PenalitaErrore,
                    MaxAstensioni = mancheSorgente.MaxAstensioni
                };
                partitaClonata.Manches.Add(mancheClonata);

                foreach (var domandaSorgente in mancheSorgente.Domande.OrderBy(domanda => domanda.Index).ThenBy(domanda => domanda.Id))
                {
                    mancheClonata.Domande.Add(new MancheDomanda
                    {
                        DomandaId = domandaSorgente.DomandaId,
                        Index = domandaSorgente.Index,
                        ModificatorePunti = domandaSorgente.ModificatorePunti,
                        DurataSecondiOverride = domandaSorgente.DurataSecondiOverride
                    });
                }
            }

            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return PartitaCloneOperationResult.Success(partitaClonata.Id, "Partita clonata. Configura i dettagli prima di renderla pronta.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return PartitaCloneOperationResult.Error("Non è stato possibile clonare la partita. Nessuna nuova partita è stata creata.");
        }
    }
}

public sealed record PartitaCloneOperationResult(bool IsSuccess, int? PartitaId, string Messaggio)
{
    public static PartitaCloneOperationResult Success(int partitaId, string messaggio) => new(true, partitaId, messaggio);
    public static PartitaCloneOperationResult Error(string messaggio) => new(false, null, messaggio);
}
