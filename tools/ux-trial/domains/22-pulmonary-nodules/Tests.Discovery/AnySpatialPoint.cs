// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SpatialPoint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SpatialPoint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpatialPoint : IAny<SpatialPoint> {

    private readonly IAny<double> _xMm;
    private readonly IAny<double> _yMm;
    private readonly IAny<double> _zMm;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpatialPoint()
        : this(xMm: AnyValidXMm(),
               yMm: AnyValidYMm(),
               zMm: AnyValidZMm()) { }

    private static AnyDouble AnyValidXMm() {
        // SpatialPoint guards each coordinate through a helper the scaffolder stays silent on: ±2000 mm.
        return Any.Double().Between(-SpatialPoint.MaxAbsoluteCoordinateMm, SpatialPoint.MaxAbsoluteCoordinateMm);
    }

    private static AnyDouble AnyValidYMm() {
        // SpatialPoint guards each coordinate through a helper the scaffolder stays silent on: ±2000 mm.
        return Any.Double().Between(-SpatialPoint.MaxAbsoluteCoordinateMm, SpatialPoint.MaxAbsoluteCoordinateMm);
    }

    private static AnyDouble AnyValidZMm() {
        // SpatialPoint guards each coordinate through a helper the scaffolder stays silent on: ±2000 mm.
        return Any.Double().Between(-SpatialPoint.MaxAbsoluteCoordinateMm, SpatialPoint.MaxAbsoluteCoordinateMm);
    }

    private AnySpatialPoint(IAny<double> xMm,
                            IAny<double> yMm,
                            IAny<double> zMm) {
        _xMm = xMm;
        _yMm = yMm;
        _zMm = zMm;
    }

    /// <summary>Pins <c>xMm</c> to a fixed value.</summary>
    public AnySpatialPoint WithXMm(double value) {
        return new AnySpatialPoint(new FixedValue<double>(value), _yMm, _zMm);
    }

    /// <summary>Draws <c>xMm</c> from <c>AnyValidXMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpatialPoint WithXMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpatialPoint(configure(AnyValidXMm()), _yMm, _zMm);
    }

    /// <summary>Pins <c>yMm</c> to a fixed value.</summary>
    public AnySpatialPoint WithYMm(double value) {
        return new AnySpatialPoint(_xMm, new FixedValue<double>(value), _zMm);
    }

    /// <summary>Draws <c>yMm</c> from <c>AnyValidYMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpatialPoint WithYMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpatialPoint(_xMm, configure(AnyValidYMm()), _zMm);
    }

    /// <summary>Pins <c>zMm</c> to a fixed value.</summary>
    public AnySpatialPoint WithZMm(double value) {
        return new AnySpatialPoint(_xMm, _yMm, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>zMm</c> from <c>AnyValidZMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpatialPoint WithZMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpatialPoint(_xMm, _yMm, configure(AnyValidZMm()));
    }

    /// <summary>Produces one arbitrary <see cref="SpatialPoint" />.</summary>
    public SpatialPoint Generate() {
        return new SpatialPoint(_xMm.Generate(),
                                _yMm.Generate(),
                                _zMm.Generate());
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
