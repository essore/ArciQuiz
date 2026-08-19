using Core.Entities;
using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class PartitaStateMachineService
{
    // Ricostruisce il contratto runtime esclusivamente dallo stato persistito.
    public static async Task<GameState?> CaricaStatoAsync(
        ArciQuizDbContext database,
        int partitaId,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        var partita = await database.Partite
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == partitaId, cancellationToken);
        if (partita is null)
            return null;

        int? questionIndex = null;
        DateTimeOffset? phaseEndsAtUtc = null;
        if (partita.CurrentMancheDomandaId.HasValue)
        {
            var currentQuestion = await database.ManchesDomande
                .AsNoTracking()
                .Where(item => item.Id == partita.CurrentMancheDomandaId.Value)
                .Select(item => new { item.Index, item.ScadenzaUtc })
                .FirstOrDefaultAsync(cancellationToken);
            questionIndex = currentQuestion?.Index;
            phaseEndsAtUtc = currentQuestion?.ScadenzaUtc;
        }

        var remainingSeconds = RemainingSeconds(phaseEndsAtUtc, (timeProvider ?? TimeProvider.System).GetUtcNow());
        var acceptingAnswers = partita.Fase == GamePhase.ShowingQuestion && remainingSeconds > 0;

        return new GameState(
            partita.Id,
            partita.CurrentMancheId,
            partita.CurrentMancheDomandaId,
            questionIndex,
            partita.Fase,
            phaseEndsAtUtc,
            remainingSeconds,
            acceptingAnswers,
            null,
            0);
    }

    // Apre la prima domanda della prima manche partendo dalla lobby validata.
    public static async Task<PartitaTransitionResult> AvviaPartitaAsync(
        ArciQuizDbContext database,
        int partitaId,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        var partita = await LoadPartitaAsync(database, partitaId, cancellationToken);
        if (partita is null)
            return PartitaTransitionResult.Error("Partita non trovata.");
        if (partita.Stato == PartitaStato.InCorso)
            return PartitaTransitionResult.Unchanged("La partita è già iniziata.");
        if (partita.Stato != PartitaStato.Pronta || partita.Fase != GamePhase.Lobby)
            return PartitaTransitionResult.Error("La partita può iniziare soltanto dalla lobby.");

        var primaManche = OrderedManches(partita).FirstOrDefault();
        var primaDomanda = primaManche is null ? null : OrderedQuestions(primaManche).FirstOrDefault();
        if (primaManche is null || primaDomanda is null)
            return PartitaTransitionResult.Error("La partita non contiene una manche giocabile.");
        if (!HasValidDuration(primaManche, primaDomanda))
            return PartitaTransitionResult.Error("La durata della domanda deve essere maggiore di zero.");

        StartQuestion(partita, primaManche, primaDomanda, (timeProvider ?? TimeProvider.System).GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return PartitaTransitionResult.ChangedResult("Partita avviata e prima domanda aperta.");
    }

    // Chiude la domanda attesa e rende persistente la fase di soluzione.
    public static async Task<PartitaTransitionResult> ChiudiDomandaAsync(
        ArciQuizDbContext database,
        int partitaId,
        int mancheDomandaId,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        var partita = await LoadPartitaAsync(database, partitaId, cancellationToken);
        if (partita is null)
            return PartitaTransitionResult.Error("Partita non trovata.");

        var domanda = FindQuestion(partita, mancheDomandaId);
        if (domanda?.DtEnd is not null)
            return PartitaTransitionResult.Unchanged("La domanda è già chiusa.");
        if (partita.Fase != GamePhase.ShowingQuestion || partita.CurrentMancheDomandaId != mancheDomandaId || domanda is null)
            return PartitaTransitionResult.Error("La domanda indicata non è quella aperta.");

        var nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow();
        domanda.DtEnd = domanda.ScadenzaUtc.HasValue && domanda.ScadenzaUtc.Value < nowUtc
            ? domanda.ScadenzaUtc.Value.UtcDateTime
            : nowUtc.UtcDateTime;
        partita.Fase = GamePhase.ShowingAnswers;
        await database.SaveChangesAsync(cancellationToken);

        await QuestionScoringService.CalcolaEPersistiAsync(database, mancheDomandaId, timeProvider, cancellationToken);

        return PartitaTransitionResult.ChangedResult("Domanda chiusa e soluzione disponibile.");
    }

    // Porta la partita dalla soluzione alla classifica senza modificare i risultati.
    public static async Task<PartitaTransitionResult> MostraClassificaAsync(
        ArciQuizDbContext database,
        int partitaId,
        int mancheDomandaId,
        CancellationToken cancellationToken = default)
    {
        var partita = await LoadPartitaAsync(database, partitaId, cancellationToken);
        if (partita is null)
            return PartitaTransitionResult.Error("Partita non trovata.");
        if (partita.Fase == GamePhase.Leaderboard && partita.CurrentMancheDomandaId == mancheDomandaId)
            return PartitaTransitionResult.Unchanged("La classifica è già visibile.");
        if (partita.CurrentMancheDomandaId != mancheDomandaId && FindQuestion(partita, mancheDomandaId)?.DtEnd is not null)
            return PartitaTransitionResult.Unchanged("Il comando è già stato superato.");
        if (partita.Fase != GamePhase.ShowingAnswers || partita.CurrentMancheDomandaId != mancheDomandaId)
            return PartitaTransitionResult.Error("La classifica può essere mostrata solo dopo la soluzione.");

        partita.Fase = GamePhase.Leaderboard;
        await database.SaveChangesAsync(cancellationToken);
        return PartitaTransitionResult.ChangedResult("Classifica mostrata.");
    }

    // Avanza una sola volta dalla domanda attesa alla successiva nella stessa manche.
    public static async Task<PartitaTransitionResult> AvviaDomandaSuccessivaAsync(
        ArciQuizDbContext database,
        int partitaId,
        int mancheDomandaId,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        var partita = await LoadPartitaAsync(database, partitaId, cancellationToken);
        if (partita is null)
            return PartitaTransitionResult.Error("Partita non trovata.");
        if (partita.CurrentMancheDomandaId != mancheDomandaId && FindQuestion(partita, mancheDomandaId)?.DtEnd is not null)
            return PartitaTransitionResult.Unchanged("La domanda successiva è già stata avviata.");
        if (partita.Fase is not (GamePhase.ShowingAnswers or GamePhase.Leaderboard)
            || partita.CurrentMancheId is null
            || partita.CurrentMancheDomandaId != mancheDomandaId)
            return PartitaTransitionResult.Error("La domanda successiva può iniziare solo dopo la soluzione.");

        var manche = partita.Manches.Single(item => item.Id == partita.CurrentMancheId.Value);
        var domandaCorrente = FindQuestion(partita, mancheDomandaId);
        var domande = OrderedQuestions(manche);
        var domandaSuccessiva = domandaCorrente is null
            ? null
            : domande.FirstOrDefault(item => item.Index > domandaCorrente.Index
                || (item.Index == domandaCorrente.Index && item.Id > domandaCorrente.Id));
        if (domandaSuccessiva is null)
            return PartitaTransitionResult.Error("Non ci sono altre domande: mostrare la classifica e concludere la manche.");
        if (!HasValidDuration(manche, domandaSuccessiva))
            return PartitaTransitionResult.Error("La durata della domanda deve essere maggiore di zero.");

        StartQuestion(partita, manche, domandaSuccessiva, (timeProvider ?? TimeProvider.System).GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return PartitaTransitionResult.ChangedResult("Domanda successiva aperta.");
    }

    // Conclude la manche soltanto dopo la classifica e dopo tutte le sue domande.
    public static async Task<PartitaTransitionResult> ConcludiMancheAsync(
        ArciQuizDbContext database,
        int partitaId,
        int mancheId,
        CancellationToken cancellationToken = default)
    {
        var partita = await LoadPartitaAsync(database, partitaId, cancellationToken);
        if (partita is null)
            return PartitaTransitionResult.Error("Partita non trovata.");

        var manche = partita.Manches.FirstOrDefault(item => item.Id == mancheId);
        if (manche?.Stato == MancheStato.Conclusa)
            return PartitaTransitionResult.Unchanged("La manche è già conclusa.");
        if (partita.Fase != GamePhase.Leaderboard || partita.CurrentMancheId != mancheId || manche is null)
            return PartitaTransitionResult.Error("La manche può concludersi soltanto dopo la classifica.");
        if (OrderedQuestions(manche).Any(item => item.DtEnd is null))
            return PartitaTransitionResult.Error("Ci sono ancora domande da giocare nella manche.");

        manche.Stato = MancheStato.Conclusa;
        manche.DtFineUtc = DateTime.UtcNow;
        partita.Fase = GamePhase.RoundEnded;
        partita.CurrentMancheDomandaId = null;
        await database.SaveChangesAsync(cancellationToken);
        return PartitaTransitionResult.ChangedResult("Manche conclusa.");
    }

    // Apre la prima domanda della manche che segue quella appena conclusa.
    public static async Task<PartitaTransitionResult> AvviaMancheSuccessivaAsync(
        ArciQuizDbContext database,
        int partitaId,
        int mancheConclusaId,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        var partita = await LoadPartitaAsync(database, partitaId, cancellationToken);
        if (partita is null)
            return PartitaTransitionResult.Error("Partita non trovata.");
        if (partita.CurrentMancheId != mancheConclusaId && partita.Stato == PartitaStato.InCorso)
            return PartitaTransitionResult.Unchanged("La manche successiva è già iniziata.");
        if (partita.Fase != GamePhase.RoundEnded || partita.CurrentMancheId != mancheConclusaId)
            return PartitaTransitionResult.Error("La manche corrente non è ancora conclusa.");

        var manches = OrderedManches(partita);
        var currentIndex = manches.FindIndex(item => item.Id == mancheConclusaId);
        var mancheSuccessiva = currentIndex >= 0 ? manches.Skip(currentIndex + 1).FirstOrDefault() : null;
        var primaDomanda = mancheSuccessiva is null ? null : OrderedQuestions(mancheSuccessiva).FirstOrDefault();
        if (mancheSuccessiva is null || primaDomanda is null)
            return PartitaTransitionResult.Error("Non ci sono altre manche: concludere la partita.");
        if (!HasValidDuration(mancheSuccessiva, primaDomanda))
            return PartitaTransitionResult.Error("La durata della domanda deve essere maggiore di zero.");

        StartQuestion(partita, mancheSuccessiva, primaDomanda, (timeProvider ?? TimeProvider.System).GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return PartitaTransitionResult.ChangedResult("Manche successiva avviata.");
    }

    // Chiude definitivamente la partita quando tutte le manche risultano concluse.
    public static async Task<PartitaTransitionResult> ConcludiPartitaAsync(
        ArciQuizDbContext database,
        int partitaId,
        CancellationToken cancellationToken = default)
    {
        var partita = await LoadPartitaAsync(database, partitaId, cancellationToken);
        if (partita is null)
            return PartitaTransitionResult.Error("Partita non trovata.");
        if (partita.Stato == PartitaStato.Conclusa && partita.Fase == GamePhase.Closed)
            return PartitaTransitionResult.Unchanged("La partita è già conclusa.");
        if (partita.Fase != GamePhase.RoundEnded || partita.CurrentMancheId is null)
            return PartitaTransitionResult.Error("La partita può concludersi solo dopo la fine dell'ultima manche.");

        var manches = OrderedManches(partita);
        if (manches.LastOrDefault()?.Id != partita.CurrentMancheId || manches.Any(item => item.Stato != MancheStato.Conclusa))
            return PartitaTransitionResult.Error("Ci sono ancora manche da giocare.");

        partita.Stato = PartitaStato.Conclusa;
        partita.Fase = GamePhase.Closed;
        partita.IsAttiva = false;
        partita.CurrentMancheId = null;
        partita.CurrentMancheDomandaId = null;
        partita.DtFineUtc = DateTime.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return PartitaTransitionResult.ChangedResult("Partita conclusa.");
    }

    private static Task<Partita?> LoadPartitaAsync(
        ArciQuizDbContext database,
        int partitaId,
        CancellationToken cancellationToken) =>
        database.Partite
            .Include(item => item.Manches)
                .ThenInclude(item => item.Domande)
            .FirstOrDefaultAsync(item => item.Id == partitaId, cancellationToken);

    private static List<Manche> OrderedManches(Partita partita) =>
        partita.Manches.OrderBy(item => item.Ordine).ThenBy(item => item.Id).ToList();

    private static List<MancheDomanda> OrderedQuestions(Manche manche) =>
        manche.Domande
            .Where(item => item.DomandaId.HasValue && !item.IsAnnullata)
            .OrderBy(item => item.Index)
            .ThenBy(item => item.Id)
            .ToList();

    private static MancheDomanda? FindQuestion(Partita partita, int mancheDomandaId) =>
        partita.Manches.SelectMany(item => item.Domande).FirstOrDefault(item => item.Id == mancheDomandaId);

    private static bool HasValidDuration(Manche manche, MancheDomanda domanda) =>
        (domanda.DurataSecondiOverride ?? manche.TempoRispostaSecondi) > 0;

    private static void StartQuestion(Partita partita, Manche manche, MancheDomanda domanda, DateTimeOffset nowUtc)
    {
        partita.Stato = PartitaStato.InCorso;
        partita.Fase = GamePhase.ShowingQuestion;
        partita.CurrentMancheId = manche.Id;
        partita.CurrentMancheDomandaId = domanda.Id;
        partita.DtInizioUtc ??= nowUtc.UtcDateTime;
        manche.Stato = MancheStato.InGioco;
        manche.DtInizioUtc ??= nowUtc.UtcDateTime;
        domanda.DtStart ??= nowUtc.UtcDateTime;
        var durationSeconds = domanda.DurataSecondiOverride ?? manche.TempoRispostaSecondi;
        domanda.ScadenzaUtc = nowUtc.AddSeconds(durationSeconds);
    }

    private static int? RemainingSeconds(DateTimeOffset? deadlineUtc, DateTimeOffset nowUtc)
    {
        if (!deadlineUtc.HasValue)
            return null;

        return Math.Max(0, (int)Math.Ceiling((deadlineUtc.Value - nowUtc).TotalSeconds));
    }
}

public sealed record PartitaTransitionResult(bool IsSuccess, bool Changed, string Messaggio)
{
    public static PartitaTransitionResult Error(string messaggio) => new(false, false, messaggio);
    public static PartitaTransitionResult Unchanged(string messaggio) => new(true, false, messaggio);
    public static PartitaTransitionResult ChangedResult(string messaggio) => new(true, true, messaggio);
}
