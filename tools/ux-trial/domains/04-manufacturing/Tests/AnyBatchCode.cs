// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BatchCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BatchCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBatchCode : IAny<BatchCode> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBatchCode()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // Fixed by hand: the scaffolder read only ThrowIfNullOrWhiteSpace and missed the format check.
        return Any.StringMatching(@"[A-Z]{3}-\d{2}(0[1-9]|[1-4]\d|5[0-3])-\d{4}");
    }

    private AnyBatchCode(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyBatchCode WithValue(string value) {
        return new AnyBatchCode(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatchCode WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyBatchCode(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="BatchCode" />.</summary>
    public BatchCode Generate() {
        return new BatchCode(_value.Generate());
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
