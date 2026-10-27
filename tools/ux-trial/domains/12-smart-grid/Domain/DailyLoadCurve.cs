namespace SmartGrid.Domain;

/// <summary>A day of half-hourly points: 48 slots, contiguous and in order.</summary>
public sealed class DailyLoadCurve {

    public const int SlotsPerDay = 48;

    private static readonly TimeSpan Slot = TimeSpan.FromMinutes(30);

    public DailyLoadCurve(DeliveryPointId deliveryPoint, DateOnly day, IReadOnlyList<LoadCurvePoint> points) {
        ArgumentNullException.ThrowIfNull(deliveryPoint);
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count != SlotsPerDay) {
            throw new ArgumentException($"A daily load curve holds {SlotsPerDay} points, got {points.Count}.", nameof(points));
        }
        for (int slot = 0; slot < points.Count; slot++) {
            TimeOnly expected = TimeOnly.MinValue.Add(Slot * slot);
            if (points[slot].SlotStart != expected) {
                throw new ArgumentException($"Point {slot} starts at {points[slot].SlotStart}, expected {expected}.", nameof(points));
            }
        }

        DeliveryPoint = deliveryPoint;
        Day           = day;
        Points        = points;
    }

    public DeliveryPointId               DeliveryPoint { get; }
    public DateOnly                      Day           { get; }
    public IReadOnlyList<LoadCurvePoint> Points        { get; }

    public decimal TotalEnergyKwh => Points.Sum(point => point.EnergyKwh);

    public LoadCurvePoint Peak() {
        LoadCurvePoint peak = Points[0];
        foreach (LoadCurvePoint point in Points) {
            if (point.AveragePowerKw > peak.AveragePowerKw) {
                peak = point;
            }
        }

        return peak;
    }

    public decimal EnergyWithin(OffPeakWindow window) {
        ArgumentNullException.ThrowIfNull(window);

        return Points.Where(point => window.Contains(point.SlotStart)).Sum(point => point.EnergyKwh);
    }

}
