// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CalibrationCurve --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CalibrationCurve" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCalibrationCurve : IAny<CalibrationCurve> {

    private readonly IAny<string>                          _name;
    private readonly IAny<IReadOnlyList<CalibrationPoint>> _points;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalibrationCurve()
        : this(name:   AnyValidName(),
               points: AnyValidPoints()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static IAny<IReadOnlyList<CalibrationPoint>> AnyValidPoints() {
        // A curve runs in strictly increasing calendar order: distinct years, then sorted.
        IEqualityComparer<CalibrationPoint> sameYear = EqualityComparer<CalibrationPoint>.Create((a, b) => a!.CalendarYear == b!.CalendarYear,
                                                                                                 p => p.CalendarYear);

        return Any.ListOf(new AnyCalibrationPoint())
                  .WithCountBetween(2, 20)
                  .Distinct(sameYear)
                  .As(points => (IReadOnlyList<CalibrationPoint>)points.OrderBy(p => p.CalendarYear).ToList());
    }

    private AnyCalibrationCurve(IAny<string>                          name,
                                IAny<IReadOnlyList<CalibrationPoint>> points) {
        _name   = name;
        _points = points;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyCalibrationCurve WithName(string value) {
        return new AnyCalibrationCurve(new FixedValue<string>(value), _points);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCalibrationCurve WithName(Func<AnyString, AnyString> configure) {
        return new AnyCalibrationCurve(configure(AnyValidName()), _points);
    }

    /// <summary>Pins <c>points</c> to a fixed value.</summary>
    public AnyCalibrationCurve WithPoints(IReadOnlyList<CalibrationPoint> value) {
        return new AnyCalibrationCurve(_name, new FixedValue<IReadOnlyList<CalibrationPoint>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="CalibrationCurve" />.</summary>
    public CalibrationCurve Generate() {
        return new CalibrationCurve(_name.Generate(),
                                    _points.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
