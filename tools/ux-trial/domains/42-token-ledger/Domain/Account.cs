namespace TokenLedger.Domain;

/// <summary>
/// An account of the ledger: its balance in base units and the nonce its next transaction must
/// carry. Identified by its address, which is never the zero address.
/// </summary>
public sealed class Account
{
    public Account(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.IsZero)
        {
            throw new ArgumentException("The zero address is reserved and holds no account.", nameof(address));
        }

        Address = address;
        Balance = TokenAmount.Zero;
        NextNonce = UInt128.Zero;
    }

    public Address Address { get; }

    public TokenAmount Balance { get; private set; }

    public UInt128 NextNonce { get; private set; }

    public bool IsHolder => !Balance.IsZero;

    internal void Credit(TokenAmount amount) => Balance = Balance.Add(amount);

    internal void Debit(TokenAmount amount) => Balance = Balance.Subtract(amount);

    internal void ConsumeNonce()
    {
        if (NextNonce == UInt128.MaxValue)
        {
            throw new InvalidOperationException($"{Address} has used every nonce.");
        }

        NextNonce++;
    }

    public override string ToString() => $"{Address}: {Balance} (next nonce {NextNonce})";
}
