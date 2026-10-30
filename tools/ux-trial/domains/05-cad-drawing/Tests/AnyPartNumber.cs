// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PartNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PartNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPartNumber : IAny<PartNumber> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPartNumber()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        return Any.StringMatching(@"^P\d{6}(-[A-Z0-9]{2})?$");
    }

    private AnyPartNumber(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyPartNumber WithValue(string value) {
        return new AnyPartNumber(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPartNumber WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyPartNumber(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="PartNumber" />.</summary>
    public PartNumber Generate() {
        return new PartNumber(_value.Generate());
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
