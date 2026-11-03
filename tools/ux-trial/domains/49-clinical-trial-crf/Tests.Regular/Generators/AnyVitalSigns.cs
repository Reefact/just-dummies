using System;

using ClinicalTrial.Domain;
using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="VitalSigns" />. Scaffolded by `any`, then completed by hand: the
///     blood pressure is drawn as a pair, and <see cref="Generate" /> repairs the measurement time, so a drawn
///     value is always one the constructor accepts. A refusal is tested with the constructor, not here.
/// </summary>
public sealed partial class AnyVitalSigns : IAny<VitalSigns> {

    private IAny<(Observed<int>? Systolic, Observed<int>? Diastolic)> _bloodPressure;
    private IAny<Observed<int>?>                                       _heartRateBpm;
    private IAny<Observed<decimal>?>                                   _temperatureCelsius;
    private IAny<Observed<decimal>?>                                   _weightKg;
    private IAny<TimeOnly?>                                            _measuredAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVitalSigns() {
        _bloodPressure      = AnyValidSystolicMmHg().Then(s => DiastolicFor(s).As(d => (Systolic: s, Diastolic: d)));
        _heartRateBpm       = TrialFields.Entered(Any.Int32().Between(20, 250));
        _temperatureCelsius = TrialFields.Entered(Any.Decimal().Between(30m, 45m).WithScale(1));
        _weightKg           = TrialFields.Entered(Any.Decimal().Between(1m, 400m).WithScale(1));
        _measuredAt         = Any.TimeOnly().OrNull();
    }

    private static IAny<Observed<int>?> AnyValidSystolicMmHg() {
        return TrialFields.Entered(Any.Int32().Between(50, 300));
    }

    private static IAny<Observed<int>?> AnyValidDiastolicMmHg() {
        return TrialFields.Entered(Any.Int32().Between(20, 200));
    }

    /// <summary>The diastolic pressures that go with a systolic one: blood pressure is a pair.</summary>
    private static IAny<Observed<int>?> DiastolicFor(Observed<int>? systolic) {
        if (systolic is null) { return AnyValidDiastolicMmHg(); }

        if (systolic.IsObserved) {
            int                upperBound = Math.Min(200, systolic.Value!.Value - 1);
            IAny<Observed<int>?> observed = TrialFields.Observation(Any.Int32().Between(20, upperBound));

            return Any.Int32().Between(0, 9).Then(k => k < 8 ? observed : TrialFields.Blank<int>());
        }

        return Any.Int32().Between(0, 9).Then(k => k < 8 ? TrialFields.Absence<int>() : TrialFields.Blank<int>());
    }

    /// <summary>The systolic pressures that go with a diastolic one.</summary>
    private static IAny<Observed<int>?> SystolicFor(Observed<int>? diastolic) {
        if (diastolic is null) { return AnyValidSystolicMmHg(); }

        if (diastolic.IsObserved) {
            int                lowerBound = Math.Max(50, diastolic.Value!.Value + 1);
            IAny<Observed<int>?> observed = TrialFields.Observation(Any.Int32().Between(lowerBound, 300));

            return Any.Int32().Between(0, 9).Then(k => k < 8 ? observed : TrialFields.Blank<int>());
        }

        return Any.Int32().Between(0, 9).Then(k => k < 8 ? TrialFields.Absence<int>() : TrialFields.Blank<int>());
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVitalSigns(AnyVitalSigns from) {
        _bloodPressure      = from._bloodPressure;
        _heartRateBpm       = from._heartRateBpm;
        _temperatureCelsius = from._temperatureCelsius;
        _weightKg           = from._weightKg;
        _measuredAt         = from._measuredAt;
    }

    /// <summary>Pins <c>systolicMmHg</c>; the diastolic side is drawn so that the pair stays valid.</summary>
    public AnyVitalSigns WithSystolicMmHg(Observed<int>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._bloodPressure = DiastolicFor(value).As(d => (Systolic: value, Diastolic: d));

        return copy;
    }

    /// <summary>Pins <c>diastolicMmHg</c>; the systolic side is drawn so that the pair stays valid.</summary>
    public AnyVitalSigns WithDiastolicMmHg(Observed<int>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._bloodPressure = SystolicFor(value).As(s => (Systolic: s, Diastolic: value));

        return copy;
    }

    public AnyVitalSigns WithHeartRateBpm(Observed<int>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._heartRateBpm = Any.Fixed(value);

        return copy;
    }

    public AnyVitalSigns WithTemperatureCelsius(Observed<decimal>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._temperatureCelsius = Any.Fixed(value);

        return copy;
    }

    public AnyVitalSigns WithWeightKg(Observed<decimal>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._weightKg = Any.Fixed(value);

        return copy;
    }

    public AnyVitalSigns WithMeasuredAt(TimeOnly? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._measuredAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Every sign measured, and the time of the measurement noted: nothing left to resolve.</summary>
    public AnyVitalSigns Complete() {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._bloodPressure      = TrialFields.Observation(Any.Int32().Between(50, 300))
                                              .Then(s => TrialFields.Observation(Any.Int32().Between(20, Math.Min(200, s!.Value!.Value - 1)))
                                                                    .As(d => (Systolic: (Observed<int>?)s, Diastolic: d)));
        copy._heartRateBpm       = TrialFields.Observation(Any.Int32().Between(20, 250));
        copy._temperatureCelsius = TrialFields.Observation(Any.Decimal().Between(30m, 45m).WithScale(1));
        copy._weightKg           = TrialFields.Observation(Any.Decimal().Between(1m, 400m).WithScale(1));
        copy._measuredAt         = Any.TimeOnly().AsNullable();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VitalSigns" />.</summary>
    public VitalSigns Generate() {
        (Observed<int>? systolic, Observed<int>? diastolic) = _bloodPressure.Generate();
        Observed<int>?     heartRate   = _heartRateBpm.Generate();
        Observed<decimal>? temperature = _temperatureCelsius.Generate();
        Observed<decimal>? weight      = _weightKg.Generate();
        TimeOnly?          measuredAt  = _measuredAt.Generate();

        bool anyObserved = systolic is { IsObserved: true } || heartRate is { IsObserved: true }
                        || temperature is { IsObserved: true } || weight is { IsObserved: true };
        bool allResolved = systolic is not null && diastolic is not null && heartRate is not null
                        && temperature is not null && weight is not null;

        if (allResolved && !anyObserved) { measuredAt = null; }

        return new VitalSigns(systolic, diastolic, heartRate, temperature, weight, measuredAt);
    }

}
