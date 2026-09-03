using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class PartitaAttivaService
{
    // Verifica se l'apertura della regia richiede di confermare la sostituzione della partita attiva.
    public static async Task<PartitaAttivaVerificationResult> VerificaAttivazioneAsync(
        ArciQuizDbContext database,
        int partitaId,
        CancellationToken cancellationToken = default)
    {
        var partita = await database.Partite
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == partitaId, cancellationToken);

        if (partita is null)
            return PartitaAttivaVerificationResult.Error("Partita non trovata.");
        if (partita.Stato == PartitaStato.Conclusa)
            return PartitaAttivaVerificationResult.Error("Una partita conclusa non può diventare attiva.");
        if (partita.IsAttiva)
            return PartitaAttivaVerificationResult.Ready("La partita è già attiva.");

        var partitaAttiva = await database.Partite
            .AsNoTracking()
            .Where(item => item.IsAttiva && item.Id != partitaId)
            .Select(item => item.Titolo)
            .FirstOrDefaultAsync(cancellationToken);

        return partitaAttiva is null
            ? PartitaAttivaVerificationResult.Ready("La partita sarà resa attiva.")
            : PartitaAttivaVerificationResult.ConfirmationRequired(partitaAttiva);
    }

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

public sealed record PartitaAttivaVerificationResult(
    bool IsSuccess,
    bool RequiresConfirmation,
    string Messaggio,
    string? PartitaAttivaTitolo)
{
    public static PartitaAttivaVerificationResult Ready(string messaggio) => new(true, false, messaggio, null);

    public static PartitaAttivaVerificationResult ConfirmationRequired(string partitaAttivaTitolo) => new(
        true,
        true,
        $"Aprendo questa regia verrà sostituita la partita attiva: {partitaAttivaTitolo}.",
        partitaAttivaTitolo);

    public static PartitaAttivaVerificationResult Error(string messaggio) => new(false, false, messaggio, null);
}
