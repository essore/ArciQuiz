using Core.Entities;
using Core.Services;

namespace Core.Tests;

public class PartitaProntaValidatorTests
{
    [Fact]
    public void Validate_WhenPartitaHasNoManches_ReturnsItalianError()
    {
        var result = PartitaProntaValidator.Validate(new Partita());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errori, errore => errore.Contains("Aggiungi almeno una manche"));
    }

    [Fact]
    public void Validate_WhenMancheHasNoDomande_ReturnsItalianError()
    {
        var partita = new Partita
        {
            Manches = { new Manche { Id = 1 } }
        };

        var result = PartitaProntaValidator.Validate(partita);

        Assert.False(result.IsValid);
        Assert.Contains("La manche #1 non contiene alcuna domanda valida.", result.Errori);
    }

    [Fact]
    public void Validate_WhenDomandaIsMarkedAsError_ReturnsItalianError()
    {
        var partita = CreatePartitaWithValidDomanda();
        partita.Manches.Single().Domande.Single().Domanda!.FlagErrore = true;

        var result = PartitaProntaValidator.Validate(partita);

        Assert.False(result.IsValid);
        Assert.Contains("La domanda alla posizione 1 della manche #1 non è valida.", result.Errori);
    }

    [Fact]
    public void Validate_WhenDomandeHaveSameOrder_ReturnsItalianError()
    {
        var partita = CreatePartitaWithValidDomanda();
        partita.Manches.Single().Domande.Add(new MancheDomanda
        {
            DomandaId = 2,
            Domanda = CreateValidDomanda(),
            Index = 1
        });

        var result = PartitaProntaValidator.Validate(partita);

        Assert.False(result.IsValid);
        Assert.Contains("La manche #1 contiene domande con lo stesso ordine.", result.Errori);
    }

    [Fact]
    public void Validate_WhenMancheHasOrderedValidDomande_ReturnsValidResult()
    {
        var result = PartitaProntaValidator.Validate(CreatePartitaWithValidDomanda());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errori);
    }

    [Fact]
    public void Validate_WhenTimerIsNotPositive_ReturnsItalianErrors()
    {
        var partita = CreatePartitaWithValidDomanda();
        var manche = partita.Manches.Single();
        manche.TempoRispostaSecondi = 0;
        manche.Domande.Single().DurataSecondiOverride = -1;

        var result = PartitaProntaValidator.Validate(partita);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errori, errore => errore.Contains("tempo di risposta maggiore di zero"));
        Assert.Contains(result.Errori, errore => errore.Contains("durata personalizzata"));
    }

    private static Partita CreatePartitaWithValidDomanda()
    {
        return new Partita
        {
            Manches =
            {
                new Manche
                {
                    Id = 1,
                    Domande =
                    {
                        new MancheDomanda
                        {
                            DomandaId = 1,
                            Domanda = CreateValidDomanda(),
                            Index = 1
                        }
                    }
                }
            }
        };
    }

    private static Domanda CreateValidDomanda()
    {
        return new Domanda
        {
            Categoria = "Storia",
            Difficolta = "Media",
            Testo = "In quale anno fu scoperta l'America?",
            RispostaA = "1492",
            RispostaB = "1500",
            RispostaC = "1453",
            RispostaD = "1517",
            RispostaEsatta = 'A'
        };
    }
}
