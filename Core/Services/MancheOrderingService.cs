using Core.Entities;

namespace Core.Services;

public static class MancheOrderingService
{
    public static List<Manche> Order(IEnumerable<Manche> manches) =>
        manches.OrderBy(item => item.Ordine).ThenBy(item => item.Id).ToList();

    public static Manche? GetNext(IEnumerable<Manche> manches, int currentMancheId)
    {
        var orderedManches = Order(manches);
        var currentIndex = orderedManches.FindIndex(item => item.Id == currentMancheId);

        return currentIndex >= 0 && currentIndex < orderedManches.Count - 1
            ? orderedManches[currentIndex + 1]
            : null;
    }
}
