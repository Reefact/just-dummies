// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BatchNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BatchNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBatchNumber : IAny<BatchNumber> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBatchNumber()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        // Verified by hand: the constructor also requires ^[A-Z0-9-]+$.
        return Any.String().NotBlank().WithMaxLength(20).WithChars("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-");
    }

    private AnyBatchNumber(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyBatchNumber WithValue(string value) {
        return new AnyBatchNumber(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatchNumber WithValue(Func<AnyString, AnyString> configure) {
        return new AnyBatchNumber(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="BatchNumber" />.</summary>
    public BatchNumber Generate() {
        return new BatchNumber(_value.Generate());
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
