using Core.Entities;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class QuestionCancellationService
{
    // Annulla una singola occorrenza giocata e ricalcola le astensioni delle domande successive della manche.
    public static async Task<PartitaTransitionResult> AnnullaAsync(
        ArciQuizDbContext database,
        int partitaId,
        int mancheDomandaId,
        string? motivo,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        var motivoNormalizzato = motivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivoNormalizzato))
            return PartitaTransitionResult.Error("Il motivo dell'annullamento è obbligatorio.");

        var domanda = await database.ManchesDomande
            .Include(item => item.Domanda)
            .Include(item => item.Manche)
            .FirstOrDefaultAsync(item => item.Id == mancheDomandaId, cancellationToken);
        if (domanda?.Manche is null || domanda.Manche.PartitaId != partitaId)
            return PartitaTransitionResult.Error("La domanda indicata non appartiene alla partita.");
        if (domanda.DtEnd is null)
            return PartitaTransitionResult.Error("È possibile annullare soltanto una domanda già chiusa.");
        if (domanda.IsAnnullata)
            return PartitaTransitionResult.Unchanged("La domanda è già annullata.");

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        domanda.IsAnnullata = true;
        domanda.MotivoAnnullamento = motivoNormalizzato;
        domanda.DtAnnullamentoUtc = nowUtc;
        if (domanda.Domanda is not null)
            domanda.Domanda.FlagErrore = true;

        var risposte = await database.ManchesRisposte
            .Where(item => item.MancheDomandaId == domanda.Id)
            .ToListAsync(cancellationToken);
        foreach (var risposta in risposte)
            risposta.IsAnnullata = true;

        await database.SaveChangesAsync(cancellationToken);

        var domandeSuccessiveIds = await database.ManchesDomande
            .Where(item => item.MancheId == domanda.MancheId
                && !item.IsAnnullata
                && item.DtEnd.HasValue
                && (item.Index > domanda.Index || (item.Index == domanda.Index && item.Id > domanda.Id)))
            .OrderBy(item => item.Index)
            .ThenBy(item => item.Id)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        foreach (var domandaSuccessivaId in domandeSuccessiveIds)
            await QuestionScoringService.CalcolaEPersistiAsync(database, domandaSuccessivaId, timeProvider, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return PartitaTransitionResult.ChangedResult("Domanda annullata e punteggi ricalcolati.");
    }
}
