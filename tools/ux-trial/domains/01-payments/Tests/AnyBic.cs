// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Bic --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Bic" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBic : IAny<Bic> {

    private readonly IAny<string> _code;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBic()
        : this(code: AnyValidCode()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching("[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?");
    }

    private AnyBic(IAny<string> code) {
        _code = code;
    }

    /// <summary>Pins <c>code</c> to a fixed value.</summary>
    public AnyBic WithCode(string value) {
        return new AnyBic(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>code</c> from <c>AnyValidCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBic WithCode(Func<AnyPattern, AnyPattern> configure) {
        return new AnyBic(configure(AnyValidCode()));
    }

    /// <summary>Produces one arbitrary <see cref="Bic" />.</summary>
    public Bic Generate() {
        return new Bic(_code.Generate());
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
