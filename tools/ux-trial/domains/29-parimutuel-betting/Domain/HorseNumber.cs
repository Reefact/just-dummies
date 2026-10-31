namespace ParimutuelBetting.Domain;

/// <summary>The saddle-cloth number of a runner, from 1 to 24.</summary>
public sealed class HorseNumber : IEquatable<HorseNumber>, IComparable<HorseNumber>
{
    /// <summary>The largest saddle-cloth number a race card can carry.</summary>
    public const int Maximum = 24;

    public HorseNumber(int value)
    {
        if (value < 1 || value > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"A horse number lies between 1 and {Maximum}.");
        }

        Value = value;
    }

    public int Value { get; }

    public bool Equals(HorseNumber? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => obj is HorseNumber other && Equals(other);

    public override int GetHashCode() => Value;

    public int CompareTo(HorseNumber? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
