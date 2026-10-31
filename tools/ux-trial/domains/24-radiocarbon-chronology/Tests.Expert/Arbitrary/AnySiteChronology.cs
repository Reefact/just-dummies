// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SiteChronology --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SiteChronology" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySiteChronology : IAny<SiteChronology> {

    private readonly IAny<string>           _siteName;
    private readonly IAny<CalibrationCurve> _curve;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySiteChronology()
        : this(siteName: AnyValidSiteName(),
               curve:    AnyValidCurve()) { }

    private static AnyString AnyValidSiteName() {
        return Any.String().NotBlank().WithMaxLength(120);
    }

    private static AnyCalibrationCurve AnyValidCurve() {
        return new AnyCalibrationCurve();
    }

    private AnySiteChronology(IAny<string>           siteName,
                              IAny<CalibrationCurve> curve) {
        _siteName = siteName;
        _curve    = curve;
    }

    /// <summary>Pins <c>siteName</c> to a fixed value.</summary>
    public AnySiteChronology WithSiteName(string value) {
        return new AnySiteChronology(new FixedValue<string>(value), _curve);
    }

    /// <summary>Draws <c>siteName</c> from <c>AnyValidSiteName()</c>, refined by <paramref name="configure" />.</summary>
    public AnySiteChronology WithSiteName(Func<AnyString, AnyString> configure) {
        return new AnySiteChronology(configure(AnyValidSiteName()), _curve);
    }

    /// <summary>Pins <c>curve</c> to a fixed value.</summary>
    public AnySiteChronology WithCurve(CalibrationCurve value) {
        return new AnySiteChronology(_siteName, new FixedValue<CalibrationCurve>(value));
    }

    /// <summary>Draws <c>curve</c> from <c>AnyValidCurve()</c>, refined by <paramref name="configure" />.</summary>
    public AnySiteChronology WithCurve(Func<AnyCalibrationCurve, AnyCalibrationCurve> configure) {
        return new AnySiteChronology(_siteName, configure(AnyValidCurve()));
    }

    /// <summary>Produces one arbitrary <see cref="SiteChronology" />.</summary>
    public SiteChronology Generate() {
        return new SiteChronology(_siteName.Generate(),
                                  _curve.Generate());
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
