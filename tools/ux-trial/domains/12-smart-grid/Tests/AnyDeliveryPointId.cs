// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DeliveryPointId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DeliveryPointId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDeliveryPointId : IAny<DeliveryPointId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDeliveryPointId()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        // The constructor matches ^\d{14}$.
        return Any.String().Numeric().WithLength(14);
    }

    private AnyDeliveryPointId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyDeliveryPointId WithValue(string value) {
        return new AnyDeliveryPointId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDeliveryPointId WithValue(Func<AnyString, AnyString> configure) {
        return new AnyDeliveryPointId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="DeliveryPointId" />.</summary>
    public DeliveryPointId Generate() {
        return new DeliveryPointId(_value.Generate());
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
