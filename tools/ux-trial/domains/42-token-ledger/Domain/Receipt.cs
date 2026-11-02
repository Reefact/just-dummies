namespace TokenLedger.Domain;

/// <summary>The record of an accepted transaction: who sent it, in which block, the gas it used and the fee it paid.</summary>
public sealed class Receipt : IEquatable<Receipt>
{
    public Receipt(TransactionKind kind, Address sender, UInt128 nonce, ulong blockNumber, ulong gasUsed, TokenAmount gasPrice)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(gasPrice);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown transaction kind.");
        }

        if (sender.IsZero)
        {
            throw new ArgumentException("No transaction is sent from the zero address.", nameof(sender));
        }

        if (gasUsed < GasSchedule.IntrinsicGas || gasUsed > GasSchedule.BlockGasLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(gasUsed), gasUsed, $"Gas used lies in [{GasSchedule.IntrinsicGas}, {GasSchedule.BlockGasLimit}].");
        }

        if (gasPrice.BaseUnits * gasUsed > TokenAmount.MaxBaseUnits)
        {
            throw new ArgumentException("The fee (gas used x gas price) exceeds 2^256 - 1 base units.", nameof(gasPrice));
        }

        Kind = kind;
        Sender = sender;
        Nonce = nonce;
        BlockNumber = blockNumber;
        GasUsed = gasUsed;
        GasPrice = gasPrice;
        Fee = gasPrice.MultiplyBy(gasUsed);
    }

    public TransactionKind Kind { get; }

    public Address Sender { get; }

    public UInt128 Nonce { get; }

    public ulong BlockNumber { get; }

    public ulong GasUsed { get; }

    public TokenAmount GasPrice { get; }

    /// <summary>Gas used × gas price, in base units.</summary>
    public TokenAmount Fee { get; }

    public bool Equals(Receipt? other) =>
        other is not null
        && Kind == other.Kind
        && Sender.Equals(other.Sender)
        && Nonce == other.Nonce
        && BlockNumber == other.BlockNumber
        && GasUsed == other.GasUsed
        && GasPrice.Equals(other.GasPrice);

    public override bool Equals(object? obj) => Equals(obj as Receipt);

    public override int GetHashCode() => HashCode.Combine(Kind, Sender, Nonce, BlockNumber, GasUsed, GasPrice);

    public override string ToString() => $"{Kind} by {Sender} #{Nonce} in block {BlockNumber}: {GasUsed} gas, fee {Fee}";
}
