namespace TokenLedger.Domain;

/// <summary>
/// One movement of base units, as the ERC-20 <c>Transfer</c> event logs it: a mint comes from
/// the zero address, a burn goes to it, and a fee is a movement to the fee collector.
/// </summary>
public sealed class TransferRecord : IEquatable<TransferRecord>
{
    public TransferRecord(Address from, Address to, TokenAmount amount, ulong blockNumber)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(amount);
        if (from.IsZero && to.IsZero)
        {
            throw new ArgumentException("A movement cannot go from the zero address to the zero address.", nameof(to));
        }

        From = from;
        To = to;
        Amount = amount;
        BlockNumber = blockNumber;
    }

    public Address From { get; }

    public Address To { get; }

    public TokenAmount Amount { get; }

    public ulong BlockNumber { get; }

    public bool IsMint => From.IsZero;

    public bool IsBurn => To.IsZero;

    public bool Equals(TransferRecord? other) =>
        other is not null
        && From.Equals(other.From)
        && To.Equals(other.To)
        && Amount.Equals(other.Amount)
        && BlockNumber == other.BlockNumber;

    public override bool Equals(object? obj) => Equals(obj as TransferRecord);

    public override int GetHashCode() => HashCode.Combine(From, To, Amount, BlockNumber);

    public override string ToString() => $"{From} -> {To}: {Amount} (block {BlockNumber})";
}
