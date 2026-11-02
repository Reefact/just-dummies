namespace FxSettlement.Domain;

/// <summary>One payment to make or to expect on value date: a positive amount, a direction, a counterparty.</summary>
public sealed class SettlementInstruction : IEquatable<SettlementInstruction>
{
    public SettlementInstruction(Lei counterparty, SettlementDirection direction, Money amount, DateOnly valueDate)
    {
        ArgumentNullException.ThrowIfNull(counterparty);
        ArgumentNullException.ThrowIfNull(amount);
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown settlement direction.");
        }

        if (!amount.IsPositive)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "An instruction moves a positive amount; the direction carries the sign.");
        }

        if (CurrencyPair.IsWeekend(valueDate))
        {
            throw new ArgumentException($"Nothing settles on a {valueDate.DayOfWeek}.", nameof(valueDate));
        }

        Counterparty = counterparty;
        Direction = direction;
        Amount = amount;
        ValueDate = valueDate;
    }

    public Lei Counterparty { get; }

    public SettlementDirection Direction { get; }

    public Money Amount { get; }

    public DateOnly ValueDate { get; }

    public bool Equals(SettlementInstruction? other) =>
        other is not null
        && other.Counterparty.Equals(Counterparty)
        && other.Direction == Direction
        && other.Amount.Equals(Amount)
        && other.ValueDate == ValueDate;

    public override bool Equals(object? obj) => Equals(obj as SettlementInstruction);

    public override int GetHashCode() => HashCode.Combine(Counterparty, Direction, Amount, ValueDate);

    public override string ToString() => $"{ValueDate:yyyy-MM-dd} {Direction} {Amount} {(Direction == SettlementDirection.Pay ? "to" : "from")} {Counterparty}";
}
