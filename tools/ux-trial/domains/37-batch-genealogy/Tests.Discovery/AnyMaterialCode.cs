// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MaterialCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MaterialCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMaterialCode : IAny<MaterialCode> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMaterialCode()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        return Any.StringMatching("MAT-[0-9]{5}");
    }

    private AnyMaterialCode(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyMaterialCode WithValue(string value) {
        return new AnyMaterialCode(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMaterialCode WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyMaterialCode(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="MaterialCode" />.</summary>
    public MaterialCode Generate() {
        return new MaterialCode(_value.Generate());
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
