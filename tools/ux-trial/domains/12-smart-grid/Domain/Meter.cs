namespace SmartGrid.Domain;

/// <summary>A meter on a delivery point: it accepts readings that move forward in time and never backwards in index.</summary>
public sealed class Meter {

    private readonly List<MeterReading> _readings = new List<MeterReading>();

    public Meter(DeliveryPointId deliveryPoint, TariffOption tariff, MeterReading commissioningReading) {
        ArgumentNullException.ThrowIfNull(deliveryPoint);
        ArgumentNullException.ThrowIfNull(commissioningReading);
        if (!Enum.IsDefined(tariff)) {
            throw new ArgumentOutOfRangeException(nameof(tariff), tariff, "Unknown tariff option.");
        }
        if (!commissioningReading.DeliveryPoint.Equals(deliveryPoint)) {
            throw new ArgumentException("The commissioning reading belongs to another delivery point.", nameof(commissioningReading));
        }
        if (commissioningReading.Source != ReadingSource.Actual) {
            throw new ArgumentException("A meter is commissioned on an actual reading.", nameof(commissioningReading));
        }

        DeliveryPoint = deliveryPoint;
        Tariff        = tariff;
        _readings.Add(commissioningReading);
    }

    public DeliveryPointId            DeliveryPoint { get; }
    public TariffOption               Tariff        { get; }
    public IReadOnlyList<MeterReading> Readings     => _readings;
    public MeterReading               LastReading   => _readings[^1];

    /// <summary>
    ///     Records a reading. A reading on the same day as an estimated last reading replaces it when it is actual;
    ///     otherwise a reading must be later and must not go below the last index.
    /// </summary>
    public ConsumptionPeriod Record(MeterReading reading) {
        ArgumentNullException.ThrowIfNull(reading);
        if (!reading.DeliveryPoint.Equals(DeliveryPoint)) {
            throw new InvalidOperationException("This reading belongs to another delivery point.");
        }

        MeterReading last = LastReading;
        if (reading.ReadingDate == last.ReadingDate && last.Source == ReadingSource.Estimated && reading.Source == ReadingSource.Actual) {
            _readings[^1] = reading;
            MeterReading previous = _readings[^2];

            return new ConsumptionPeriod(previous, reading);
        }
        if (reading.ReadingDate <= last.ReadingDate) {
            throw new InvalidOperationException($"A reading on {reading.ReadingDate} does not follow the last one on {last.ReadingDate}.");
        }
        if (reading.Index.Kwh < last.Index.Kwh) {
            throw new InvalidOperationException($"Index {reading.Index.Kwh} kWh is below the last one, {last.Index.Kwh} kWh.");
        }

        _readings.Add(reading);

        return new ConsumptionPeriod(last, reading);
    }

}
