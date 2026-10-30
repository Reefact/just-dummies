// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SerialisedPack --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SerialisedPack" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySerialisedPack : IAny<SerialisedPack> {

    private readonly IAny<Gtin>         _gtin;
    private readonly IAny<BatchNumber>  _batch;
    private readonly IAny<DateOnly>     _expiresOn;
    private readonly IAny<SerialNumber> _serial;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySerialisedPack()
        : this(gtin:      AnyValidGtin(),
               batch:     AnyValidBatch(),
               expiresOn: AnyValidExpiresOn(),
               serial:    AnyValidSerial()) { }

    private static AnyGtin AnyValidGtin() {
        return new AnyGtin();
    }

    private static AnyBatchNumber AnyValidBatch() {
        return new AnyBatchNumber();
    }

    private static AnyDateOnly AnyValidExpiresOn() {
        return Any.DateOnly();
    }

    private static AnySerialNumber AnyValidSerial() {
        return new AnySerialNumber();
    }

    private AnySerialisedPack(IAny<Gtin>         gtin,
                              IAny<BatchNumber>  batch,
                              IAny<DateOnly>     expiresOn,
                              IAny<SerialNumber> serial) {
        _gtin      = gtin;
        _batch     = batch;
        _expiresOn = expiresOn;
        _serial    = serial;
    }

    /// <summary>Pins <c>gtin</c> to a fixed value.</summary>
    public AnySerialisedPack WithGtin(Gtin value) {
        return new AnySerialisedPack(new FixedValue<Gtin>(value), _batch, _expiresOn, _serial);
    }

    /// <summary>Draws <c>gtin</c> from <c>AnyValidGtin()</c>, refined by <paramref name="configure" />.</summary>
    public AnySerialisedPack WithGtin(Func<AnyGtin, AnyGtin> configure) {
        return new AnySerialisedPack(configure(AnyValidGtin()), _batch, _expiresOn, _serial);
    }

    /// <summary>Pins <c>batch</c> to a fixed value.</summary>
    public AnySerialisedPack WithBatch(BatchNumber value) {
        return new AnySerialisedPack(_gtin, new FixedValue<BatchNumber>(value), _expiresOn, _serial);
    }

    /// <summary>Draws <c>batch</c> from <c>AnyValidBatch()</c>, refined by <paramref name="configure" />.</summary>
    public AnySerialisedPack WithBatch(Func<AnyBatchNumber, AnyBatchNumber> configure) {
        return new AnySerialisedPack(_gtin, configure(AnyValidBatch()), _expiresOn, _serial);
    }

    /// <summary>Pins <c>expiresOn</c> to a fixed value.</summary>
    public AnySerialisedPack WithExpiresOn(DateOnly value) {
        return new AnySerialisedPack(_gtin, _batch, new FixedValue<DateOnly>(value), _serial);
    }

    /// <summary>Draws <c>expiresOn</c> from <c>AnyValidExpiresOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnySerialisedPack WithExpiresOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnySerialisedPack(_gtin, _batch, configure(AnyValidExpiresOn()), _serial);
    }

    /// <summary>Pins <c>serial</c> to a fixed value.</summary>
    public AnySerialisedPack WithSerial(SerialNumber value) {
        return new AnySerialisedPack(_gtin, _batch, _expiresOn, new FixedValue<SerialNumber>(value));
    }

    /// <summary>Draws <c>serial</c> from <c>AnyValidSerial()</c>, refined by <paramref name="configure" />.</summary>
    public AnySerialisedPack WithSerial(Func<AnySerialNumber, AnySerialNumber> configure) {
        return new AnySerialisedPack(_gtin, _batch, _expiresOn, configure(AnyValidSerial()));
    }

    /// <summary>Produces one arbitrary <see cref="SerialisedPack" />.</summary>
    public SerialisedPack Generate() {
        return new SerialisedPack(_gtin.Generate(),
                                  _batch.Generate(),
                                  _expiresOn.Generate(),
                                  _serial.Generate());
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
