namespace SmartGrid.Domain;

/// <summary>One index read on a delivery point, on a given day.</summary>
public sealed class MeterReading {

    public static readonly DateOnly SmartMeteringEpoch = new DateOnly(2010, 1, 1);

    public MeterReading(DeliveryPointId deliveryPoint, MeterIndex index, DateOnly readingDate, ReadingSource source) {
        ArgumentNullException.ThrowIfNull(deliveryPoint);
        ArgumentNullException.ThrowIfNull(index);
        if (readingDate < SmartMeteringEpoch) {
            throw new ArgumentOutOfRangeException(nameof(readingDate), readingDate, "No reading predates smart metering.");
        }

        Source = source switch {
            ReadingSource.Actual or ReadingSource.Estimated or ReadingSource.CustomerSupplied => source,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown reading source.")
        };
        DeliveryPoint = deliveryPoint;
        Index         = index;
        ReadingDate   = readingDate;
    }

    public DeliveryPointId DeliveryPoint { get; }
    public MeterIndex      Index         { get; }
    public DateOnly        ReadingDate   { get; }
    public ReadingSource   Source        { get; }

}
