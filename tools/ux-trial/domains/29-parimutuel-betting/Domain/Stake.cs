namespace ParimutuelBetting.Domain;

/// <summary>A number of unit stakes placed on one combination.</summary>
public sealed class Stake
{
    /// <summary>The largest number of units a single ticket may carry.</summary>
    public const int MaximumUnits = 10_000;

    public Stake(Combination combination, int units)
    {
        ArgumentNullException.ThrowIfNull(combination);
        if (units < 1 || units > MaximumUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(units), units, $"A stake carries between 1 and {MaximumUnits} units.");
        }

        Combination = combination;
        Units = units;
    }

    public Combination Combination { get; }

    public int Units { get; }
}
