using Core.Entities;
using Core.Services;

namespace Core.Tests;

public class DomandeCsvServiceTests
{
    [Fact]
    public void ExportThenImport_PreservesSupportedFieldsAndItalianText()
    {
        var original = new Domanda
        {
            Categoria = "Scienza e natura", Difficolta = "Difficile", Testo = "Perché l'acqua è \"bagnata\"?",
            RispostaA = "È liquida", RispostaB = "È blu", RispostaC = "Non lo è", RispostaD = "Dipende", RispostaEsatta = 'A', FlagErrore = true
        };

        var result = DomandeCsvService.Import(DomandeCsvService.Export([original]));

        var domanda = Assert.Single(result.Domande);
        Assert.True(result.IsValid);
        Assert.Equal(original.Categoria, domanda.Categoria);
        Assert.Equal(original.Difficolta, domanda.Difficolta);
        Assert.Equal(original.Testo, domanda.Testo);
        Assert.Equal(original.RispostaEsatta, domanda.RispostaEsatta);
        Assert.Equal(original.FlagErrore, domanda.FlagErrore);
    }

    [Fact]
    public void Import_WhenRowsAreInvalid_ReturnsRowNumbersAndDoesNotReturnThoseRows()
    {
        var csv = DomandeCsvService.Intestazione + "\n" +
                  "Storia,Media,Domanda,A,B,C,D,A,False\n" +
                  "Inesistente,Media,Domanda,A,B,C,D,A,False\n" +
                  "Storia,Media,Domanda,A,B,C,D,Z,False";

        var result = DomandeCsvService.Import(csv);

        Assert.False(result.IsValid);
        Assert.Single(result.Domande);
        Assert.Collection(result.Errori,
            errore => Assert.Equal(3, errore.NumeroRiga),
            errore => Assert.Equal(4, errore.NumeroRiga));
    }
}
