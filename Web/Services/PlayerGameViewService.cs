using Core.Enums;
using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public enum PlayerGameScreen
{
    Waiting,
    Question,
    TimeExpired,
    Solution,
    Leaderboard,
    RoundEnded,
    GameEnded
}

public sealed record PlayerAnswerOption(char Code, string Text);

public sealed record PlayerGameView(
    int PartitaId,
    string GameTitle,
    string TeamName,
    PlayerGameScreen Screen,
    int? QuestionIndex,
    string? QuestionText,
    IReadOnlyList<PlayerAnswerOption> Options,
    char? CorrectAnswer,
    DateTimeOffset? DeadlineUtc);

public sealed record PlayerGameViewResult(bool SessionValid, PlayerGameView? View);

public static class PlayerGameViewService
{
    // Costruisce la vista mobile filtrando soluzione e dati della domanda in base alla fase persistita.
    public static async Task<PlayerGameViewResult> LoadAsync(
        ArciQuizDbContext database,
        int playerId,
        string? sessionToken,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        if (!await PlayerSessionService.SessioneValidaAsync(database, playerId, sessionToken))
            return new PlayerGameViewResult(false, null);

        var player = await database.Players
            .AsNoTracking()
            .Where(item => item.Id == playerId && item.PartitaId.HasValue)
            .Select(item => new
            {
                item.NomeSquadra,
                PartitaId = item.PartitaId!.Value,
                GameTitle = item.Partita!.Titolo,
                GameState = item.Partita.Stato,
                GamePhase = item.Partita.Fase,
                item.Partita.CurrentMancheDomandaId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (player is null)
            return new PlayerGameViewResult(false, null);

        var question = player.CurrentMancheDomandaId.HasValue
            ? await database.ManchesDomande
                .AsNoTracking()
                .Where(item => item.Id == player.CurrentMancheDomandaId.Value && item.Domanda != null)
                .Select(item => new
                {
                    item.Index,
                    item.ScadenzaUtc,
                    item.Domanda!.Testo,
                    item.Domanda.RispostaA,
                    item.Domanda.RispostaB,
                    item.Domanda.RispostaC,
                    item.Domanda.RispostaD,
                    item.Domanda.RispostaEsatta
                })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var screen = ResolveScreen(player.GameState, player.GamePhase, question?.ScadenzaUtc, timeProvider.GetUtcNow());
        var showQuestion = screen is PlayerGameScreen.Question or PlayerGameScreen.TimeExpired or PlayerGameScreen.Solution;
        var options = showQuestion && question is not null
            ? new PlayerAnswerOption[]
            {
                new('A', question.RispostaA),
                new('B', question.RispostaB),
                new('C', question.RispostaC),
                new('D', question.RispostaD)
            }
            : [];

        return new PlayerGameViewResult(true, new PlayerGameView(
            player.PartitaId,
            player.GameTitle,
            player.NomeSquadra,
            screen,
            showQuestion ? question?.Index : null,
            showQuestion ? question?.Testo : null,
            options,
            screen == PlayerGameScreen.Solution ? question?.RispostaEsatta : null,
            screen == PlayerGameScreen.Question ? question?.ScadenzaUtc : null));
    }

    private static PlayerGameScreen ResolveScreen(
        PartitaStato gameState,
        GamePhase gamePhase,
        DateTimeOffset? deadlineUtc,
        DateTimeOffset nowUtc)
    {
        if (gameState == PartitaStato.Conclusa || gamePhase == GamePhase.Closed)
            return PlayerGameScreen.GameEnded;

        return gamePhase switch
        {
            GamePhase.ShowingQuestion when deadlineUtc.HasValue && nowUtc >= deadlineUtc.Value => PlayerGameScreen.TimeExpired,
            GamePhase.ShowingQuestion => PlayerGameScreen.Question,
            GamePhase.ShowingAnswers => PlayerGameScreen.Solution,
            GamePhase.Leaderboard => PlayerGameScreen.Leaderboard,
            GamePhase.RoundEnded => PlayerGameScreen.RoundEnded,
            _ => PlayerGameScreen.Waiting
        };
    }
}
