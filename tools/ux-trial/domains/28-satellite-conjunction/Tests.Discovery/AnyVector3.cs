// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SatelliteConjunction.Domain.Vector3 --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Vector3" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVector3 : IAny<Vector3> {

    private readonly IAny<double> _x;
    private readonly IAny<double> _y;
    private readonly IAny<double> _z;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVector3()
        : this(x: AnyValidX(),
               y: AnyValidY(),
               z: AnyValidZ()) { }

    private static AnyDouble AnyValidX() {
        return Any.Double();
    }

    private static AnyDouble AnyValidY() {
        return Any.Double();
    }

    private static AnyDouble AnyValidZ() {
        return Any.Double();
    }

    private AnyVector3(IAny<double> x,
                       IAny<double> y,
                       IAny<double> z) {
        _x = x;
        _y = y;
        _z = z;
    }

    /// <summary>Pins <c>x</c> to a fixed value.</summary>
    public AnyVector3 WithX(double value) {
        return new AnyVector3(new FixedValue<double>(value), _y, _z);
    }

    /// <summary>Draws <c>x</c> from <c>AnyValidX()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVector3 WithX(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVector3(configure(AnyValidX()), _y, _z);
    }

    /// <summary>Pins <c>y</c> to a fixed value.</summary>
    public AnyVector3 WithY(double value) {
        return new AnyVector3(_x, new FixedValue<double>(value), _z);
    }

    /// <summary>Draws <c>y</c> from <c>AnyValidY()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVector3 WithY(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVector3(_x, configure(AnyValidY()), _z);
    }

    /// <summary>Pins <c>z</c> to a fixed value.</summary>
    public AnyVector3 WithZ(double value) {
        return new AnyVector3(_x, _y, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>z</c> from <c>AnyValidZ()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVector3 WithZ(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVector3(_x, _y, configure(AnyValidZ()));
    }

    /// <summary>Produces one arbitrary <see cref="Vector3" />.</summary>
    public Vector3 Generate() {
        return new Vector3(_x.Generate(),
                           _y.Generate(),
                           _z.Generate());
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
