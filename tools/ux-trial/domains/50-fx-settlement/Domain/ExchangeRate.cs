namespace FxSettlement.Domain;

/// <summary>
/// A rate for a pair, quoted with no more decimal places than the pair allows. Inverting it or
/// converting with it rounds explicitly, with the midpoint mode the caller's operation requires.
/// </summary>
public sealed class ExchangeRate : IEquatable<ExchangeRate>
{
    public const decimal MaxValue = 1_000_000m;

    public ExchangeRate(CurrencyPair pair, decimal value)
    {
        ArgumentNullException.ThrowIfNull(pair);
        if (value <= 0m || value > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"A rate lies in (0, {MaxValue}].");
        }

        if (decimal.Round(value, pair.RateDecimalPlaces) != value)
        {
            throw new ArgumentException(
                $"{pair} is quoted to {pair.RateDecimalPlaces} decimal places; {value} has more.",
                nameof(value));
        }

        Pair = pair;
        Value = value;
    }

    public CurrencyPair Pair { get; }

    public decimal Value { get; }

    /// <summary>The rate of the inverse pair, 1 / value rounded to that pair's decimal places.</summary>
    public ExchangeRate Invert(MidpointRounding mode)
    {
        CurrencyPair inverse = Pair.Inverse();
        return FromUnrounded(inverse, 1m / Value, mode);
    }

    /// <summary>
    /// Converts an amount in either currency of the pair into the other one, rounded to the
    /// target currency's minor units with <paramref name="mode"/>.
    /// </summary>
    public Money Convert(Money amount, MidpointRounding mode)
    {
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Currency.Equals(Pair.Base))
        {
            return new Money(Pair.Quote.Round(amount.Amount * Value, mode), Pair.Quote);
        }

        if (amount.Currency.Equals(Pair.Quote))
        {
            return new Money(Pair.Base.Round(amount.Amount / Value, mode), Pair.Base);
        }

        throw new ArgumentException($"{amount.Currency.Code} is not a currency of {Pair}.", nameof(amount));
    }

    /// <summary>Builds a rate from a computed value, rounding it to the pair's decimal places; refused when that leaves it out of range.</summary>
    internal static ExchangeRate FromUnrounded(CurrencyPair pair, decimal unrounded, MidpointRounding mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown midpoint rounding mode.");
        }

        decimal rounded = decimal.Round(unrounded, pair.RateDecimalPlaces, mode);
        if (rounded <= 0m || rounded > MaxValue)
        {
            throw new InvalidOperationException(
                $"{pair} would be {unrounded}, which is not quotable to {pair.RateDecimalPlaces} decimal places within (0, {MaxValue}].");
        }

        return new ExchangeRate(pair, rounded);
    }

    public bool Equals(ExchangeRate? other) => other is not null && other.Pair.Equals(Pair) && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as ExchangeRate);

    public override int GetHashCode() => HashCode.Combine(Pair, Value);

    public override string ToString() =>
        $"{Pair} {Value.ToString("F" + Pair.RateDecimalPlaces, System.Globalization.CultureInfo.InvariantCulture)}";
}
