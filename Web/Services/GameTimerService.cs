using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class GameTimerService
{
    // Chiude in modo idempotente tutte le domande aperte la cui scadenza server è trascorsa.
    public static async Task<IReadOnlyList<int>> ChiudiDomandeScaduteAsync(
        ArciQuizDbContext database,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var partite = await database.Partite
            .Where(item => item.Fase == GamePhase.ShowingQuestion && item.CurrentMancheDomandaId.HasValue)
            .ToListAsync(cancellationToken);
        if (partite.Count == 0)
            return [];

        var questionIds = partite.Select(item => item.CurrentMancheDomandaId!.Value).ToList();
        var questions = await database.ManchesDomande
            .Where(item => questionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var nowUtc = timeProvider.GetUtcNow();
        var changedPartitaIds = new List<int>();

        foreach (var partita in partite)
        {
            if (!questions.TryGetValue(partita.CurrentMancheDomandaId!.Value, out var question)
                || !question.ScadenzaUtc.HasValue
                || question.ScadenzaUtc.Value > nowUtc)
                continue;

            question.DtEnd ??= question.ScadenzaUtc.Value.UtcDateTime;
            partita.Fase = GamePhase.ShowingAnswers;
            changedPartitaIds.Add(partita.Id);
        }

        if (changedPartitaIds.Count > 0)
            await database.SaveChangesAsync(cancellationToken);

        return changedPartitaIds;
    }

    // Verifica l'istante sul server e rifiuta una risposta non riferita alla domanda ancora aperta.
    public static async Task<AnswerTimingResult> VerificaRispostaAsync(
        ArciQuizDbContext database,
        int partitaId,
        int mancheDomandaId,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        await ChiudiDomandeScaduteAsync(database, timeProvider, cancellationToken);

        var partita = await database.Partite
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == partitaId, cancellationToken);
        if (partita is null)
            return AnswerTimingResult.Rejected("Partita non trovata.");
        if (partita.Fase != GamePhase.ShowingQuestion || partita.CurrentMancheDomandaId != mancheDomandaId)
            return AnswerTimingResult.Rejected("La domanda non accetta più risposte.");

        var deadlineUtc = await database.ManchesDomande
            .AsNoTracking()
            .Where(item => item.Id == mancheDomandaId)
            .Select(item => item.ScadenzaUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (!deadlineUtc.HasValue || timeProvider.GetUtcNow() >= deadlineUtc.Value)
            return AnswerTimingResult.Rejected("Tempo scaduto: la risposta non è stata registrata.");

        return AnswerTimingResult.Accepted(deadlineUtc.Value);
    }
}

public sealed record AnswerTimingResult(bool IsAccepted, string Messaggio, DateTimeOffset? DeadlineUtc)
{
    public static AnswerTimingResult Accepted(DateTimeOffset deadlineUtc) =>
        new(true, string.Empty, deadlineUtc);

    public static AnswerTimingResult Rejected(string messaggio) =>
        new(false, messaggio, null);
}
