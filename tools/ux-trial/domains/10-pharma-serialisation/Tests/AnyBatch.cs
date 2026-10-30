// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Batch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Batch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBatch : IAny<Batch> {

    private readonly IAny<Gtin>               _gtin;
    private readonly IAny<BatchNumber>        _number;
    private readonly IAny<DateOnly>           _manufacturedOn;
    private readonly IAny<DateOnly>           _expiresOn;
    private readonly IAny<int>                _plannedQuantity;
    private readonly IAny<ReimbursementPrice> _price;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBatch()
        : this(gtin:            AnyValidGtin(),
               number:          AnyValidNumber(),
               manufacturedOn:  AnyValidManufacturedOn(),
               expiresOn:       AnyValidExpiresOn(),
               plannedQuantity: AnyValidPlannedQuantity(),
               price:           AnyValidPrice()) { }

    private static AnyGtin AnyValidGtin() {
        return new AnyGtin();
    }

    private static AnyBatchNumber AnyValidNumber() {
        return new AnyBatchNumber();
    }

    private static AnyDateOnly AnyValidManufacturedOn() {
        // Cross-field guard expiresOn > manufacturedOn: manufacture is drawn strictly before the earliest expiry below.
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));
    }

    private static AnyDateOnly AnyValidExpiresOn() {
        // Cross-field guard expiresOn > manufacturedOn: expiry is drawn strictly after the latest manufacture above.
        return Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2030, 12, 31));
    }

    private static AnyInt32 AnyValidPlannedQuantity() {
        return Any.Int32().Positive().LessThanOrEqualTo(500000);
    }

    private static AnyReimbursementPrice AnyValidPrice() {
        return new AnyReimbursementPrice();
    }

    private AnyBatch(IAny<Gtin>               gtin,
                     IAny<BatchNumber>        number,
                     IAny<DateOnly>           manufacturedOn,
                     IAny<DateOnly>           expiresOn,
                     IAny<int>                plannedQuantity,
                     IAny<ReimbursementPrice> price) {
        _gtin            = gtin;
        _number          = number;
        _manufacturedOn  = manufacturedOn;
        _expiresOn       = expiresOn;
        _plannedQuantity = plannedQuantity;
        _price           = price;
    }

    /// <summary>Pins <c>gtin</c> to a fixed value.</summary>
    public AnyBatch WithGtin(Gtin value) {
        return new AnyBatch(new FixedValue<Gtin>(value), _number, _manufacturedOn, _expiresOn, _plannedQuantity, _price);
    }

    /// <summary>Draws <c>gtin</c> from <c>AnyValidGtin()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatch WithGtin(Func<AnyGtin, AnyGtin> configure) {
        return new AnyBatch(configure(AnyValidGtin()), _number, _manufacturedOn, _expiresOn, _plannedQuantity, _price);
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyBatch WithNumber(BatchNumber value) {
        return new AnyBatch(_gtin, new FixedValue<BatchNumber>(value), _manufacturedOn, _expiresOn, _plannedQuantity, _price);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatch WithNumber(Func<AnyBatchNumber, AnyBatchNumber> configure) {
        return new AnyBatch(_gtin, configure(AnyValidNumber()), _manufacturedOn, _expiresOn, _plannedQuantity, _price);
    }

    /// <summary>Pins <c>manufacturedOn</c> to a fixed value.</summary>
    public AnyBatch WithManufacturedOn(DateOnly value) {
        return new AnyBatch(_gtin, _number, new FixedValue<DateOnly>(value), _expiresOn, _plannedQuantity, _price);
    }

    /// <summary>Draws <c>manufacturedOn</c> from <c>AnyValidManufacturedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatch WithManufacturedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyBatch(_gtin, _number, configure(AnyValidManufacturedOn()), _expiresOn, _plannedQuantity, _price);
    }

    /// <summary>Pins <c>expiresOn</c> to a fixed value.</summary>
    public AnyBatch WithExpiresOn(DateOnly value) {
        return new AnyBatch(_gtin, _number, _manufacturedOn, new FixedValue<DateOnly>(value), _plannedQuantity, _price);
    }

    /// <summary>Draws <c>expiresOn</c> from <c>AnyValidExpiresOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatch WithExpiresOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyBatch(_gtin, _number, _manufacturedOn, configure(AnyValidExpiresOn()), _plannedQuantity, _price);
    }

    /// <summary>Pins <c>plannedQuantity</c> to a fixed value.</summary>
    public AnyBatch WithPlannedQuantity(int value) {
        return new AnyBatch(_gtin, _number, _manufacturedOn, _expiresOn, new FixedValue<int>(value), _price);
    }

    /// <summary>Draws <c>plannedQuantity</c> from <c>AnyValidPlannedQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatch WithPlannedQuantity(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBatch(_gtin, _number, _manufacturedOn, _expiresOn, configure(AnyValidPlannedQuantity()), _price);
    }

    /// <summary>Pins <c>price</c> to a fixed value.</summary>
    public AnyBatch WithPrice(ReimbursementPrice value) {
        return new AnyBatch(_gtin, _number, _manufacturedOn, _expiresOn, _plannedQuantity, new FixedValue<ReimbursementPrice>(value));
    }

    /// <summary>Draws <c>price</c> from <c>AnyValidPrice()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatch WithPrice(Func<AnyReimbursementPrice, AnyReimbursementPrice> configure) {
        return new AnyBatch(_gtin, _number, _manufacturedOn, _expiresOn, _plannedQuantity, configure(AnyValidPrice()));
    }

    /// <summary>Produces one arbitrary <see cref="Batch" />.</summary>
    public Batch Generate() {
        return new Batch(_gtin.Generate(),
                         _number.Generate(),
                         _manufacturedOn.Generate(),
                         _expiresOn.Generate(),
                         _plannedQuantity.Generate(),
                         _price.Generate());
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
