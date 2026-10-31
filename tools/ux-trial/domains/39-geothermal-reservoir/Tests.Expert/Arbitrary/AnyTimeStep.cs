// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeStep --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="TimeStep" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTimeStep : IAny<TimeStep> {

    private readonly IAny<double> _seconds;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTimeStep()
        : this(seconds: AnyValidSeconds()) { }

    private static AnyDouble AnyValidSeconds() {
        // A step in (0 s, 1 year].
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(TimeStep.MaximumSeconds);
    }

    private AnyTimeStep(IAny<double> seconds) {
        _seconds = seconds;
    }

    /// <summary>Pins <c>seconds</c> to a fixed value.</summary>
    public AnyTimeStep WithSeconds(double value) {
        return new AnyTimeStep(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>seconds</c> from <c>AnyValidSeconds()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTimeStep WithSeconds(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTimeStep(configure(AnyValidSeconds()));
    }

    /// <summary>Produces one arbitrary <see cref="TimeStep" />.</summary>
    public TimeStep Generate() {
        return new TimeStep(_seconds.Generate());
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
