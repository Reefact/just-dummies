// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SensorId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SensorId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySensorId : IAny<SensorId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySensorId()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        return Any.StringMatching("^AG-[0-9A-F]{6}$");
    }

    private AnySensorId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnySensorId WithValue(string value) {
        return new AnySensorId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnySensorId WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnySensorId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="SensorId" />.</summary>
    public SensorId Generate() {
        return new SensorId(_value.Generate());
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
