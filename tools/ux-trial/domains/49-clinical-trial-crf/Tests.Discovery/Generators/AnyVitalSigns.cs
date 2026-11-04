// Scaffolded by `any` (JustDummies), then completed by hand: blood pressure is drawn as a pair, and the
// time of measurement follows the signs.

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary, valid <see cref="VitalSigns" />.</summary>
public sealed partial class AnyVitalSigns : IAny<VitalSigns> {

    private IAny<(Observed<int>? Systolic, Observed<int>? Diastolic)> _bloodPressure;
    private IAny<Observed<int>?>                                      _heartRateBpm;
    private IAny<Observed<decimal>?>                                  _temperatureCelsius;
    private IAny<Observed<decimal>?>                                  _weightKg;
    private IAny<TimeOnly?>                                           _measuredAt;

    public AnyVitalSigns() {
        _bloodPressure      = AnyValidBloodPressure();
        _heartRateBpm       = Observations.Entered(Any.Int32().Between(20, 250));
        _temperatureCelsius = Observations.Entered(Any.Decimal().Between(30m, 45m).WithScale(1));
        _weightKg           = Observations.Entered(Any.Decimal().Between(1m, 400m).WithScale(1));
        _measuredAt         = Any.TimeOnly().OrNull();
    }

    private AnyVitalSigns(AnyVitalSigns from) {
        _bloodPressure      = from._bloodPressure;
        _heartRateBpm       = from._heartRateBpm;
        _temperatureCelsius = from._temperatureCelsius;
        _weightKg           = from._weightKg;
        _measuredAt         = from._measuredAt;
    }

    // Systolic above diastolic when both are observed, and "a pair": an observed side never meets a missing one
    // (a blank side is fine).
    private static IAny<(Observed<int>?, Observed<int>?)> AnyValidBloodPressure() {
        return Any.Combine(Any.Pair(Any.Int32().Between(20, 200), Any.Int32().Between(50, 300)).Having.Item1LessThanItem2(),
                           Any.Int32().Between(0, 6),
                           Any.Enum<MissingReason>(),
                           Any.Enum<MissingReason>(),
                           (pair, shape, reasonA, reasonB) => {
                               Observed<int>  diastolic = Observed<int>.Of(pair.Item1);
                               Observed<int>  systolic  = Observed<int>.Of(pair.Item2);
                               Observed<int>? missingA  = Observed<int>.Missing(reasonA);
                               Observed<int>? missingB  = Observed<int>.Missing(reasonB);

                               return shape switch {
                                   0 => ((Observed<int>?)systolic, (Observed<int>?)diastolic),
                                   1 => (systolic, null),
                                   2 => (null, diastolic),
                                   3 => (null, null),
                                   4 => (missingA, missingB),
                                   5 => (missingA, null),
                                   _ => (null, missingB)
                               };
                           });
    }

    public AnyVitalSigns WithSystolicMmHg(Observed<int>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._bloodPressure = copy._bloodPressure.Then(pair => Any.Fixed((value, pair.Diastolic)));

        return copy;
    }

    public AnyVitalSigns WithDiastolicMmHg(Observed<int>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._bloodPressure = copy._bloodPressure.Then(pair => Any.Fixed((pair.Systolic, value)));

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

    public VitalSigns Generate() {
        (Observed<int>? systolic, Observed<int>? diastolic) = _bloodPressure.Generate();
        Observed<int>?                                      heartRate   = _heartRateBpm.Generate();
        Observed<decimal>?                                  temperature = _temperatureCelsius.Generate();
        Observed<decimal>?                                  weight      = _weightKg.Generate();

        bool anyObserved = systolic is { IsObserved: true } || heartRate is { IsObserved: true }
                        || temperature is { IsObserved: true } || weight is { IsObserved: true };
        bool allResolved = systolic is not null && diastolic is not null && heartRate is not null
                        && temperature is not null && weight is not null;

        // A time of measurement is refused when every sign is resolved and none observed: it follows the signs.
        TimeOnly? measuredAt = allResolved && !anyObserved ? null : _measuredAt.Generate();

        return new VitalSigns(systolic, diastolic, heartRate, temperature, weight, measuredAt);
    }

}
