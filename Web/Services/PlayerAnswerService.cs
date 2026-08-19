using Core.Entities;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class PlayerAnswerService
{
    // Registra la prima risposta valida della squadra, senza permettere modifiche o duplicati.
    public static async Task<PlayerAnswerResult> InviaAsync(
        ArciQuizDbContext database,
        int playerId,
        string? sessionToken,
        int partitaId,
        int mancheDomandaId,
        char risposta,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var codiceRisposta = char.ToUpperInvariant(risposta);
        if (codiceRisposta is not ('A' or 'B' or 'C' or 'D'))
            return PlayerAnswerResult.Rejected("Seleziona una risposta valida.");

        if (!await PlayerSessionService.SessioneValidaAsync(database, playerId, sessionToken))
            return PlayerAnswerResult.SessionRejected("La sessione è stata sostituita da un accesso più recente.");

        var timing = await GameTimerService.VerificaRispostaAsync(
            database,
            partitaId,
            mancheDomandaId,
            timeProvider,
            cancellationToken);
        if (!timing.IsAccepted)
            return PlayerAnswerResult.Rejected(timing.Messaggio);

        var existingAnswer = await database.ManchesRisposte
            .AsNoTracking()
            .Where(item => item.PlayerId == playerId && item.MancheDomandaId == mancheDomandaId)
            .Select(item => item.Risposta)
            .SingleOrDefaultAsync(cancellationToken);
        if (existingAnswer.HasValue)
            return PlayerAnswerResult.AlreadyRecorded(existingAnswer.Value);

        var questionStartedAtUtc = await database.ManchesDomande
            .AsNoTracking()
            .Where(item => item.Id == mancheDomandaId)
            .Select(item => item.DtStart)
            .SingleOrDefaultAsync(cancellationToken);
        var nowUtc = timeProvider.GetUtcNow();
        var elapsedMilliseconds = questionStartedAtUtc.HasValue
            ? Math.Max(0, (int)Math.Min((nowUtc.UtcDateTime - questionStartedAtUtc.Value).TotalMilliseconds, int.MaxValue))
            : 0;

        database.ManchesRisposte.Add(new MancheRispostaRicevuta
        {
            PlayerId = playerId,
            MancheDomandaId = mancheDomandaId,
            Risposta = codiceRisposta,
            DtRisposta = nowUtc.UtcDateTime,
            TempoImpiegatoMs = elapsedMilliseconds
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return PlayerAnswerResult.Accepted(codiceRisposta);
        }
        catch (DbUpdateException)
        {
            database.ChangeTracker.Clear();
            var recordedAnswer = await database.ManchesRisposte
                .AsNoTracking()
                .Where(item => item.PlayerId == playerId && item.MancheDomandaId == mancheDomandaId)
                .Select(item => item.Risposta)
                .SingleOrDefaultAsync(cancellationToken);
            return recordedAnswer.HasValue
                ? PlayerAnswerResult.AlreadyRecorded(recordedAnswer.Value)
                : PlayerAnswerResult.Rejected("Non è stato possibile registrare la risposta. Riprova.");
        }
    }
}

public sealed record PlayerAnswerResult(bool IsAccepted, bool SessionValid, bool AlreadySubmitted, char? RecordedAnswer, string Message)
{
    public static PlayerAnswerResult Accepted(char answer) =>
        new(true, true, false, answer, "Risposta confermata.");

    public static PlayerAnswerResult AlreadyRecorded(char answer) =>
        new(true, true, true, answer, "La risposta era già stata confermata.");

    public static PlayerAnswerResult SessionRejected(string message) =>
        new(false, false, false, null, message);

    public static PlayerAnswerResult Rejected(string message) =>
        new(false, true, false, null, message);
}
