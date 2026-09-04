using Core.Enums;
using Core.Services;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public static class PartitaPreparazioneService
{
    // Espone le sole transizioni della preparazione, lasciando alla regia quelle di svolgimento.
    public static async Task<PartitaPreparazioneResult> DichiaraProntaAsync(
        ArciQuizDbContext database,
        int partitaId,
        CancellationToken cancellationToken = default)
    {
        var partita = await CaricaPartitaAsync(database, partitaId, cancellationToken);
        if (partita is null)
            return PartitaPreparazioneResult.Error("Partita non trovata.");
        if (partita.Stato is PartitaStato.InCorso or PartitaStato.Conclusa)
            return PartitaPreparazioneResult.Error("Una partita avviata o conclusa non può tornare alla lobby dalla preparazione.");

        var validationResult = PartitaProntaValidator.Validate(partita);
        if (!validationResult.IsValid)
            return PartitaPreparazioneResult.RequisitiDaCompletare(validationResult.Errori);

        partita.Stato = PartitaStato.Pronta;
        partita.Fase = GamePhase.Lobby;
        await database.SaveChangesAsync(cancellationToken);
        return PartitaPreparazioneResult.Success("Partita pronta: puoi aprire la lobby e iniziare le iscrizioni.");
    }

    public static async Task<PartitaPreparazioneResult> TornaInPreparazioneAsync(
        ArciQuizDbContext database,
        int partitaId,
        CancellationToken cancellationToken = default)
    {
        var partita = await database.Partite.FirstOrDefaultAsync(item => item.Id == partitaId, cancellationToken);
        if (partita is null)
            return PartitaPreparazioneResult.Error("Partita non trovata.");
        if (partita.Stato is PartitaStato.InCorso or PartitaStato.Conclusa)
            return PartitaPreparazioneResult.Error("Una partita avviata o conclusa non può essere modificata dalla preparazione.");
        if (partita.Stato != PartitaStato.Pronta)
            return PartitaPreparazioneResult.Unchanged("La partita è già in preparazione.");

        partita.Stato = PartitaStato.Configurazione;
        partita.Fase = GamePhase.Idle;
        await database.SaveChangesAsync(cancellationToken);
        return PartitaPreparazioneResult.Success("Partita riportata in preparazione. I dati già salvati restano disponibili.");
    }

    private static Task<Core.Entities.Partita?> CaricaPartitaAsync(
        ArciQuizDbContext database,
        int partitaId,
        CancellationToken cancellationToken)
    {
        return database.Partite
            .Include(item => item.Manches)
                .ThenInclude(manche => manche.Domande)
                    .ThenInclude(mancheDomanda => mancheDomanda.Domanda)
            .FirstOrDefaultAsync(item => item.Id == partitaId, cancellationToken);
    }
}

public sealed record PartitaPreparazioneResult(
    bool IsSuccess,
    bool Changed,
    string Messaggio,
    IReadOnlyList<string> RequisitiMancanti)
{
    public static PartitaPreparazioneResult Success(string messaggio) => new(true, true, messaggio, []);

    public static PartitaPreparazioneResult Unchanged(string messaggio) => new(true, false, messaggio, []);

    public static PartitaPreparazioneResult RequisitiDaCompletare(IReadOnlyList<string> requisiti) => new(
        false,
        false,
        "Completa i requisiti indicati prima di aprire la lobby.",
        requisiti);

    public static PartitaPreparazioneResult Error(string messaggio) => new(false, false, messaggio, []);
}
