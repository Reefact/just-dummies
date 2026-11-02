namespace FxSettlement.Domain;

/// <summary>
/// A spot deal with a counterparty. The client deals an amount in either currency of the pair; the
/// other leg is the client conversion at the dealt rate, rounded away from zero to that currency's
/// minor units. It settles on the pair's spot value date.
/// </summary>
public sealed class SpotTrade
{
    /// <summary>Client conversions round midpoints away from zero.</summary>
    public const MidpointRounding ClientRounding = MidpointRounding.AwayFromZero;

    public const decimal MaxDealtAmount = 1_000_000_000_000m;

    public SpotTrade(TradeId id, Lei counterparty, TradeSide side, Money dealtAmount, ExchangeRate rate, DateOnly tradeDate)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(counterparty);
        ArgumentNullException.ThrowIfNull(dealtAmount);
        ArgumentNullException.ThrowIfNull(rate);
        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown trade side.");
        }

        if (!rate.Pair.Involves(dealtAmount.Currency))
        {
            throw new ArgumentException($"The dealt currency {dealtAmount.Currency.Code} is not part of {rate.Pair}.", nameof(dealtAmount));
        }

        if (!dealtAmount.IsPositive || dealtAmount.Amount > MaxDealtAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(dealtAmount), dealtAmount, $"A dealt amount lies in (0, {MaxDealtAmount}].");
        }

        bool dealtInBase = dealtAmount.Currency.Equals(rate.Pair.Base);
        Currency counterCurrency = dealtInBase ? rate.Pair.Quote : rate.Pair.Base;
        decimal counterValue = dealtInBase ? dealtAmount.Amount * rate.Value : dealtAmount.Amount / rate.Value;
        if (counterCurrency.Round(counterValue, ClientRounding) > Money.MaxMagnitude)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dealtAmount),
                dealtAmount,
                $"At {rate} the other leg would exceed {Money.MaxMagnitude} {counterCurrency.Code}.");
        }

        Money counterAmount = rate.Convert(dealtAmount, ClientRounding);
        if (counterAmount.IsZero)
        {
            throw new ArgumentException(
                $"{dealtAmount} at {rate} is worth less than one {counterAmount.Currency.Code} minor unit.",
                nameof(dealtAmount));
        }

        Id = id;
        Counterparty = counterparty;
        Side = side;
        DealtAmount = dealtAmount;
        Rate = rate;
        TradeDate = tradeDate;
        ValueDate = rate.Pair.SpotValueDate(tradeDate);
        BaseAmount = dealtInBase ? dealtAmount : counterAmount;
        QuoteAmount = dealtInBase ? counterAmount : dealtAmount;
    }

    public TradeId Id { get; }

    public Lei Counterparty { get; }

    public TradeSide Side { get; }

    public Money DealtAmount { get; }

    public ExchangeRate Rate { get; }

    public CurrencyPair Pair => Rate.Pair;

    public DateOnly TradeDate { get; }

    public DateOnly ValueDate { get; }

    public Money BaseAmount { get; }

    public Money QuoteAmount { get; }

    /// <summary>What the bank receives on value date: the base when it buys it, the quote when it sells it.</summary>
    public Money BankReceives => Side == TradeSide.BuyBase ? BaseAmount : QuoteAmount;

    /// <summary>What the bank pays on value date.</summary>
    public Money BankPays => Side == TradeSide.BuyBase ? QuoteAmount : BaseAmount;

    public override string ToString() => $"{Id} {Side} {BaseAmount} vs {QuoteAmount} @ {Rate} value {ValueDate:yyyy-MM-dd}";
}
