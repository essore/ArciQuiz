using Core.Entities;

namespace Core.Services;

public sealed record PartitaProntaValidationResult(IReadOnlyList<string> Errori)
{
    public bool IsValid => Errori.Count == 0;
}

public static class PartitaProntaValidator
{
    // Verifica che la configurazione contenga manche con domande valide e ordinate in modo univoco.
    public static PartitaProntaValidationResult Validate(Partita partita)
    {
        ArgumentNullException.ThrowIfNull(partita);

        var errori = new List<string>();

        if (partita.Manches.Count == 0)
        {
            errori.Add("Aggiungi almeno una manche prima di dichiarare la partita pronta.");
            return new PartitaProntaValidationResult(errori);
        }

        foreach (var manche in partita.Manches)
        {
            if (manche.TempoRispostaSecondi <= 0)
            {
                errori.Add($"La manche #{manche.Id} deve avere un tempo di risposta maggiore di zero.");
            }

            var domandeGiocabili = manche.Domande
                .Where(mancheDomanda => mancheDomanda.DomandaId.HasValue)
                .ToList();

            if (domandeGiocabili.Count == 0)
            {
                errori.Add($"La manche #{manche.Id} non contiene alcuna domanda valida.");
            }

            if (manche.Domande.GroupBy(mancheDomanda => mancheDomanda.Index).Any(gruppo => gruppo.Count() > 1))
            {
                errori.Add($"La manche #{manche.Id} contiene domande con lo stesso ordine.");
            }

            foreach (var mancheDomanda in domandeGiocabili)
            {
                if (mancheDomanda.DurataSecondiOverride <= 0)
                {
                    errori.Add($"La durata personalizzata della domanda alla posizione {mancheDomanda.Index} deve essere maggiore di zero.");
                }

                if (!IsDomandaValida(mancheDomanda.Domanda))
                {
                    errori.Add($"La domanda alla posizione {mancheDomanda.Index} della manche #{manche.Id} non è valida.");
                }
            }
        }

        return new PartitaProntaValidationResult(errori);
    }

    private static bool IsDomandaValida(Domanda? domanda)
    {
        return domanda is not null
            && !domanda.FlgDeleted
            && !domanda.FlagErrore
            && !string.IsNullOrWhiteSpace(domanda.Categoria)
            && !string.IsNullOrWhiteSpace(domanda.Difficolta)
            && !string.IsNullOrWhiteSpace(domanda.Testo)
            && !string.IsNullOrWhiteSpace(domanda.RispostaA)
            && !string.IsNullOrWhiteSpace(domanda.RispostaB)
            && !string.IsNullOrWhiteSpace(domanda.RispostaC)
            && !string.IsNullOrWhiteSpace(domanda.RispostaD)
            && "ABCD".Contains(char.ToUpperInvariant(domanda.RispostaEsatta));
    }
}
