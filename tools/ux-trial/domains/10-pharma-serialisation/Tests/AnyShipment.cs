// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Shipment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Shipment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShipment : IAny<Shipment> {

    private readonly IAny<string>                      _reference;
    private readonly IAny<IReadOnlyList<SerialNumber>> _serials;
    private readonly IAny<DateOnly>                    _shippedOn;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShipment()
        : this(reference: AnyValidReference(),
               serials:   AnyValidSerials(),
               shippedOn: AnyValidShippedOn()) { }

    private static AnyString AnyValidReference() {
        // Verified by hand: the constructor requires ^SHP-\d{8}$.
        return Any.String().StartingWith("SHP-").Numeric().WithLength(12);
    }

    private static AnyList<SerialNumber> AnyValidSerials() {
        // Verified by hand: the constructor refuses a repeated serial.
        return Any.ListOf(new AnySerialNumber()).NonEmpty().Distinct();
    }

    private static AnyDateOnly AnyValidShippedOn() {
        return Any.DateOnly();
    }

    private AnyShipment(IAny<string>                      reference,
                        IAny<IReadOnlyList<SerialNumber>> serials,
                        IAny<DateOnly>                    shippedOn) {
        _reference = reference;
        _serials   = serials;
        _shippedOn = shippedOn;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyShipment WithReference(string value) {
        return new AnyShipment(new FixedValue<string>(value), _serials, _shippedOn);
    }

    /// <summary>Draws <c>reference</c> from <c>AnyValidReference()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithReference(Func<AnyString, AnyString> configure) {
        return new AnyShipment(configure(AnyValidReference()), _serials, _shippedOn);
    }

    /// <summary>Pins <c>serials</c> to a fixed value.</summary>
    public AnyShipment WithSerials(IReadOnlyList<SerialNumber> value) {
        return new AnyShipment(_reference, new FixedValue<IReadOnlyList<SerialNumber>>(value), _shippedOn);
    }

    /// <summary>Draws <c>serials</c> from <c>AnyValidSerials()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithSerials(Func<AnyList<SerialNumber>, AnyList<SerialNumber>> configure) {
        return new AnyShipment(_reference, configure(AnyValidSerials()), _shippedOn);
    }

    /// <summary>Pins <c>shippedOn</c> to a fixed value.</summary>
    public AnyShipment WithShippedOn(DateOnly value) {
        return new AnyShipment(_reference, _serials, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>shippedOn</c> from <c>AnyValidShippedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithShippedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyShipment(_reference, _serials, configure(AnyValidShippedOn()));
    }

    /// <summary>Produces one arbitrary <see cref="Shipment" />.</summary>
    public Shipment Generate() {
        return new Shipment(_reference.Generate(),
                            _serials.Generate(),
                            _shippedOn.Generate());
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
