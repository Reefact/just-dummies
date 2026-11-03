using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReferenceRange" /> values. Started from the scaffold, which paired
///     <c>lower</c> and <c>upper</c> correctly but could not tie the critical limits to the bounds: they are drawn
///     here from the bounds actually drawn, so a critical low never exceeds the lower bound and a critical high is
///     never below the upper one. Whether the critical limits exist is decided by <c>WithCriticalLimits</c> /
///     <c>WithoutCriticalLimits</c>; left alone, each is absent one draw in ten.
/// </summary>
public sealed partial class AnyReferenceRange : IAny<ReferenceRange> {

    private const double Ceiling = 1_000d;

    private AnyOrderedPair<double> _lowerAndUpper;
    private IAny<MeasurementUnit>  _unit;
    private bool?                  _criticalLimits;

    private AnyMeasurementUnit _unitRecipe;

    private double? _lowerPin;
    private double? _upperPin;

    public AnyReferenceRange()
        : this(unit: new AnyMeasurementUnit()) { }

    private AnyReferenceRange(AnyMeasurementUnit unit) {
        _unit           = unit;
        _unitRecipe     = unit;
        _lowerAndUpper  = LowerAndUpper();
    }

    private AnyReferenceRange(AnyReferenceRange from) {
        _lowerAndUpper  = from._lowerAndUpper;
        _unit           = from._unit;
        _unitRecipe     = from._unitRecipe;
        _criticalLimits = from._criticalLimits;
        _lowerPin       = from._lowerPin;
        _upperPin       = from._upperPin;
    }

    private AnyOrderedPair<double> LowerAndUpper() {
        AnyDouble              bound = Any.Double().Between(0d, Ceiling);
        AnyOrderedPair<double> pair  = Any.OrderedPair(bound, bound, strict: true);

        if (_lowerPin is { } lower) { pair = pair.WithFirst(lower); }
        if (_upperPin is { } upper) { pair = pair.WithSecond(upper); }

        return pair;
    }

    public AnyReferenceRange WithLower(double value) {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._lowerPin      = value;
        copy._lowerAndUpper = copy.LowerAndUpper();

        return copy;
    }

    public AnyReferenceRange WithUpper(double value) {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._upperPin      = value;
        copy._lowerAndUpper = copy.LowerAndUpper();

        return copy;
    }

    public AnyReferenceRange WithUnit(MeasurementUnit value) {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._unit = Any.Fixed(value);

        return copy;
    }

    public AnyReferenceRange WithUnit(Func<AnyMeasurementUnit, AnyMeasurementUnit> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._unitRecipe = configure(_unitRecipe);
        copy._unit       = copy._unitRecipe;

        return copy;
    }

    /// <summary>Both critical limits are present, each on its side of the normal interval.</summary>
    public AnyReferenceRange WithCriticalLimits() {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._criticalLimits = true;

        return copy;
    }

    /// <summary>Neither critical limit is present.</summary>
    public AnyReferenceRange WithoutCriticalLimits() {
        AnyReferenceRange copy = new AnyReferenceRange(this);

        copy._criticalLimits = false;

        return copy;
    }

    public ReferenceRange Generate() {
        (double lower, double upper) = _lowerAndUpper.Generate();

        AnyDouble criticalLow  = Any.Double().Between(0d, lower);
        AnyDouble criticalHigh = Any.Double().Between(upper, 2 * Ceiling);

        double? low  = _criticalLimits switch { true => criticalLow.Generate(), false => null, null => criticalLow.OrNull().Generate() };
        double? high = _criticalLimits switch { true => criticalHigh.Generate(), false => null, null => criticalHigh.OrNull().Generate() };

        return new ReferenceRange(lower, upper, _unit.Generate(), low, high);
    }

}
