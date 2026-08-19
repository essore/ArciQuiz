using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class PartitaAttivaService
{
    // Seleziona in modo atomico la partita che governa gli ingressi pubblici e la regia.
    public static async Task<PartitaAttivaOperationResult> ImpostaAttivaAsync(
        ArciQuizDbContext database,
        int partitaId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var partita = await database.Partite.FirstOrDefaultAsync(item => item.Id == partitaId, cancellationToken);
        if (partita is null)
            return PartitaAttivaOperationResult.Error("Partita non trovata.");
        if (partita.Stato == PartitaStato.Conclusa)
            return PartitaAttivaOperationResult.Error("Una partita conclusa non può diventare attiva.");
        if (partita.IsAttiva)
            return PartitaAttivaOperationResult.Unchanged("La partita è già attiva.");

        await database.Partite
            .Where(item => item.IsAttiva && item.Id != partitaId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsAttiva, false), cancellationToken);

        partita.IsAttiva = true;
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return PartitaAttivaOperationResult.Success("Partita attiva aggiornata.");
    }
}

public sealed record PartitaAttivaOperationResult(bool IsSuccess, bool Changed, string Messaggio)
{
    public static PartitaAttivaOperationResult Success(string messaggio) => new(true, true, messaggio);
    public static PartitaAttivaOperationResult Unchanged(string messaggio) => new(true, false, messaggio);
    public static PartitaAttivaOperationResult Error(string messaggio) => new(false, false, messaggio);
}
