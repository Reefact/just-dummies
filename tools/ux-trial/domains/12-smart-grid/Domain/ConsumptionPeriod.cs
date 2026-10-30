namespace SmartGrid.Domain;

/// <summary>The energy consumed between two readings of the same delivery point.</summary>
public sealed class ConsumptionPeriod {

    public ConsumptionPeriod(MeterReading opening, MeterReading closing) {
        ArgumentNullException.ThrowIfNull(opening);
        ArgumentNullException.ThrowIfNull(closing);
        if (!opening.DeliveryPoint.Equals(closing.DeliveryPoint)) {
            throw new ArgumentException("Both readings must belong to the same delivery point.", nameof(closing));
        }
        if (closing.ReadingDate <= opening.ReadingDate) {
            throw new ArgumentException("The closing reading must come after the opening one.", nameof(closing));
        }
        if (closing.Index.Kwh < opening.Index.Kwh) {
            throw new ArgumentException("A meter index never goes down.", nameof(closing));
        }

        Opening = opening;
        Closing = closing;
    }

    public MeterReading Opening { get; }
    public MeterReading Closing { get; }

    public int Days => Closing.ReadingDate.DayNumber - Opening.ReadingDate.DayNumber;

    public decimal ConsumedKwh => Closing.Index.Kwh - Opening.Index.Kwh;

    public decimal AverageDailyKwh => Math.Round(ConsumedKwh / Days, 3, MidpointRounding.ToEven);

    public bool IsEstimated => Opening.Source == ReadingSource.Estimated || Closing.Source == ReadingSource.Estimated;

}
