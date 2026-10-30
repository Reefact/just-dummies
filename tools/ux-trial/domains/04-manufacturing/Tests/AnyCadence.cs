// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Cadence --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Cadence" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCadence : IAny<Cadence> {

    private readonly IAny<int> _unitsPerHour;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCadence()
        : this(unitsPerHour: AnyValidUnitsPerHour()) { }

    private static AnyInt32 AnyValidUnitsPerHour() {
        return Any.Int32().Positive().LessThanOrEqualTo(10000);
    }

    private AnyCadence(IAny<int> unitsPerHour) {
        _unitsPerHour = unitsPerHour;
    }

    /// <summary>Pins <c>unitsPerHour</c> to a fixed value.</summary>
    public AnyCadence WithUnitsPerHour(int value) {
        return new AnyCadence(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>unitsPerHour</c> from <c>AnyValidUnitsPerHour()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCadence WithUnitsPerHour(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCadence(configure(AnyValidUnitsPerHour()));
    }

    /// <summary>Produces one arbitrary <see cref="Cadence" />.</summary>
    public Cadence Generate() {
        return new Cadence(_unitsPerHour.Generate());
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
