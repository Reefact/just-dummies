// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate UnLocode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="UnLocode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyUnLocode : IAny<UnLocode> {

    private readonly IAny<string> _code;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyUnLocode()
        : this(code: AnyValidCode()) { }

    private static AnyPattern AnyValidCode() {
        // UN/LOCODE: two-letter country, then three letters or digits 2-9.
        return Any.StringMatching("[A-Z]{2}[A-Z2-9]{3}");
    }

    private AnyUnLocode(IAny<string> code) {
        _code = code;
    }

    /// <summary>Pins <c>code</c> to a fixed value.</summary>
    public AnyUnLocode WithCode(string value) {
        return new AnyUnLocode(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>code</c> from <c>AnyValidCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyUnLocode WithCode(Func<AnyPattern, AnyPattern> configure) {
        return new AnyUnLocode(configure(AnyValidCode()));
    }

    /// <summary>Produces one arbitrary <see cref="UnLocode" />.</summary>
    public UnLocode Generate() {
        return new UnLocode(_code.Generate());
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
