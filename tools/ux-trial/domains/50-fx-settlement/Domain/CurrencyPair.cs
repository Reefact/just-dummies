namespace FxSettlement.Domain;

/// <summary>
/// A quoted pair BASE/QUOTE: one unit of the base costs <c>rate</c> units of the quote. The pair fixes
/// how many decimal places its rate carries and how many business days spot settles after the trade.
/// </summary>
public sealed class CurrencyPair : IEquatable<CurrencyPair>
{
    /// <summary>Decimal places of a rate whose quote currency has minor units (EUR/USD 1.08425).</summary>
    public const int StandardRateDecimalPlaces = 5;

    /// <summary>Decimal places of a rate quoted in a currency without minor units (USD/JPY 151.237).</summary>
    public const int WholeUnitQuoteRateDecimalPlaces = 3;

    public CurrencyPair(Currency @base, Currency quote)
    {
        ArgumentNullException.ThrowIfNull(@base);
        ArgumentNullException.ThrowIfNull(quote);
        if (@base.Code == quote.Code)
        {
            throw new ArgumentException($"A pair needs two different currencies; got {@base.Code} twice.", nameof(quote));
        }

        Base = @base;
        Quote = quote;
    }

    public Currency Base { get; }

    public Currency Quote { get; }

    /// <summary>3 when the quote currency has no minor units (JPY, KRW pairs), 5 otherwise.</summary>
    public int RateDecimalPlaces => Quote.MinorUnits == 0 ? WholeUnitQuoteRateDecimalPlaces : StandardRateDecimalPlaces;

    /// <summary>Business days from trade date to spot value date: 1 for USD/CAD (either way round), 2 otherwise.</summary>
    public int SpotLagDays => IsUsdCad ? 1 : 2;

    private bool IsUsdCad =>
        (Base.Code == "USD" && Quote.Code == "CAD") || (Base.Code == "CAD" && Quote.Code == "USD");

    public bool Involves(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return Base.Equals(currency) || Quote.Equals(currency);
    }

    public CurrencyPair Inverse() => new(Quote, Base);

    /// <summary>The spot value date: <see cref="SpotLagDays"/> business days after the trade date, skipping weekends.</summary>
    public DateOnly SpotValueDate(DateOnly tradeDate)
    {
        if (IsWeekend(tradeDate))
        {
            throw new ArgumentException($"{tradeDate:yyyy-MM-dd} is a {tradeDate.DayOfWeek}; spot trades are dealt on business days.", nameof(tradeDate));
        }

        DateOnly date = tradeDate;
        int remaining = SpotLagDays;
        while (remaining > 0)
        {
            date = date.AddDays(1);
            if (!IsWeekend(date))
            {
                remaining--;
            }
        }

        return date;
    }

    public static bool IsWeekend(DateOnly date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public bool Equals(CurrencyPair? other) => other is not null && other.Base.Equals(Base) && other.Quote.Equals(Quote);

    public override bool Equals(object? obj) => Equals(obj as CurrencyPair);

    public override int GetHashCode() => HashCode.Combine(Base, Quote);

    public override string ToString() => $"{Base.Code}/{Quote.Code}";
}
