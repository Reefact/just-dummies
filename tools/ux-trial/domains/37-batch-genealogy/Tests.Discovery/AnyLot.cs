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
    private readonly IAny<int>          _shelfLifeDays;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLot()
        : this(number:          AnyValidNumber(),
               material:        AnyValidMaterial(),
               initialQuantity: AnyValidInitialQuantity(),
               manufacturedOn:  AnyValidManufacturedOn(),
               shelfLifeDays:   AnyValidShelfLifeDays()) { }

    private static AnyLotNumber AnyValidNumber() {
        return new AnyLotNumber();
    }

    private static AnyMaterialCode AnyValidMaterial() {
        return new AnyMaterialCode();
    }

    private static AnyQuantity AnyValidInitialQuantity() {
        // A lot is created with a positive quantity.
        return new AnyQuantity().WithAmount(amount => amount.Positive());
    }

    private static AnyDateOnly AnyValidManufacturedOn() {
        // Bounded so that manufacture plus the longest shelf life stays a representable date.
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2099, 12, 31));
    }

    // expiresOn > manufacturedOn ties two parameters: the expiry is derived in Generate()
    // from a shelf life of 1 to Lot.MaximumShelfLifeDays days, so the rule holds by construction.
    private static AnyInt32 AnyValidShelfLifeDays() {
        return Any.Int32().Between(1, Lot.MaximumShelfLifeDays);
    }

    private AnyLot(IAny<LotNumber>    number,
                   IAny<MaterialCode> material,
                   IAny<Quantity>     initialQuantity,
                   IAny<DateOnly>     manufacturedOn,
                   IAny<int>          shelfLifeDays) {
        _number          = number;
        _material        = material;
        _initialQuantity = initialQuantity;
        _manufacturedOn  = manufacturedOn;
        _shelfLifeDays   = shelfLifeDays;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyLot WithNumber(LotNumber value) {
        return new AnyLot(new FixedValue<LotNumber>(value), _material, _initialQuantity, _manufacturedOn, _shelfLifeDays);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithNumber(Func<AnyLotNumber, AnyLotNumber> configure) {
        return new AnyLot(configure(AnyValidNumber()), _material, _initialQuantity, _manufacturedOn, _shelfLifeDays);
    }

    /// <summary>Pins <c>material</c> to a fixed value.</summary>
    public AnyLot WithMaterial(MaterialCode value) {
        return new AnyLot(_number, new FixedValue<MaterialCode>(value), _initialQuantity, _manufacturedOn, _shelfLifeDays);
    }

    /// <summary>Draws <c>material</c> from <c>AnyValidMaterial()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithMaterial(Func<AnyMaterialCode, AnyMaterialCode> configure) {
        return new AnyLot(_number, configure(AnyValidMaterial()), _initialQuantity, _manufacturedOn, _shelfLifeDays);
    }

    /// <summary>Pins <c>initialQuantity</c> to a fixed value.</summary>
    public AnyLot WithInitialQuantity(Quantity value) {
        return new AnyLot(_number, _material, new FixedValue<Quantity>(value), _manufacturedOn, _shelfLifeDays);
    }

    /// <summary>Draws <c>initialQuantity</c> from <c>AnyValidInitialQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithInitialQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        return new AnyLot(_number, _material, configure(AnyValidInitialQuantity()), _manufacturedOn, _shelfLifeDays);
    }

    /// <summary>Pins <c>manufacturedOn</c> to a fixed value.</summary>
    public AnyLot WithManufacturedOn(DateOnly value) {
        return new AnyLot(_number, _material, _initialQuantity, new FixedValue<DateOnly>(value), _shelfLifeDays);
    }

    /// <summary>Draws <c>manufacturedOn</c> from <c>AnyValidManufacturedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithManufacturedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyLot(_number, _material, _initialQuantity, configure(AnyValidManufacturedOn()), _shelfLifeDays);
    }

    /// <summary>Pins the shelf life, in days: the lot expires that many days after manufacture.</summary>
    public AnyLot WithShelfLifeDays(int value) {
        return new AnyLot(_number, _material, _initialQuantity, _manufacturedOn, new FixedValue<int>(value));
    }

    /// <summary>Draws the shelf life from <c>AnyValidShelfLifeDays()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLot WithShelfLifeDays(Func<AnyInt32, AnyInt32> configure) {
        return new AnyLot(_number, _material, _initialQuantity, _manufacturedOn, configure(AnyValidShelfLifeDays()));
    }

    /// <summary>Produces one arbitrary <see cref="Lot" />.</summary>
    public Lot Generate() {
        DateOnly manufacturedOn = _manufacturedOn.Generate();

        return new Lot(_number.Generate(),
                       _material.Generate(),
                       _initialQuantity.Generate(),
                       manufacturedOn,
                       manufacturedOn.AddDays(_shelfLifeDays.Generate()));
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
