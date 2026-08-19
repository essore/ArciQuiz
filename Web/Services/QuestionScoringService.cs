using Core.Entities;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class QuestionScoringService
{
    // Calcola il coefficiente tempo da 1.0 (immediato) a 0.5 (allo scadere)
    public static double CalcolaCoefficienteTempo(int? tempoImpiegatoMs, int durataSecondi)
    {
        if (durataSecondi <= 0)
            return 1.0;

        var durataMs = (double)durataSecondi * 1000.0;
        var msImpiegati = tempoImpiegatoMs.HasValue ? Math.Max(0.0, tempoImpiegatoMs.Value) : 0.0;
        var msRimanenti = Math.Max(0.0, durataMs - msImpiegati);
        var ratio = Math.Min(1.0, msRimanenti / durataMs);

        return 0.5 + (0.5 * ratio);
    }

    // Calcola i punti per una risposta corretta con arrotondamento deterministico AwayFromZero
    public static int CalcolaPuntiCorretti(int puntiBase, int moltiplicatoreManche, int modificatoreDomanda, double coefficienteTempo)
    {
        var moltiplicatoreTotale = moltiplicatoreManche * modificatoreDomanda;
        var rawPunti = puntiBase * moltiplicatoreTotale * coefficienteTempo;
        return (int)Math.Round(rawPunti, MidpointRounding.AwayFromZero);
    }

    // Calcola il malus per risposta errata o astensione oltre soglia
    public static int CalcolaMalus(int malusBase, int moltiplicatoreManche, int modificatoreDomanda, bool penalitaErrore)
    {
        if (!penalitaErrore)
            return 0;

        var moltiplicatoreTotale = moltiplicatoreManche * modificatoreDomanda;
        return -(malusBase * moltiplicatoreTotale);
    }

    // Registra le astensioni mancanti e calcola i punteggi per tutte le risposte della domanda
    public static async Task CalcolaEPersistiAsync(
        ArciQuizDbContext database,
        int mancheDomandaId,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        var mancheDomanda = await database.ManchesDomande
            .Include(md => md.Domanda)
            .Include(md => md.Manche)
                .ThenInclude(m => m!.Partita)
            .FirstOrDefaultAsync(md => md.Id == mancheDomandaId, cancellationToken);

        if (mancheDomanda is null || mancheDomanda.Manche is null || mancheDomanda.Manche.Partita is null || mancheDomanda.Domanda is null)
            return;

        var manche = mancheDomanda.Manche;
        var partitaId = manche.PartitaId;
        var durataSecondi = mancheDomanda.DurataSecondiOverride ?? manche.TempoRispostaSecondi;
        var rispostaEsatta = mancheDomanda.Domanda.RispostaEsatta;

        var malus = CalcolaMalus(manche.MalusBase, manche.Moltiplicatore, mancheDomanda.ModificatorePunti, manche.PenalitaErrore);

        // 1. Recupera tutte le squadre iscritte alla partita
        var playerIds = await database.Players
            .Where(p => p.PartitaId == partitaId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        // 2. Recupera le risposte già registrate per questa domanda
        var risposteEsistenti = await database.ManchesRisposte
            .Where(mr => mr.MancheDomandaId == mancheDomandaId)
            .ToListAsync(cancellationToken);

        var risposteMap = risposteEsistenti.ToDictionary(r => r.PlayerId);
        var nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;

        // 3. Aggiungi astensioni per le squadre senza risposta
        foreach (var playerId in playerIds)
        {
            if (!risposteMap.ContainsKey(playerId))
            {
                var nuovaAstensione = new MancheRispostaRicevuta
                {
                    PlayerId = playerId,
                    MancheDomandaId = mancheDomandaId,
                    DtRisposta = nowUtc,
                    Risposta = null,
                    IsAstenuto = true,
                    IsCorrect = false,
                    TempoImpiegatoMs = null,
                    CoefficienteTempo = null
                };
                database.ManchesRisposte.Add(nuovaAstensione);
                risposteEsistenti.Add(nuovaAstensione);
                risposteMap[playerId] = nuovaAstensione;
            }
        }

        // 4. Conteggia astensioni precedenti nella stessa manche per la soglia MaxAstensioni
        var domandePrecedentiMancheIds = await database.ManchesDomande
            .Where(md => md.MancheId == manche.Id && !md.IsAnnullata && (md.Index < mancheDomanda.Index || (md.Index == mancheDomanda.Index && md.Id < mancheDomanda.Id)))
            .Select(md => md.Id)
            .ToListAsync(cancellationToken);

        Dictionary<int, int> astensioniPrecedentiConteggio = new();
        if (domandePrecedentiMancheIds.Count > 0)
        {
            astensioniPrecedentiConteggio = await database.ManchesRisposte
                .Where(mr => domandePrecedentiMancheIds.Contains(mr.MancheDomandaId) && mr.IsAstenuto && !mr.IsAnnullata)
                .GroupBy(mr => mr.PlayerId)
                .Select(g => new { PlayerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.PlayerId, g => g.Count, cancellationToken);
        }

        // 5. Assegna esiti e punteggi a tutte le risposte
        foreach (var risposta in risposteEsistenti)
        {
            if (risposta.IsAnnullata)
                continue;

            if (risposta.IsAstenuto || !risposta.Risposta.HasValue)
            {
                risposta.IsAstenuto = true;
                risposta.Risposta = null;
                risposta.IsCorrect = false;
                risposta.CoefficienteTempo = null;

                var astensioniPrecedenti = astensioniPrecedentiConteggio.GetValueOrDefault(risposta.PlayerId, 0);
                var astensioneCorrenteNumero = astensioniPrecedenti + 1;

                if (astensioneCorrenteNumero <= manche.MaxAstensioni)
                {
                    risposta.PuntiAssegnati = 0;
                    risposta.AstensionePenalizzata = false;
                }
                else
                {
                    risposta.PuntiAssegnati = malus;
                    risposta.AstensionePenalizzata = true;
                }
            }
            else
            {
                var isCorretto = char.ToUpperInvariant(risposta.Risposta.Value) == char.ToUpperInvariant(rispostaEsatta);
                risposta.IsCorrect = isCorretto;
                risposta.IsAstenuto = false;
                risposta.AstensionePenalizzata = false;

                if (isCorretto)
                {
                    var coeff = CalcolaCoefficienteTempo(risposta.TempoImpiegatoMs, durataSecondi);
                    risposta.CoefficienteTempo = coeff;
                    risposta.PuntiAssegnati = CalcolaPuntiCorretti(manche.PuntiBase, manche.Moltiplicatore, mancheDomanda.ModificatorePunti, coeff);
                }
                else
                {
                    risposta.CoefficienteTempo = null;
                    risposta.PuntiAssegnati = malus;
                }
            }
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}
