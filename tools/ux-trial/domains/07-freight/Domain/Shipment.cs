namespace Freight.Domain;

/// <summary>One container moved door to door over a contiguous itinerary of legs.</summary>
public sealed class Shipment {

    private readonly List<Leg> _legs;

    public Shipment(Guid id, ContainerCode container, ContainerType containerType, Weight grossWeight, Volume volume, IReadOnlyList<Leg> legs) {
        if (id == Guid.Empty) {
            throw new ArgumentException("A shipment needs an identifier.", nameof(id));
        }
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(grossWeight);
        ArgumentNullException.ThrowIfNull(volume);
        if (!Enum.IsDefined(containerType)) {
            throw new ArgumentOutOfRangeException(nameof(containerType), containerType, null);
        }
        if (grossWeight.Kilograms > containerType.MaxPayloadKilograms()) {
            throw new ArgumentException($"{grossWeight} exceeds the payload of a {containerType}.", nameof(grossWeight));
        }
        if (volume.CubicMetres > containerType.CapacityCubicMetres()) {
            throw new ArgumentException($"{volume} does not fit in a {containerType}.", nameof(volume));
        }
        ValidateItinerary(legs);

        Id            = id;
        Container     = container;
        ContainerType = containerType;
        GrossWeight   = grossWeight;
        Volume        = volume;
        _legs         = legs.ToList();
        Status        = ShipmentStatus.Booked;
    }

    public Guid Id { get; }

    public ContainerCode Container { get; }

    public ContainerType ContainerType { get; }

    public Weight GrossWeight { get; }

    public Volume Volume { get; }

    public IReadOnlyList<Leg> Legs => _legs;

    public ShipmentStatus Status { get; private set; }

    public DateTimeOffset? DepartedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public DateTimeOffset EstimatedArrival => _legs[^1].Arrival;

    public TimeSpan PlannedTransitTime => _legs[^1].Arrival - _legs[0].Departure;

    /// <summary>W/M: the greater of the weight in metric tons and the volume in cubic metres.</summary>
    public double RevenueTons => Math.Max(GrossWeight.MetricTons, Volume.CubicMetres);

    public double VolumeUtilisation => Volume.CubicMetres / ContainerType.CapacityCubicMetres();

    public DateTimeOffset EstimatedArrivalAt(TimeSpan utcOffset) {
        return EstimatedArrival.ToOffset(utcOffset);
    }

    public decimal QuoteWith(FreightRate rate) {
        ArgumentNullException.ThrowIfNull(rate);

        return Math.Round(rate.PerRevenueTon * (decimal)RevenueTons, 2, MidpointRounding.AwayFromZero);
    }

    public bool IsLateAt(DateTimeOffset now) {
        return Status is ShipmentStatus.Booked or ShipmentStatus.InTransit && now > EstimatedArrival;
    }

    public void Depart(DateTimeOffset at) {
        if (Status != ShipmentStatus.Booked) {
            throw new InvalidOperationException($"A {Status} shipment cannot depart.");
        }
        Status     = ShipmentStatus.InTransit;
        DepartedAt = at;
    }

    public void Deliver(DateTimeOffset at) {
        if (Status != ShipmentStatus.InTransit) {
            throw new InvalidOperationException($"A {Status} shipment cannot be delivered.");
        }
        if (at < DepartedAt) {
            throw new ArgumentException("A shipment cannot be delivered before it departed.", nameof(at));
        }
        Status      = ShipmentStatus.Delivered;
        DeliveredAt = at;
    }

    public void Cancel() {
        if (Status != ShipmentStatus.Booked) {
            throw new InvalidOperationException($"A {Status} shipment cannot be cancelled.");
        }
        Status = ShipmentStatus.Cancelled;
    }

    private static void ValidateItinerary(IReadOnlyList<Leg> legs) {
        ArgumentNullException.ThrowIfNull(legs);
        if (legs.Count == 0) {
            throw new ArgumentException("An itinerary has at least one leg.", nameof(legs));
        }
        for (int index = 1; index < legs.Count; index++) {
            Leg previous = legs[index - 1];
            Leg next     = legs[index];
            if (!previous.Destination.Equals(next.Origin)) {
                throw new ArgumentException($"Leg {index} starts at {next.Origin}, not at {previous.Destination}.", nameof(legs));
            }
            if (next.Departure < previous.Arrival) {
                throw new ArgumentException($"Leg {index} departs before the previous one arrives.", nameof(legs));
            }
        }
    }

}
