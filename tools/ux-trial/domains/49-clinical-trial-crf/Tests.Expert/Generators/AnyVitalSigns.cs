// Started from `any generate VitalSigns`, then rewritten by hand: five of its six parameters were TODOs (the tool cannot
// draw an Observed<T>?) and the sixth was tied to the rest. The page is drawn as a whole: blood pressure as the pair
// it is, the time of measurement last, once it is known whether anything was measured.

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary, valid <see cref="VitalSigns" /> values — blank, missing and observed signs alike.</summary>
public sealed partial class AnyVitalSigns : IAny<VitalSigns> {

    private enum PressureShape { Observed, Missing, SystolicBlank, DiastolicBlank, BothBlank }

    private readonly bool _resolved;

    private (Observed<int>? Systolic, Observed<int>? Diastolic)? _pressure;
    private Pin<Observed<int>?>                                   _heartRate;
    private Pin<Observed<decimal>?>                               _temperature;
    private Pin<Observed<decimal>?>                               _weight;
    private Pin<TimeOnly?>                                        _measuredAt;

    private readonly struct Pin<T> {

        public Pin(T value) {
            IsSet = true;
            Value = value;
        }

        public bool IsSet { get; }
        public T    Value { get; }

    }

    public AnyVitalSigns() { }

    private AnyVitalSigns(AnyVitalSigns from, bool resolved) {
        _resolved    = resolved;
        _pressure    = from._pressure;
        _heartRate   = from._heartRate;
        _temperature = from._temperature;
        _weight      = from._weight;
        _measuredAt  = from._measuredAt;
    }

    /// <summary>Every sign is resolved (observed, or missing for a reason): nothing blank, so the page can be signed.</summary>
    public AnyVitalSigns Resolved() {
        return new AnyVitalSigns(this, resolved: true);
    }

    public AnyVitalSigns WithBloodPressure(Observed<int>? systolic, Observed<int>? diastolic) {
        AnyVitalSigns copy = new AnyVitalSigns(this, _resolved);

        copy._pressure = (systolic, diastolic);

        return copy;
    }

    public AnyVitalSigns WithHeartRate(Observed<int>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this, _resolved);

        copy._heartRate = new Pin<Observed<int>?>(value);

        return copy;
    }

    public AnyVitalSigns WithTemperature(Observed<decimal>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this, _resolved);

        copy._temperature = new Pin<Observed<decimal>?>(value);

        return copy;
    }

    public AnyVitalSigns WithWeight(Observed<decimal>? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this, _resolved);

        copy._weight = new Pin<Observed<decimal>?>(value);

        return copy;
    }

    public AnyVitalSigns WithMeasuredAt(TimeOnly? value) {
        AnyVitalSigns copy = new AnyVitalSigns(this, _resolved);

        copy._measuredAt = new Pin<TimeOnly?>(value);

        return copy;
    }

    public VitalSigns Generate() {
        (Observed<int>? systolic, Observed<int>? diastolic) = _pressure ?? BloodPressure().Generate();

        Observed<int>?     heartRate   = _heartRate.IsSet   ? _heartRate.Value   : Sign(Any.Int32().Between(20, 250)).Generate();
        Observed<decimal>? temperature = _temperature.IsSet ? _temperature.Value : Sign(Any.Decimal().Between(30m, 45m).WithScale(1)).Generate();
        Observed<decimal>? weight      = _weight.IsSet      ? _weight.Value      : Sign(Any.Decimal().Between(1m, 400m).WithScale(1)).Generate();

        bool anyObserved = systolic is { IsObserved: true } || heartRate is { IsObserved: true }
                        || temperature is { IsObserved: true } || weight is { IsObserved: true };
        bool allResolved = systolic is not null && diastolic is not null && heartRate is not null
                        && temperature is not null && weight is not null;

        TimeOnly? measuredAt;
        if (_measuredAt.IsSet) {
            measuredAt = _measuredAt.Value;
        } else if (allResolved && !anyObserved) {
            measuredAt = null;
        } else if (_resolved && anyObserved) {
            measuredAt = Any.TimeOnly().Generate();
        } else {
            measuredAt = Any.TimeOnly().OrNull().Generate();
        }

        return new VitalSigns(systolic, diastolic, heartRate, temperature, weight, measuredAt);
    }

    private IAny<Observed<T>?> Sign<T>(IAny<T> values) where T : struct {
        return _resolved ? AnyObservedOf.Resolved(values) : AnyObservedOf.MaybeBlank(values);
    }

    // Blood pressure is a pair: both observed (systolic above diastolic), both missing, or a blank side.
    private IAny<(Observed<int>? Systolic, Observed<int>? Diastolic)> BloodPressure() {
        IAny<(Observed<int>? Systolic, Observed<int>? Diastolic)> observed =
            Any.OrderedPair(Any.Int32().Between(20, 200), Any.Int32().Between(50, 300), strict: true)
               .As(pair => ((Observed<int>?)Observed<int>.Of(pair.Item2), (Observed<int>?)Observed<int>.Of(pair.Item1)));

        IAny<(Observed<int>? Systolic, Observed<int>? Diastolic)> missing =
            Any.Combine(AnyObservedOf.Missing<int>(), AnyObservedOf.Missing<int>(),
                        (s, d) => ((Observed<int>?)s, (Observed<int>?)d));

        if (_resolved) {
            return Any.Boolean().Then(isObserved => isObserved ? observed : missing);
        }

        IAny<Observed<int>> aSystolic  = AnyObservedOf.Resolved(Any.Int32().Between(50, 300));
        IAny<Observed<int>> aDiastolic = AnyObservedOf.Resolved(Any.Int32().Between(20, 200));

        return Any.Enum<PressureShape>().Then(shape => shape switch {
            PressureShape.Observed       => observed,
            PressureShape.Missing        => missing,
            PressureShape.SystolicBlank  => aDiastolic.As(d => ((Observed<int>?)null, (Observed<int>?)d)),
            PressureShape.DiastolicBlank => aSystolic.As(s => ((Observed<int>?)s, (Observed<int>?)null)),
            _                            => Any.Fixed(((Observed<int>?)null, (Observed<int>?)null))
        });
    }

}
