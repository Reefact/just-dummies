namespace FxSettlement.Domain;

/// <summary>
/// The bank's net position with one counterparty in one currency, after netting every trade of the
/// batch: positive when the bank receives, negative when it pays.
/// </summary>
public sealed class NetPosition : IEquatable<NetPosition>
{
    public NetPosition(Lei counterparty, Money net)
    {
        ArgumentNullException.ThrowIfNull(counterparty);
        ArgumentNullException.ThrowIfNull(net);
        Counterparty = counterparty;
        Net = net;
    }

    public Lei Counterparty { get; }

    public Money Net { get; }

    public Currency Currency => Net.Currency;

    public bool IsFlat => Net.IsZero;

    public bool Equals(NetPosition? other) => other is not null && other.Counterparty.Equals(Counterparty) && other.Net.Equals(Net);

    public override bool Equals(object? obj) => Equals(obj as NetPosition);

    public override int GetHashCode() => HashCode.Combine(Counterparty, Net);

    public override string ToString() => $"{Counterparty}: {Net}";
}
