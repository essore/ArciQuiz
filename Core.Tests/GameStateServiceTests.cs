using Core.Enums;
using Web.Services;

namespace Core.Tests;

public class GameStateServiceTests
{
    [Fact]
    public void SetState_IncrementsVersionForEveryPublishedState()
    {
        var gameStateService = new GameStateService();

        gameStateService.SetState(CreateState(GamePhase.Waiting));
        var firstState = gameStateService.Current;

        gameStateService.SetState(CreateState(GamePhase.ShowingQuestion));
        var secondState = gameStateService.Current;

        Assert.Equal(1, firstState.Version);
        Assert.Equal(firstState.Version + 1, secondState.Version);
        Assert.Equal(GamePhase.ShowingQuestion, secondState.Phase);
    }

    [Fact]
    public void SetState_PreservesQrVisibilityForTheCurrentGame()
    {
        var gameStateService = new GameStateService();

        gameStateService.SetState(CreateState(GamePhase.Waiting));
        gameStateService.SetQrVisibility(1, false);
        gameStateService.SetState(CreateState(GamePhase.ShowingQuestion));

        Assert.False(gameStateService.Current.IsQrVisible);
    }

    private static GameState CreateState(GamePhase phase) => new(
        PartitaId: 1,
        MancheId: 1,
        MancheDomandaId: 1,
        QuestionIndex: 1,
        Phase: phase,
        PhaseEndsAtUtc: null,
        SecondsRemainingHint: null,
        AcceptingAnswers: false,
        PublicMessage: null,
        Version: 0);
}
