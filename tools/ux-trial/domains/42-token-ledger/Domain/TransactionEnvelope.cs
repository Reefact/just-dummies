namespace TokenLedger.Domain;

/// <summary>
/// What every signed transaction carries besides its operation: the sender, the sender's
/// nonce, the most gas it may use, the price it pays per unit of gas (in base units of the
/// ledger's token), and the last block in which it may be included.
/// </summary>
public sealed class TransactionEnvelope : IEquatable<TransactionEnvelope>
{
    public TransactionEnvelope(Address sender, UInt128 nonce, ulong gasLimit, TokenAmount gasPrice, ulong deadlineBlock)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(gasPrice);
        if (sender.IsZero)
        {
            throw new ArgumentException("Nobody signs for the zero address.", nameof(sender));
        }

        if (gasLimit < GasSchedule.IntrinsicGas || gasLimit > GasSchedule.BlockGasLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasLimit),
                gasLimit,
                $"A gas limit lies in [{GasSchedule.IntrinsicGas}, {GasSchedule.BlockGasLimit}].");
        }

        if (gasPrice.BaseUnits * gasLimit > TokenAmount.MaxBaseUnits)
        {
            throw new ArgumentException($"The maximum fee (gas limit {gasLimit} x gas price {gasPrice}) exceeds 2^256 - 1 base units.", nameof(gasPrice));
        }

        Sender = sender;
        Nonce = nonce;
        GasLimit = gasLimit;
        GasPrice = gasPrice;
        DeadlineBlock = deadlineBlock;
    }

    public Address Sender { get; }

    public UInt128 Nonce { get; }

    public ulong GasLimit { get; }

    /// <summary>Base units paid per unit of gas used.</summary>
    public TokenAmount GasPrice { get; }

    /// <summary>The last block number at which the transaction is still accepted (inclusive).</summary>
    public ulong DeadlineBlock { get; }

    /// <summary>The fee if the whole gas limit were used: what the sender must be able to pay up front.</summary>
    public TokenAmount MaxFee => GasPrice.MultiplyBy(GasLimit);

    public bool Equals(TransactionEnvelope? other) =>
        other is not null
        && Sender.Equals(other.Sender)
        && Nonce == other.Nonce
        && GasLimit == other.GasLimit
        && GasPrice.Equals(other.GasPrice)
        && DeadlineBlock == other.DeadlineBlock;

    public override bool Equals(object? obj) => Equals(obj as TransactionEnvelope);

    public override int GetHashCode() => HashCode.Combine(Sender, Nonce, GasLimit, GasPrice, DeadlineBlock);

    public override string ToString() => $"{Sender} #{Nonce} (gas {GasLimit} @ {GasPrice}, until block {DeadlineBlock})";
}
