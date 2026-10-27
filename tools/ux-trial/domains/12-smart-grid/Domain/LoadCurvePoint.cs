namespace SmartGrid.Domain;

/// <summary>The average power drawn over one half-hour slot.</summary>
public sealed class LoadCurvePoint {

    public const decimal MaxPowerKw = 36m;

    public LoadCurvePoint(TimeOnly slotStart, decimal averagePowerKw) {
        Validate(slotStart, averagePowerKw);
        SlotStart      = slotStart;
        AveragePowerKw = averagePowerKw;
    }

    public TimeOnly SlotStart      { get; }
    public decimal  AveragePowerKw { get; }

    /// <summary>The energy of the slot: average power over half an hour.</summary>
    public decimal EnergyKwh => AveragePowerKw / 2m;

    private static void Validate(TimeOnly slotStart, decimal averagePowerKw) {
        if (slotStart.Minute % 30 != 0) {
            throw new ArgumentException("A slot starts on the hour or on the half hour.", nameof(slotStart));
        }
        if (slotStart.Second != 0) {
            throw new ArgumentException("A slot starts on a whole minute.", nameof(slotStart));
        }
        if (slotStart.Millisecond != 0 || slotStart.Microsecond != 0) {
            throw new ArgumentException("A slot starts on a whole second.", nameof(slotStart));
        }
        if (averagePowerKw < 0m || averagePowerKw > MaxPowerKw) {
            throw new ArgumentOutOfRangeException(nameof(averagePowerKw), averagePowerKw, "Power must lie within the meter's rating.");
        }
    }

}
