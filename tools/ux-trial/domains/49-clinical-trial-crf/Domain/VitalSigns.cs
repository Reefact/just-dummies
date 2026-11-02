namespace ClinicalTrial.Domain;

/// <summary>
/// The vital signs page of a visit. Each field is <see langword="null"/> while not entered, or resolved as
/// an observed value or a missing reason. Observed values are physiologically plausible; blood pressure is
/// taken as a pair; the time of measurement is recorded only when something was measured.
/// </summary>
public sealed class VitalSigns : IEquatable<VitalSigns>
{
    public VitalSigns(
        Observed<int>? systolicMmHg,
        Observed<int>? diastolicMmHg,
        Observed<int>? heartRateBpm,
        Observed<decimal>? temperatureCelsius,
        Observed<decimal>? weightKg,
        TimeOnly? measuredAt)
    {
        if (systolicMmHg?.Value is int systolic && (systolic < 50 || systolic > 300))
        {
            throw new ArgumentOutOfRangeException(nameof(systolicMmHg), systolic, "Systolic pressure is between 50 and 300 mmHg.");
        }

        if (diastolicMmHg?.Value is int diastolic && (diastolic < 20 || diastolic > 200))
        {
            throw new ArgumentOutOfRangeException(nameof(diastolicMmHg), diastolic, "Diastolic pressure is between 20 and 200 mmHg.");
        }

        if (systolicMmHg?.Value is int s && diastolicMmHg?.Value is int d && s <= d)
        {
            throw new ArgumentException($"Systolic pressure ({s}) must exceed diastolic pressure ({d}).", nameof(diastolicMmHg));
        }

        if ((systolicMmHg is { IsObserved: true } && diastolicMmHg is { IsObserved: false })
            || (diastolicMmHg is { IsObserved: true } && systolicMmHg is { IsObserved: false }))
        {
            throw new ArgumentException("Blood pressure is taken as a pair: one side cannot be observed while the other is missing.", nameof(diastolicMmHg));
        }

        if (heartRateBpm?.Value is int heartRate && (heartRate < 20 || heartRate > 250))
        {
            throw new ArgumentOutOfRangeException(nameof(heartRateBpm), heartRate, "Heart rate is between 20 and 250 bpm.");
        }

        if (temperatureCelsius?.Value is decimal temperature)
        {
            if (temperature < 30m || temperature > 45m)
            {
                throw new ArgumentOutOfRangeException(nameof(temperatureCelsius), temperature, "Body temperature is between 30.0 and 45.0 °C.");
            }

            if (decimal.Round(temperature, 1) != temperature)
            {
                throw new ArgumentException($"Body temperature is recorded to one decimal place, got {temperature}.", nameof(temperatureCelsius));
            }
        }

        if (weightKg?.Value is decimal weight)
        {
            if (weight < 1m || weight > 400m)
            {
                throw new ArgumentOutOfRangeException(nameof(weightKg), weight, "Weight is between 1.0 and 400.0 kg.");
            }

            if (decimal.Round(weight, 1) != weight)
            {
                throw new ArgumentException($"Weight is recorded to one decimal place, got {weight}.", nameof(weightKg));
            }
        }

        bool anyObserved = systolicMmHg is { IsObserved: true }
            || heartRateBpm is { IsObserved: true }
            || temperatureCelsius is { IsObserved: true }
            || weightKg is { IsObserved: true };
        bool allResolved = systolicMmHg is not null
            && diastolicMmHg is not null
            && heartRateBpm is not null
            && temperatureCelsius is not null
            && weightKg is not null;
        if (measuredAt.HasValue && allResolved && !anyObserved)
        {
            throw new ArgumentException("A time of measurement is recorded only when at least one sign was measured.", nameof(measuredAt));
        }

        SystolicMmHg = systolicMmHg;
        DiastolicMmHg = diastolicMmHg;
        HeartRateBpm = heartRateBpm;
        TemperatureCelsius = temperatureCelsius;
        WeightKg = weightKg;
        MeasuredAt = measuredAt;
    }

    public Observed<int>? SystolicMmHg { get; }

    public Observed<int>? DiastolicMmHg { get; }

    public Observed<int>? HeartRateBpm { get; }

    public Observed<decimal>? TemperatureCelsius { get; }

    public Observed<decimal>? WeightKg { get; }

    public TimeOnly? MeasuredAt { get; }

    public bool IsAnyObserved =>
        SystolicMmHg is { IsObserved: true }
        || HeartRateBpm is { IsObserved: true }
        || TemperatureCelsius is { IsObserved: true }
        || WeightKg is { IsObserved: true };

    /// <summary>
    /// The fields that still block signing: every sign not entered yet, and the time of measurement when
    /// a sign was observed.
    /// </summary>
    public IReadOnlyList<string> UnresolvedFields()
    {
        List<string> fields = new();
        if (SystolicMmHg is null)
        {
            fields.Add(nameof(SystolicMmHg));
        }

        if (DiastolicMmHg is null)
        {
            fields.Add(nameof(DiastolicMmHg));
        }

        if (HeartRateBpm is null)
        {
            fields.Add(nameof(HeartRateBpm));
        }

        if (TemperatureCelsius is null)
        {
            fields.Add(nameof(TemperatureCelsius));
        }

        if (WeightKg is null)
        {
            fields.Add(nameof(WeightKg));
        }

        if (IsAnyObserved && MeasuredAt is null)
        {
            fields.Add(nameof(MeasuredAt));
        }

        return fields;
    }

    public bool Equals(VitalSigns? other) =>
        other is not null
        && Equals(other.SystolicMmHg, SystolicMmHg)
        && Equals(other.DiastolicMmHg, DiastolicMmHg)
        && Equals(other.HeartRateBpm, HeartRateBpm)
        && Equals(other.TemperatureCelsius, TemperatureCelsius)
        && Equals(other.WeightKg, WeightKg)
        && other.MeasuredAt == MeasuredAt;

    public override bool Equals(object? obj) => Equals(obj as VitalSigns);

    public override int GetHashCode() =>
        HashCode.Combine(SystolicMmHg, DiastolicMmHg, HeartRateBpm, TemperatureCelsius, WeightKg, MeasuredAt);
}
