// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Surface --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Surface" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySurface : IAny<Surface> {

    private readonly IAny<double> _squareMeters;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySurface()
        : this(squareMeters: AnyValidSquareMeters().As(ToDouble)) { }

    private static AnyDecimal AnyValidSquareMeters() {
        // A surface is measured to the hundredth of a square metre. AnyDouble has no WithScale, so the
        // grid is drawn as a decimal and converted.
        return Any.Decimal().Positive().LessThanOrEqualTo((decimal)Surface.MaxSquareMeters).WithScale(2);
    }

    private static double ToDouble(decimal squareMeters) {
        return (double)squareMeters;
    }

    private AnySurface(IAny<double> squareMeters) {
        _squareMeters = squareMeters;
    }

    /// <summary>Pins <c>squareMeters</c> to a fixed value.</summary>
    public AnySurface WithSquareMeters(double value) {
        return new AnySurface(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>squareMeters</c> from <c>AnyValidSquareMeters()</c>, refined by <paramref name="configure" />.</summary>
    public AnySurface WithSquareMeters(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnySurface(configure(AnyValidSquareMeters()).As(ToDouble));
    }

    /// <summary>Produces one arbitrary <see cref="Surface" />.</summary>
    public Surface Generate() {
        return new Surface(_squareMeters.Generate());
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
