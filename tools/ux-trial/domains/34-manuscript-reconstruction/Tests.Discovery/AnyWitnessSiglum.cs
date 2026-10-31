// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate WitnessSiglum --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="WitnessSiglum" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWitnessSiglum : IAny<WitnessSiglum> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWitnessSiglum()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // A siglum: an uppercase letter, then up to 11 letters, digits or dots (1 to 12 characters).
        return Any.StringMatching("[A-Z][A-Za-z0-9.]{0,11}");
    }

    private AnyWitnessSiglum(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyWitnessSiglum WithValue(string value) {
        return new AnyWitnessSiglum(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWitnessSiglum WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyWitnessSiglum(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="WitnessSiglum" />.</summary>
    public WitnessSiglum Generate() {
        return new WitnessSiglum(_value.Generate());
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
