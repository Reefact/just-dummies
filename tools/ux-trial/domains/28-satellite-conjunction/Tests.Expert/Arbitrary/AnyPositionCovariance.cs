// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PositionCovariance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="PositionCovariance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPositionCovariance : IAny<PositionCovariance> {

    private readonly IAny<double> _xx;
    private readonly IAny<double> _yy;
    private readonly IAny<double> _zz;
    private readonly IAny<double> _xy;
    private readonly IAny<double> _xz;
    private readonly IAny<double> _yz;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPositionCovariance()
        : this(xx: AnyValidXx(),
               yy: AnyValidYy(),
               zz: AnyValidZz(),
               xy: AnyValidXy(),
               xz: AnyValidXz(),
               yz: AnyValidYz()) { }

    private static AnyDouble AnyValidXx() {
        return AnyVariance();
    }

    private static AnyDouble AnyValidYy() {
        return AnyVariance();
    }

    private static AnyDouble AnyValidZz() {
        return AnyVariance();
    }

    private static AnyDouble AnyValidXy() {
        return AnyCrossTerm();
    }

    private static AnyDouble AnyValidXz() {
        return AnyCrossTerm();
    }

    private static AnyDouble AnyValidYz() {
        return AnyCrossTerm();
    }

    private AnyPositionCovariance(IAny<double> xx,
                                  IAny<double> yy,
                                  IAny<double> zz,
                                  IAny<double> xy,
                                  IAny<double> xz,
                                  IAny<double> yz) {
        _xx = xx;
        _yy = yy;
        _zz = zz;
        _xy = xy;
        _xz = xz;
        _yz = yz;
    }

    /// <summary>Pins <c>xx</c> to a fixed value.</summary>
    public AnyPositionCovariance WithXx(double value) {
        return new AnyPositionCovariance(new FixedValue<double>(value), _yy, _zz, _xy, _xz, _yz);
    }

    /// <summary>Draws <c>xx</c> from <c>AnyValidXx()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPositionCovariance WithXx(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPositionCovariance(configure(AnyValidXx()), _yy, _zz, _xy, _xz, _yz);
    }

    /// <summary>Pins <c>yy</c> to a fixed value.</summary>
    public AnyPositionCovariance WithYy(double value) {
        return new AnyPositionCovariance(_xx, new FixedValue<double>(value), _zz, _xy, _xz, _yz);
    }

    /// <summary>Draws <c>yy</c> from <c>AnyValidYy()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPositionCovariance WithYy(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPositionCovariance(_xx, configure(AnyValidYy()), _zz, _xy, _xz, _yz);
    }

    /// <summary>Pins <c>zz</c> to a fixed value.</summary>
    public AnyPositionCovariance WithZz(double value) {
        return new AnyPositionCovariance(_xx, _yy, new FixedValue<double>(value), _xy, _xz, _yz);
    }

    /// <summary>Draws <c>zz</c> from <c>AnyValidZz()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPositionCovariance WithZz(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPositionCovariance(_xx, _yy, configure(AnyValidZz()), _xy, _xz, _yz);
    }

    /// <summary>Pins <c>xy</c> to a fixed value.</summary>
    public AnyPositionCovariance WithXy(double value) {
        return new AnyPositionCovariance(_xx, _yy, _zz, new FixedValue<double>(value), _xz, _yz);
    }

    /// <summary>Draws <c>xy</c> from <c>AnyValidXy()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPositionCovariance WithXy(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPositionCovariance(_xx, _yy, _zz, configure(AnyValidXy()), _xz, _yz);
    }

    /// <summary>Pins <c>xz</c> to a fixed value.</summary>
    public AnyPositionCovariance WithXz(double value) {
        return new AnyPositionCovariance(_xx, _yy, _zz, _xy, new FixedValue<double>(value), _yz);
    }

    /// <summary>Draws <c>xz</c> from <c>AnyValidXz()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPositionCovariance WithXz(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPositionCovariance(_xx, _yy, _zz, _xy, configure(AnyValidXz()), _yz);
    }

    /// <summary>Pins <c>yz</c> to a fixed value.</summary>
    public AnyPositionCovariance WithYz(double value) {
        return new AnyPositionCovariance(_xx, _yy, _zz, _xy, _xz, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>yz</c> from <c>AnyValidYz()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPositionCovariance WithYz(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPositionCovariance(_xx, _yy, _zz, _xy, _xz, configure(AnyValidYz()));
    }

    /// <summary>Produces one arbitrary <see cref="PositionCovariance" />.</summary>
    public PositionCovariance Generate() {
        return new PositionCovariance(_xx.Generate(),
                                      _yy.Generate(),
                                      _zz.Generate(),
                                      _xy.Generate(),
                                      _xz.Generate(),
                                      _yz.Generate());
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
