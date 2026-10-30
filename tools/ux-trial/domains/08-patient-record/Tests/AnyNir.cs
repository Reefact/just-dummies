// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Nir --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Nir" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNir : IAny<Nir> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNir()
        : this(value: AnyValidValue()) { }

    // The 13-character body: sex, birth year, birth month, department (Corsica included), commune, order.
    private const string BodyPattern = "[12][0-9]{2}(0[1-9]|1[0-2])([0-9]{2}|2[AB])[0-9]{6}";

    private static IAny<string> AnyValidValue() {
        return WithControlKey(AnyValidBody());
    }

    private static AnyPattern AnyValidBody() {
        return Any.StringMatching(BodyPattern);
    }

    // The control key is a checksum of the body: no pattern can express it, so it is computed.
    private static IAny<string> WithControlKey(IAny<string> body) {
        return body.As(drawn => drawn + Nir.ComputeKey(drawn).ToString("00", System.Globalization.CultureInfo.InvariantCulture));
    }

    private AnyNir(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyNir WithValue(string value) {
        return new AnyNir(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNir WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyNir(WithControlKey(configure(AnyValidBody())));
    }

    /// <summary>Produces one arbitrary <see cref="Nir" />.</summary>
    public Nir Generate() {
        return new Nir(_value.Generate());
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
