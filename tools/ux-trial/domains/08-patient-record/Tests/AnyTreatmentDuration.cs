// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TreatmentDuration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TreatmentDuration" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTreatmentDuration : IAny<TreatmentDuration> {

    private readonly IAny<int> _days;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTreatmentDuration()
        : this(days: AnyValidDays()) { }

    private static AnyInt32 AnyValidDays() {
        return Any.Int32().Between(1, TreatmentDuration.MaxDays);
    }

    private AnyTreatmentDuration(IAny<int> days) {
        _days = days;
    }

    /// <summary>Pins <c>days</c> to a fixed value.</summary>
    public AnyTreatmentDuration WithDays(int value) {
        return new AnyTreatmentDuration(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>days</c> from <c>AnyValidDays()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTreatmentDuration WithDays(Func<AnyInt32, AnyInt32> configure) {
        return new AnyTreatmentDuration(configure(AnyValidDays()));
    }

    /// <summary>Produces one arbitrary <see cref="TreatmentDuration" />.</summary>
    public TreatmentDuration Generate() {
        return new TreatmentDuration(_days.Generate());
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
