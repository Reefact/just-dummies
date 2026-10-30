// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Iccid --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Iccid" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIccid : IAny<Iccid> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIccid()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // Iccid's guard, copied from its constructor: ^89[0-9]{17,18}$.
        return Any.StringMatching(@"89[0-9]{17,18}");
    }

    private AnyIccid(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyIccid WithValue(string value) {
        return new AnyIccid(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIccid WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyIccid(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="Iccid" />.</summary>
    public Iccid Generate() {
        return new Iccid(_value.Generate());
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
