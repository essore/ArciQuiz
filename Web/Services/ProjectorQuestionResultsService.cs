using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public sealed record ProjectorQuestionAnswer(
    string TeamName,
    bool IsAbstained,
    bool IsCorrect,
    char? Answer,
    int? ElapsedMilliseconds);

public sealed record ProjectorFastestAnswer(IReadOnlyList<string> TeamNames, int ElapsedMilliseconds)
{
    public bool IsTie => TeamNames.Count > 1;
}

public sealed record ProjectorQuestionResults(
    int CorrectAnswers,
    int IncorrectAnswers,
    int Abstentions,
    ProjectorFastestAnswer? FastestAnswer);

public static class ProjectorQuestionResultsService
{
    // Riassume solo gli esiti persistiti della domanda già chiusa per la vista pubblica.
    public static async Task<ProjectorQuestionResults> LoadAsync(
        ArciQuizDbContext database,
        int mancheDomandaId,
        CancellationToken cancellationToken = default)
    {
        var answers = await database.ManchesRisposte
            .AsNoTracking()
            .Where(item => item.MancheDomandaId == mancheDomandaId && !item.IsAnnullata)
            .Select(item => new ProjectorQuestionAnswer(
                item.Player!.NomeSquadra,
                item.IsAstenuto,
                item.IsCorrect,
                item.Risposta,
                item.TempoImpiegatoMs))
            .ToListAsync(cancellationToken);

        return Create(answers);
    }

    public static ProjectorQuestionResults Create(IEnumerable<ProjectorQuestionAnswer> answers)
    {
        var recordedAnswers = answers.ToList();
        var correctAnswers = recordedAnswers.Count(item => !item.IsAbstained && item.IsCorrect);
        var incorrectAnswers = recordedAnswers.Count(item => !item.IsAbstained && !item.IsCorrect);
        var abstentions = recordedAnswers.Count(item => item.IsAbstained);
        var validAnswers = recordedAnswers
            .Where(item => !item.IsAbstained && item.Answer is 'A' or 'B' or 'C' or 'D')
            .Select(item => new { item.TeamName, ElapsedMilliseconds = item.ElapsedMilliseconds ?? 0 })
            .ToList();

        if (validAnswers.Count == 0)
            return new ProjectorQuestionResults(correctAnswers, incorrectAnswers, abstentions, null);

        var elapsedMilliseconds = validAnswers.Min(item => item.ElapsedMilliseconds);
        var teamNames = validAnswers
            .Where(item => item.ElapsedMilliseconds == elapsedMilliseconds)
            .Select(item => item.TeamName)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ProjectorQuestionResults(
            correctAnswers,
            incorrectAnswers,
            abstentions,
            new ProjectorFastestAnswer(teamNames, elapsedMilliseconds));
    }
}
