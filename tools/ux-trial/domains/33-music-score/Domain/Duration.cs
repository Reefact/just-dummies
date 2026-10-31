namespace MusicScore.Domain;

/// <summary>An exact, non-negative rational length of musical time, in whole notes (1/4 is a quarter note).</summary>
public sealed class Duration : IEquatable<Duration>, IComparable<Duration>
{
    public const long MaxDenominator = 1024;

    public Duration(long numerator, long denominator)
    {
        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), denominator, "The denominator must be positive.");
        }

        if (numerator < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numerator), numerator, "A duration cannot be negative.");
        }

        long divisor = GreatestCommonDivisor(numerator, denominator);
        Numerator = numerator / divisor;
        Denominator = denominator / divisor;

        if (Denominator > MaxDenominator)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), denominator, $"A duration finer than 1/{MaxDenominator} of a whole note is not notatable.");
        }
    }

    public static Duration Zero { get; } = new(0, 1);

    public long Numerator { get; }

    public long Denominator { get; }

    public bool IsZero => Numerator == 0;

    public double InWholeNotes => (double)Numerator / Denominator;

    public static Duration operator +(Duration left, Duration right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new Duration(checked((left.Numerator * right.Denominator) + (right.Numerator * left.Denominator)), checked(left.Denominator * right.Denominator));
    }

    public static Duration operator *(Duration duration, int factor)
    {
        ArgumentNullException.ThrowIfNull(duration);
        ArgumentOutOfRangeException.ThrowIfNegative(factor);
        return new Duration(checked(duration.Numerator * factor), duration.Denominator);
    }

    public static bool operator ==(Duration? left, Duration? right) => Equals(left, right);

    public static bool operator !=(Duration? left, Duration? right) => !Equals(left, right);

    public static bool operator <(Duration left, Duration right) => left.CompareTo(right) < 0;

    public static bool operator >(Duration left, Duration right) => left.CompareTo(right) > 0;

    public static bool operator <=(Duration left, Duration right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Duration left, Duration right) => left.CompareTo(right) >= 0;

    /// <summary>How many times <paramref name="unit"/> fits in this duration, as an exact ratio turned into a double.</summary>
    public double DividedBy(Duration unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (unit.IsZero)
        {
            throw new ArgumentException("Cannot divide by a zero duration.", nameof(unit));
        }

        return (double)(Numerator * unit.Denominator) / (Denominator * unit.Numerator);
    }

    public int CompareTo(Duration? other)
    {
        if (other is null)
        {
            return 1;
        }

        return checked(Numerator * other.Denominator).CompareTo(checked(other.Numerator * Denominator));
    }

    public bool Equals(Duration? other) => other is not null && Numerator == other.Numerator && Denominator == other.Denominator;

    public override bool Equals(object? obj) => Equals(obj as Duration);

    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    public override string ToString() => $"{Numerator}/{Denominator}";

    private static long GreatestCommonDivisor(long a, long b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a == 0 ? 1 : a;
    }
}
