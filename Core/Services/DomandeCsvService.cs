using System.Text;
using Core.Entities;

namespace Core.Services;

public static class DomandeCsvService
{
    public const string Intestazione = "Categoria,Difficolta,Testo,RispostaA,RispostaB,RispostaC,RispostaD,RispostaEsatta,FlagErrore";

    public static readonly IReadOnlyList<string> Categorie =
    [
        "Cultura generale", "Storia", "Geografia", "Scienza e natura", "Letteratura", "Arte",
        "Cinema e TV", "Musica", "Sport", "Attualità", "Territorio e associazione", "Bambini", "Altro"
    ];

    public static readonly IReadOnlyList<string> Difficolta = ["Facile", "Media", "Difficile"];

    // Esporta solo i campi del catalogo e produce un CSV UTF-8 riutilizzabile dall'importazione.
    public static string Export(IEnumerable<Domanda> domande)
    {
        var csv = new StringBuilder();
        csv.AppendLine(Intestazione);

        foreach (var domanda in domande)
        {
            csv.AppendLine(string.Join(',',
                Escape(domanda.Categoria), Escape(domanda.Difficolta), Escape(domanda.Testo),
                Escape(domanda.RispostaA), Escape(domanda.RispostaB), Escape(domanda.RispostaC),
                Escape(domanda.RispostaD), Escape(domanda.RispostaEsatta.ToString()),
                Escape(domanda.FlagErrore.ToString())));
        }

        return csv.ToString();
    }

    public static DomandeCsvImportResult Import(string csv)
    {
        var righe = ReadRows(csv);
        var errori = new List<DomandeCsvImportError>();
        var domande = new List<Domanda>();

        if (righe.Count == 0 || !righe[0].SequenceEqual(Intestazione.Split(','), StringComparer.Ordinal))
        {
            errori.Add(new DomandeCsvImportError(1, "L'intestazione CSV non è valida."));
            return new DomandeCsvImportResult(domande, errori);
        }

        for (var index = 1; index < righe.Count; index++)
        {
            var campi = righe[index];
            var numeroRiga = index + 1;
            if (campi.Count == 1 && string.IsNullOrWhiteSpace(campi[0]))
                continue;

            if (campi.Count != 9)
            {
                errori.Add(new DomandeCsvImportError(numeroRiga, "Sono richiesti 9 campi."));
                continue;
            }

            var errore = Validate(campi);
            if (errore is not null)
            {
                errori.Add(new DomandeCsvImportError(numeroRiga, errore));
                continue;
            }

            domande.Add(new Domanda
            {
                Categoria = campi[0],
                Difficolta = campi[1],
                Testo = campi[2],
                RispostaA = campi[3],
                RispostaB = campi[4],
                RispostaC = campi[5],
                RispostaD = campi[6],
                RispostaEsatta = char.ToUpperInvariant(campi[7][0]),
                FlagErrore = bool.Parse(campi[8])
            });
        }

        return new DomandeCsvImportResult(domande, errori);
    }

    private static string? Validate(IReadOnlyList<string> campi)
    {
        if (!Categorie.Contains(campi[0], StringComparer.Ordinal)) return "Categoria non ammessa.";
        if (!Difficolta.Contains(campi[1], StringComparer.Ordinal)) return "Difficoltà non ammessa.";
        if (campi.Skip(2).Take(5).Any(string.IsNullOrWhiteSpace)) return "Testo e risposte A/B/C/D sono obbligatori.";
        if (campi[7].Length != 1 || !"ABCD".Contains(char.ToUpperInvariant(campi[7][0]))) return "La risposta esatta deve essere A, B, C o D.";
        if (!bool.TryParse(campi[8], out _)) return "FlagErrore deve essere True o False.";
        return null;
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static List<List<string>> ReadRows(string csv)
    {
        var righe = new List<List<string>>();
        var riga = new List<string>();
        var campo = new StringBuilder();
        var traVirgolette = false;

        for (var index = 0; index < csv.Length; index++)
        {
            var carattere = csv[index];
            if (carattere == '\"')
            {
                if (traVirgolette && index + 1 < csv.Length && csv[index + 1] == '\"')
                {
                    campo.Append(carattere);
                    index++;
                }
                else traVirgolette = !traVirgolette;
            }
            else if (carattere == ',' && !traVirgolette)
            {
                riga.Add(campo.ToString());
                campo.Clear();
            }
            else if ((carattere == '\r' || carattere == '\n') && !traVirgolette)
            {
                if (carattere == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n') index++;
                riga.Add(campo.ToString());
                righe.Add(riga);
                riga = [];
                campo.Clear();
            }
            else campo.Append(carattere);
        }

        if (traVirgolette)
            return [["__CSV_NON_CHIUSO__"]];

        if (campo.Length > 0 || riga.Count > 0)
        {
            riga.Add(campo.ToString());
            righe.Add(riga);
        }

        if (righe.Count > 0 && righe[0].Count > 0)
            righe[0][0] = righe[0][0].TrimStart('\uFEFF');

        return righe;
    }
}

public sealed record DomandeCsvImportResult(IReadOnlyList<Domanda> Domande, IReadOnlyList<DomandeCsvImportError> Errori)
{
    public bool IsValid => Errori.Count == 0;
}

public sealed record DomandeCsvImportError(int NumeroRiga, string Motivo);
