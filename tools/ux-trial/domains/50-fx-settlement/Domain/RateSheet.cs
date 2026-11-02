namespace FxSettlement.Domain;

/// <summary>
/// The day's reference rates, each quoted against one pivot currency (usually USD). Any other pair is
/// a cross rate, triangulated through the pivot and rounded once, at the end, with the caller's mode.
/// </summary>
public sealed class RateSheet
{
    private readonly Dictionary<Currency, ExchangeRate> byCurrency = new();

    public RateSheet(Currency pivot, DateOnly asOf, IEnumerable<ExchangeRate> rates)
    {
        ArgumentNullException.ThrowIfNull(pivot);
        ArgumentNullException.ThrowIfNull(rates);
        if (CurrencyPair.IsWeekend(asOf))
        {
            throw new ArgumentException($"Reference rates are fixed on business days; {asOf:yyyy-MM-dd} is a {asOf.DayOfWeek}.", nameof(asOf));
        }

        foreach (ExchangeRate rate in rates)
        {
            if (rate is null)
            {
                throw new ArgumentException("A rate sheet holds no null rate.", nameof(rates));
            }

            if (!rate.Pair.Involves(pivot))
            {
                throw new ArgumentException($"{rate.Pair} is not quoted against the pivot {pivot.Code}.", nameof(rates));
            }

            Currency other = rate.Pair.Base.Equals(pivot) ? rate.Pair.Quote : rate.Pair.Base;
            if (!byCurrency.TryAdd(other, rate))
            {
                throw new ArgumentException($"{other.Code} is quoted twice against {pivot.Code}.", nameof(rates));
            }
        }

        if (byCurrency.Count == 0)
        {
            throw new ArgumentException("A rate sheet quotes at least one currency.", nameof(rates));
        }

        Pivot = pivot;
        AsOf = asOf;
    }

    public Currency Pivot { get; }

    public DateOnly AsOf { get; }

    public IReadOnlyCollection<ExchangeRate> Rates => byCurrency.Values;

    public bool Covers(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return currency.Equals(Pivot) || byCurrency.ContainsKey(currency);
    }

    public bool Covers(CurrencyPair pair)
    {
        ArgumentNullException.ThrowIfNull(pair);
        return Covers(pair.Base) && Covers(pair.Quote);
    }

    /// <summary>
    /// The rate for any pair of covered currencies: the quoted rate as is, its inverse, or the cross
    /// rate through the pivot, each rounded to the pair's decimal places with <paramref name="mode"/>.
    /// </summary>
    public ExchangeRate RateFor(CurrencyPair pair, MidpointRounding mode)
    {
        ArgumentNullException.ThrowIfNull(pair);
        if (!Covers(pair))
        {
            throw new InvalidOperationException($"The {AsOf:yyyy-MM-dd} sheet has no rate for {pair}.");
        }

        Currency nonPivot = pair.Base.Equals(Pivot) ? pair.Quote : pair.Base;
        if (pair.Involves(Pivot))
        {
            ExchangeRate quoted = byCurrency[nonPivot];
            return quoted.Pair.Equals(pair) ? quoted : quoted.Invert(mode);
        }

        decimal cross = PivotValueOf(pair.Base) / PivotValueOf(pair.Quote);
        return ExchangeRate.FromUnrounded(pair, cross, mode);
    }

    /// <summary>How many pivot units one unit of the currency is worth, unrounded.</summary>
    private decimal PivotValueOf(Currency currency)
    {
        ExchangeRate rate = byCurrency[currency];
        return rate.Pair.Base.Equals(currency) ? rate.Value : 1m / rate.Value;
    }
}
