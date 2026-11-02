using Freight.Domain;

namespace ColdChain.Domain;

/// <summary>Who answers for the consignment on one leg of its itinerary, from acceptance until hand-over.</summary>
public sealed class LegCustody
{
    public LegCustody(int legIndex, Leg leg, CustodianId custodian, DateTimeOffset acceptedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(legIndex);
        ArgumentNullException.ThrowIfNull(leg);
        ArgumentNullException.ThrowIfNull(custodian);
        LegIndex = legIndex;
        Leg = leg;
        Custodian = custodian;
        AcceptedAt = acceptedAt;
    }

    public int LegIndex { get; }

    public Leg Leg { get; }

    public CustodianId Custodian { get; }

    public DateTimeOffset AcceptedAt { get; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public bool IsOpen => ReleasedAt is null;

    /// <summary>Whether the custodian held the consignment at <paramref name="instant"/>: from acceptance, up to (excluding) release.</summary>
    public bool Covers(DateTimeOffset instant) => instant >= AcceptedAt && (ReleasedAt is null || instant < ReleasedAt);

    internal void Release(DateTimeOffset at)
    {
        if (ReleasedAt is not null)
        {
            throw new InvalidOperationException($"Custody of leg {LegIndex} was already released at {ReleasedAt:u}.");
        }

        if (at < AcceptedAt)
        {
            throw new ArgumentException("Custody cannot be released before it was accepted.", nameof(at));
        }

        ReleasedAt = at;
    }

    public override string ToString() => $"leg {LegIndex} {Leg.Origin}->{Leg.Destination}: {Custodian}";
}
