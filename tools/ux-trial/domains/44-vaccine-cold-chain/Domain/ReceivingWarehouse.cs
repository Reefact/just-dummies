using Freight.Domain;
using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>The GDP-licensed warehouse taking delivery: where it is, who runs it, its cold room and its quarantine bay.</summary>
public sealed class ReceivingWarehouse : IEquatable<ReceivingWarehouse>
{
    public ReceivingWarehouse(UnLocode location, CustodianId operatorId, TemperatureRange coldRoom, BinLocation quarantineBay)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(operatorId);
        ArgumentNullException.ThrowIfNull(coldRoom);
        ArgumentNullException.ThrowIfNull(quarantineBay);
        if (coldRoom.Zone != StorageZone.Chilled)
        {
            throw new ArgumentException($"A vaccine warehouse receives into a chilled room, not a {coldRoom.Zone} one.", nameof(coldRoom));
        }

        if (coldRoom.MinCelsius < VaccineProduct.FreezeLimitCelsius)
        {
            throw new ArgumentException("A vaccine cold room never runs below 0 °C.", nameof(coldRoom));
        }

        if (quarantineBay.NeedsReachTruck)
        {
            throw new ArgumentException("Quarantined pallets are kept at floor levels (1 to 4) for inspection.", nameof(quarantineBay));
        }

        Location = location;
        OperatorId = operatorId;
        ColdRoom = coldRoom;
        QuarantineBay = quarantineBay;
    }

    public UnLocode Location { get; }

    public CustodianId OperatorId { get; }

    public TemperatureRange ColdRoom { get; }

    public BinLocation QuarantineBay { get; }

    public bool Equals(ReceivingWarehouse? other) =>
        other is not null
        && other.Location.Equals(Location)
        && other.OperatorId.Equals(OperatorId)
        && other.ColdRoom.MinCelsius == ColdRoom.MinCelsius
        && other.ColdRoom.MaxCelsius == ColdRoom.MaxCelsius
        && other.QuarantineBay.Equals(QuarantineBay);

    public override bool Equals(object? obj) => Equals(obj as ReceivingWarehouse);

    public override int GetHashCode() => HashCode.Combine(Location, OperatorId, ColdRoom.MinCelsius, ColdRoom.MaxCelsius, QuarantineBay);

    public override string ToString() => $"{OperatorId} at {Location}";
}
