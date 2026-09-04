using Core.Entities;

namespace Core.Services;

public sealed record MancheRulesValidationResult(IReadOnlyList<string> Errori)
{
    public bool IsValid => Errori.Count == 0;
}

public static class MancheRulesValidator
{
    // Mantiene i limiti della configurazione coerenti tra la UI e il salvataggio lato server.
    public static MancheRulesValidationResult Validate(Manche manche)
    {
        ArgumentNullException.ThrowIfNull(manche);

        var errori = new List<string>();

        if (manche.TempoRispostaSecondi is < 5 or > 300)
            errori.Add("Il tempo di risposta deve essere compreso tra 5 e 300 secondi.");

        if (manche.PuntiBase is < 0 or > 100000)
            errori.Add("I punti base devono essere compresi tra 0 e 100000.");

        if (manche.MalusBase is < 0 or > 100000)
            errori.Add("Il malus base deve essere compreso tra 0 e 100000.");

        if (manche.Moltiplicatore is < 1 or > 10)
            errori.Add("Il moltiplicatore della manche deve essere compreso tra 1 e 10.");

        if (manche.MaxAstensioni is < 0 or > 10)
            errori.Add("Il numero massimo di astensioni gratuite deve essere compreso tra 0 e 10.");

        return new MancheRulesValidationResult(errori);
    }
}
