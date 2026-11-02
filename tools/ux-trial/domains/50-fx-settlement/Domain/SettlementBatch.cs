namespace FxSettlement.Domain;

/// <summary>
/// The desk's settlement batch for one value date: the aggregate root. Trades settling that day are
/// booked against the reference rate sheet, netted per counterparty and currency, and once the batch
/// is cut off it produces one settlement instruction per non-flat net position. Netting and every
/// valuation of the batch round midpoints to even (banker's rounding).
/// </summary>
public sealed class SettlementBatch
{
    /// <summary>Netting and the desk's own valuations round midpoints to even.</summary>
    public const MidpointRounding NettingRounding = MidpointRounding.ToEven;

    public const decimal MaxOffMarketTolerance = 0.05m;

    private readonly Dictionary<TradeId, SpotTrade> trades = new();
    private readonly Dictionary<Currency, decimal> bankFlows = new();
    private readonly Dictionary<(Lei Counterparty, Currency Currency), decimal> counterpartyFlows = new();

    public SettlementBatch(DateOnly valueDate, RateSheet referenceRates, decimal offMarketTolerance)
    {
        ArgumentNullException.ThrowIfNull(referenceRates);
        if (CurrencyPair.IsWeekend(valueDate))
        {
            throw new ArgumentException($"Nothing settles on a {valueDate.DayOfWeek}.", nameof(valueDate));
        }

        if (referenceRates.AsOf > valueDate)
        {
            throw new ArgumentException(
                $"The reference rates of {referenceRates.AsOf:yyyy-MM-dd} postdate the value date {valueDate:yyyy-MM-dd}.",
                nameof(referenceRates));
        }

        if (offMarketTolerance <= 0m || offMarketTolerance > MaxOffMarketTolerance)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offMarketTolerance),
                offMarketTolerance,
                $"The off-market tolerance is a fraction in (0, {MaxOffMarketTolerance}].");
        }

        ValueDate = valueDate;
        ReferenceRates = referenceRates;
        OffMarketTolerance = offMarketTolerance;
    }

    public DateOnly ValueDate { get; }

    public RateSheet ReferenceRates { get; }

    /// <summary>The largest relative gap allowed between a dealt rate and the reference rate.</summary>
    public decimal OffMarketTolerance { get; }

    public BatchStatus Status { get; private set; } = BatchStatus.Open;

    public IReadOnlyCollection<SpotTrade> Trades => trades.Values;

    /// <summary>
    /// Books a trade into the batch. Refused, in this order, when the batch is cut off, the trade
    /// settles on another day, its id is already booked, the sheet cannot price its pair, or its rate
    /// strays from the reference rate by more than the tolerance.
    /// </summary>
    public void Book(SpotTrade trade)
    {
        ArgumentNullException.ThrowIfNull(trade);
        ThrowIfCutOff();
        if (trade.ValueDate != ValueDate)
        {
            throw new SettlementRefusedException(
                SettlementRefusalReason.ValueDateMismatch,
                $"{trade.Id} settles on {trade.ValueDate:yyyy-MM-dd}, not on {ValueDate:yyyy-MM-dd}.");
        }

        if (trades.ContainsKey(trade.Id))
        {
            throw new SettlementRefusedException(SettlementRefusalReason.DuplicateTrade, $"{trade.Id} is already booked.");
        }

        if (!ReferenceRates.Covers(trade.Pair))
        {
            throw new SettlementRefusedException(
                SettlementRefusalReason.NoReferenceRate,
                $"The {ReferenceRates.AsOf:yyyy-MM-dd} sheet cannot price {trade.Pair}.");
        }

        ExchangeRate reference = ReferenceRates.RateFor(trade.Pair, NettingRounding);
        decimal deviation = Math.Abs(trade.Rate.Value - reference.Value) / reference.Value;
        if (deviation > OffMarketTolerance)
        {
            throw new SettlementRefusedException(
                SettlementRefusalReason.RateOffMarket,
                $"{trade.Id} was dealt at {trade.Rate}, {deviation:P3} away from the reference {reference}.");
        }

        Post(trade, 1m);
        trades.Add(trade.Id, trade);
    }

    /// <summary>Takes a trade out of the batch before cut-off and reverses its effect on the net positions.</summary>
    public void Cancel(TradeId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        ThrowIfCutOff();
        if (!trades.Remove(id, out SpotTrade? trade))
        {
            throw new SettlementRefusedException(SettlementRefusalReason.UnknownTrade, $"{id} is not in the batch.");
        }

        Post(trade, -1m);
    }

    /// <summary>Closes the batch to bookings and cancellations; the net positions are final.</summary>
    public void CutOff()
    {
        if (Status == BatchStatus.CutOff)
        {
            throw new SettlementRefusedException(SettlementRefusalReason.AlreadyCutOff, $"The {ValueDate:yyyy-MM-dd} batch is already cut off.");
        }

        Status = BatchStatus.CutOff;
    }

    /// <summary>The bank's non-flat net positions, by counterparty then currency code.</summary>
    public IReadOnlyList<NetPosition> NetPositions() =>
        counterpartyFlows
            .Where(entry => entry.Value != 0m)
            .Select(entry => new NetPosition(entry.Key.Counterparty, new Money(-entry.Value, entry.Key.Currency)))
            .OrderBy(position => position.Counterparty.Value, StringComparer.Ordinal)
            .ThenBy(position => position.Currency.Code, StringComparer.Ordinal)
            .ToList();

    /// <summary>The bank's net in one currency across every counterparty (multilateral net).</summary>
    public Money BankNetPosition(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return new Money(bankFlows.GetValueOrDefault(currency), currency);
    }

    /// <summary>The batch's net value for the bank, every currency converted to <paramref name="reporting"/> with banker's rounding.</summary>
    public Money NetValueIn(Currency reporting)
    {
        ArgumentNullException.ThrowIfNull(reporting);
        return ValueIn(bankFlows.Select(entry => new Money(entry.Value, entry.Key)), reporting);
    }

    /// <summary>The bank's net value with one counterparty, converted to <paramref name="reporting"/> with banker's rounding.</summary>
    public Money ExposureTo(Lei counterparty, Currency reporting)
    {
        ArgumentNullException.ThrowIfNull(counterparty);
        ArgumentNullException.ThrowIfNull(reporting);
        return ValueIn(
            counterpartyFlows
                .Where(entry => entry.Key.Counterparty.Equals(counterparty))
                .Select(entry => new Money(-entry.Value, entry.Key.Currency)),
            reporting);
    }

    /// <summary>One instruction per non-flat net position: receive what is owed to the bank, pay what it owes.</summary>
    public IReadOnlyList<SettlementInstruction> Instructions()
    {
        if (Status != BatchStatus.CutOff)
        {
            throw new SettlementRefusedException(
                SettlementRefusalReason.NotCutOff,
                $"The {ValueDate:yyyy-MM-dd} batch is still open; instructions follow the cut-off.");
        }

        return NetPositions()
            .Select(position => new SettlementInstruction(
                position.Counterparty,
                position.Net.IsPositive ? SettlementDirection.Receive : SettlementDirection.Pay,
                position.Net.Abs(),
                ValueDate))
            .ToList();
    }

    private void ThrowIfCutOff()
    {
        if (Status != BatchStatus.Open)
        {
            throw new SettlementRefusedException(SettlementRefusalReason.BatchCutOff, $"The {ValueDate:yyyy-MM-dd} batch is cut off.");
        }
    }

    /// <summary>
    /// Posts both sides of both legs. Each currency of a trade must balance to zero across the bank
    /// and its counterparty before anything is accumulated.
    /// </summary>
    private void Post(SpotTrade trade, decimal sign)
    {
        (Lei? Party, Money Flow)[] postings =
        [
            (null, trade.BankReceives),
            (trade.Counterparty, trade.BankReceives.Negated()),
            (null, trade.BankPays.Negated()),
            (trade.Counterparty, trade.BankPays),
        ];

        Dictionary<Currency, decimal> balance = new();
        foreach ((Lei? _, Money flow) in postings)
        {
            balance[flow.Currency] = balance.GetValueOrDefault(flow.Currency) + flow.Amount;
        }

        foreach (KeyValuePair<Currency, decimal> entry in balance)
        {
            if (entry.Value != 0m)
            {
                throw new InvalidOperationException($"{trade.Id} does not balance in {entry.Key.Code}: {entry.Value}.");
            }
        }

        foreach ((Lei? party, Money flow) in postings)
        {
            decimal amount = sign * flow.Amount;
            if (party is null)
            {
                bankFlows[flow.Currency] = bankFlows.GetValueOrDefault(flow.Currency) + amount;
            }
            else
            {
                (Lei, Currency) key = (party, flow.Currency);
                counterpartyFlows[key] = counterpartyFlows.GetValueOrDefault(key) + amount;
            }
        }
    }

    private Money ValueIn(IEnumerable<Money> amounts, Currency reporting)
    {
        if (!ReferenceRates.Covers(reporting))
        {
            throw new SettlementRefusedException(
                SettlementRefusalReason.NoReferenceRate,
                $"The {ReferenceRates.AsOf:yyyy-MM-dd} sheet cannot value anything in {reporting.Code}.");
        }

        Money total = Money.Zero(reporting);
        foreach (Money amount in amounts)
        {
            if (amount.IsZero)
            {
                continue;
            }

            Money converted = amount.Currency.Equals(reporting)
                ? amount
                : ReferenceRates.RateFor(new CurrencyPair(amount.Currency, reporting), NettingRounding).Convert(amount, NettingRounding);
            total = total.Plus(converted);
        }

        return total;
    }
}
