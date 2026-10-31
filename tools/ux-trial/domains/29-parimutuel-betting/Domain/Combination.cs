namespace ParimutuelBetting.Domain;

/// <summary>
/// The horses a bet names, for a given bet type. An unordered combination is normalised to
/// ascending horse numbers, so that two equivalent selections are equal.
/// </summary>
public sealed class Combination : IEquatable<Combination>
{
    public Combination(BetType betType, IReadOnlyList<HorseNumber> horses)
    {
        ArgumentNullException.ThrowIfNull(horses);
        if (!Enum.IsDefined(betType))
        {
            throw new ArgumentOutOfRangeException(nameof(betType), betType, "Unknown bet type.");
        }

        if (horses.Any(horse => horse is null))
        {
            throw new ArgumentException("A combination cannot name a null horse.", nameof(horses));
        }

        if (horses.Count != betType.Arity())
        {
            throw new ArgumentException($"A {betType} combination names exactly {betType.Arity()} horse(s), not {horses.Count}.", nameof(horses));
        }

        if (horses.Distinct().Count() != horses.Count)
        {
            throw new ArgumentException("A combination cannot name the same horse twice.", nameof(horses));
        }

        BetType = betType;
        Horses = betType.IsOrdered() ? horses.ToArray() : horses.OrderBy(horse => horse.Value).ToArray();
    }

    public BetType BetType { get; }

    public IReadOnlyList<HorseNumber> Horses { get; }

    /// <summary>Whether this combination names at least one of the given horses.</summary>
    public bool Involves(IEnumerable<HorseNumber> horses) => horses.Any(horse => Horses.Contains(horse));

    public bool Equals(Combination? other) =>
        other is not null && other.BetType == BetType && other.Horses.SequenceEqual(Horses);

    public override bool Equals(object? obj) => obj is Combination other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(BetType);
        foreach (HorseNumber horse in Horses)
        {
            hash.Add(horse);
        }

        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"{BetType} {string.Join(BetType.IsOrdered() ? "-" : "/", Horses)}";
}
