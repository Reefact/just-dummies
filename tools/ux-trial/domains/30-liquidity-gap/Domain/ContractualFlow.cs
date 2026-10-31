namespace LiquidityGap.Domain;

/// <summary>A single contractual cash flow: a known amount, in one currency, on one value date.</summary>
public sealed class ContractualFlow
{
    public ContractualFlow(string flowId, DateOnly valueDate, decimal amount, Currency currency, FlowDirection direction)
    {
        if (string.IsNullOrWhiteSpace(flowId))
        {
            throw new ArgumentException("A flow needs an identifier.", nameof(flowId));
        }

        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "A flow amount is strictly positive; the direction carries the sign.");
        }

        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown flow direction.");
        }

        FlowId = flowId;
        ValueDate = valueDate;
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Direction = direction;
    }

    public string FlowId { get; }

    public DateOnly ValueDate { get; }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public FlowDirection Direction { get; }

    /// <summary>The amount signed from the bank's liquidity point of view: positive in, negative out.</summary>
    public decimal SignedAmount => Direction == FlowDirection.Inflow ? Amount : -Amount;
}
