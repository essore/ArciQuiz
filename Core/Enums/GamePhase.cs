using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Enums
{
    public enum GamePhase
    {
        Idle = 0,
        Waiting = 1,
        ShowingQuestion = 2,
        ShowingAnswers = 3,
        Leaderboard = 4,
        Closed = 5,
        Lobby = 6,
        RoundEnded = 7
    }

    public sealed record GameState(
       int PartitaId,
       int? MancheId,
       int? MancheDomandaId,
       int? QuestionIndex,
       GamePhase Phase,
       DateTimeOffset? PhaseEndsAtUtc,
       int? SecondsRemainingHint,
       bool AcceptingAnswers,
       string? PublicMessage,
       long Version
   );

}
