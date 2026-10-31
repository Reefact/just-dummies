// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Lot --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Lot" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLot : IAny<Lot> {

    private readonly IAny<LotNumber>    _number;
    private readonly IAny<MaterialCode> _material;
    private readonly IAny<Quantity>     _initialQuantity;
    private readonly IAny<DateOnly>     _manufacturedOn;
    private readonly IAny<DateOnly>     _expiresOn;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLot()
        : this(number:          AnyValidNumber(),
               material:        AnyValidMaterial(),
               initialQuantity: AnyValidInitialQuantity(),
               manufacturedOn:  AnyValidManufacturedOn(),
               expiresOn:       AnyValidExpiresOn()) { }

    private static AnyLotNumber AnyValidNumber() {
        return new AnyLotNumber();
    }

    private static AnyMaterialCode AnyValidMaterial() {
        return new AnyMaterialCode();
    }

    private static AnyQuantity AnyValidInitialQuantity() {
        // A lot is created with a positive quantity.
        return new AnyQuantity().WithAmount(a => a.Positive());
    }

    // Manufactured in 2024, expiring in 2026: expiry is after manufacture and within the
    // 3653-day shelf life by construction, and the lot is consumable throughout 2025.
    private static AnyDateOnly AnyValidManufacturedOn() {
        return Any.DateOnly().Between(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
    }

    private static AnyDateOnly AnyValidExpiresOn() {
        return Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
    }

    private AnyLot(IAny<LotNumber>    number,
                   IAny<MaterialCode> material,
                   IAny<Quantity>     initialQuantity,
                   IAny<DateOnly>     manufacturedOn,
                   IAny<DateOnly>     expiresOn) {
        _number          = number;
        _material        = material;
        _initialQuantity = initialQuantity;
        _manufacturedOn  = manufacturedOn;
        _expiresOn       = expiresOn;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyLot WithNumber(LotNumber value) {
        return new AnyLot(new FixedValue<LotNumber>(value), _material, _initialQuantity, _manufacturedOn, _expiresOn);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithNumber(Func<AnyLotNumber, AnyLotNumber> configure) {
        return new AnyLot(configure(AnyValidNumber()), _material, _initialQuantity, _manufacturedOn, _expiresOn);
    }

    /// <summary>Pins <c>material</c> to a fixed value.</summary>
    public AnyLot WithMaterial(MaterialCode value) {
        return new AnyLot(_number, new FixedValue<MaterialCode>(value), _initialQuantity, _manufacturedOn, _expiresOn);
    }

    /// <summary>Draws <c>material</c> from <c>AnyValidMaterial()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithMaterial(Func<AnyMaterialCode, AnyMaterialCode> configure) {
        return new AnyLot(_number, configure(AnyValidMaterial()), _initialQuantity, _manufacturedOn, _expiresOn);
    }

    /// <summary>Pins <c>initialQuantity</c> to a fixed value.</summary>
    public AnyLot WithInitialQuantity(Quantity value) {
        return new AnyLot(_number, _material, new FixedValue<Quantity>(value), _manufacturedOn, _expiresOn);
    }

    /// <summary>Draws <c>initialQuantity</c> from <c>AnyValidInitialQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithInitialQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        return new AnyLot(_number, _material, configure(AnyValidInitialQuantity()), _manufacturedOn, _expiresOn);
    }

    /// <summary>Pins <c>manufacturedOn</c> to a fixed value.</summary>
    public AnyLot WithManufacturedOn(DateOnly value) {
        return new AnyLot(_number, _material, _initialQuantity, new FixedValue<DateOnly>(value), _expiresOn);
    }

    /// <summary>Draws <c>manufacturedOn</c> from <c>AnyValidManufacturedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithManufacturedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyLot(_number, _material, _initialQuantity, configure(AnyValidManufacturedOn()), _expiresOn);
    }

    /// <summary>Pins <c>expiresOn</c> to a fixed value.</summary>
    public AnyLot WithExpiresOn(DateOnly value) {
        return new AnyLot(_number, _material, _initialQuantity, _manufacturedOn, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>expiresOn</c> from <c>AnyValidExpiresOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithExpiresOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyLot(_number, _material, _initialQuantity, _manufacturedOn, configure(AnyValidExpiresOn()));
    }

    /// <summary>Produces one arbitrary <see cref="Lot" />.</summary>
    public Lot Generate() {
        return new Lot(_number.Generate(),
                       _material.Generate(),
                       _initialQuantity.Generate(),
                       _manufacturedOn.Generate(),
                       _expiresOn.Generate());
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
