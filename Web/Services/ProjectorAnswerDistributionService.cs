using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public sealed record ProjectorAnswerDistribution(int AnswerA, int AnswerB, int AnswerC, int AnswerD);

public static class ProjectorAnswerDistributionService
{
    // Aggrega esclusivamente le risposte confermate della domanda già chiusa.
    public static async Task<ProjectorAnswerDistribution> LoadAsync(
        ArciQuizDbContext database,
        int mancheDomandaId,
        CancellationToken cancellationToken = default)
    {
        var answers = await database.ManchesRisposte
            .AsNoTracking()
            .Where(item => item.MancheDomandaId == mancheDomandaId
                && !item.IsAstenuto
                && !item.IsAnnullata
                && item.Risposta.HasValue)
            .Select(item => item.Risposta!.Value)
            .ToListAsync(cancellationToken);

        return Create(answers);
    }

    public static ProjectorAnswerDistribution Create(IEnumerable<char> answers)
    {
        var answerA = 0;
        var answerB = 0;
        var answerC = 0;
        var answerD = 0;

        foreach (var answer in answers)
        {
            switch (char.ToUpperInvariant(answer))
            {
                case 'A': answerA++; break;
                case 'B': answerB++; break;
                case 'C': answerC++; break;
                case 'D': answerD++; break;
            }
        }

        return new ProjectorAnswerDistribution(
            answerA,
            answerB,
            answerC,
            answerD);
    }
}
