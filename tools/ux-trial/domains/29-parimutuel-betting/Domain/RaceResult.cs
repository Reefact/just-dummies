namespace ParimutuelBetting.Domain;

/// <summary>The official result of a race: the finishing order and the horses scratched before the off.</summary>
public sealed class RaceResult
{
    public RaceResult(Race race, IReadOnlyList<HorseNumber> finishingOrder, IReadOnlyCollection<HorseNumber> scratched)
    {
        ArgumentNullException.ThrowIfNull(race);
        ArgumentNullException.ThrowIfNull(finishingOrder);
        ArgumentNullException.ThrowIfNull(scratched);
        if (finishingOrder.Count == 0)
        {
            throw new ArgumentException("An official result names at least the winner.", nameof(finishingOrder));
        }

        if (finishingOrder.Any(horse => horse is null) || scratched.Any(horse => horse is null))
        {
            throw new ArgumentException("A result cannot name a null horse.");
        }

        if (finishingOrder.Distinct().Count() != finishingOrder.Count)
        {
            throw new ArgumentException("A horse cannot finish twice.", nameof(finishingOrder));
        }

        if (scratched.Distinct().Count() != scratched.Count)
        {
            throw new ArgumentException("A horse cannot be scratched twice.", nameof(scratched));
        }

        if (finishingOrder.Concat(scratched).Any(horse => !race.IsRunner(horse)))
        {
            throw new ArgumentException($"Every horse in the result must be a declared runner of {race.Code} (1 to {race.FieldSize}).");
        }

        if (finishingOrder.Any(scratched.Contains))
        {
            throw new ArgumentException("A scratched horse cannot appear in the finishing order.", nameof(finishingOrder));
        }

        Race = race;
        FinishingOrder = finishingOrder.ToArray();
        Scratched = scratched.ToArray();
    }

    public Race Race { get; }

    public IReadOnlyList<HorseNumber> FinishingOrder { get; }

    public IReadOnlyCollection<HorseNumber> Scratched { get; }

    /// <summary>The winning combination of a bet type other than Place.</summary>
    public Combination WinningCombination(BetType betType)
    {
        if (betType == BetType.Place)
        {
            throw new ArgumentException("A Place bet has one winning horse per paid place, not one combination.", nameof(betType));
        }

        int arity = betType.Arity();
        if (FinishingOrder.Count < arity)
        {
            throw new InvalidOperationException($"The result of {Race.Code} names {FinishingOrder.Count} finisher(s); a {betType} needs {arity}.");
        }

        return new Combination(betType, FinishingOrder.Take(arity).ToArray());
    }
}
