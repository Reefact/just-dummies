// Scaffolded by `any` (JustDummies), then reworked by hand. The scaffold drew the two dates together
// through Any.OrderedPair, which holds `manufacturedOn < expiresOn` but not the 3653-day shelf-life
// limit, and refuses any pin that lies outside the window the other date is drawn from. Here a lot is
// drawn as a manufacture date and a shelf life, so both rules hold by construction and any date can
// be pinned. `any generate Lot --force` would lose this.

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Lot" /> values: manufactured on some day, expiring within the
///     shelf-life limit afterwards, quarantined and carrying a positive quantity.
/// </summary>
public sealed partial class AnyLot : IAny<Lot> {

    private IAny<LotNumber>    _number;
    private IAny<MaterialCode> _material;
    private IAny<Quantity>     _initialQuantity;
    private IAny<DateOnly>     _manufacturedOn;
    private IAny<int>          _shelfLifeDays;
    private DateOnly?          _expiresOnPin;
    private bool              _manufacturedOnPinned;

    private AnyLotNumber    _numberRecipe;
    private AnyMaterialCode _materialRecipe;
    private AnyQuantity     _initialQuantityRecipe;
    private AnyDateOnly     _manufacturedOnRecipe;
    private AnyInt32        _shelfLifeDaysRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLot() {
        _numberRecipe          = new AnyLotNumber();
        _materialRecipe        = new AnyMaterialCode();
        _initialQuantityRecipe = new AnyQuantity().WithAmount(amount => amount.Positive());
        _manufacturedOnRecipe  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
        _shelfLifeDaysRecipe   = Any.Int32().Between(1, Lot.MaximumShelfLifeDays);

        _number          = _numberRecipe;
        _material        = _materialRecipe;
        _initialQuantity = _initialQuantityRecipe;
        _manufacturedOn  = _manufacturedOnRecipe;
        _shelfLifeDays   = _shelfLifeDaysRecipe;
    }

    private AnyLot(AnyLot from) {
        _number                = from._number;
        _numberRecipe          = from._numberRecipe;
        _material              = from._material;
        _materialRecipe        = from._materialRecipe;
        _initialQuantity       = from._initialQuantity;
        _initialQuantityRecipe = from._initialQuantityRecipe;
        _manufacturedOn        = from._manufacturedOn;
        _manufacturedOnRecipe  = from._manufacturedOnRecipe;
        _shelfLifeDays         = from._shelfLifeDays;
        _shelfLifeDaysRecipe   = from._shelfLifeDaysRecipe;
        _expiresOnPin          = from._expiresOnPin;
        _manufacturedOnPinned  = from._manufacturedOnPinned;
    }

    public AnyLot WithNumber(LotNumber value) {
        AnyLot copy = new AnyLot(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    public AnyLot WithNumber(Func<AnyLotNumber, AnyLotNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    public AnyLot WithMaterial(MaterialCode value) {
        AnyLot copy = new AnyLot(this);

        copy._material = Any.Fixed(value);

        return copy;
    }

    public AnyLot WithMaterial(Func<AnyMaterialCode, AnyMaterialCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._materialRecipe = configure(_materialRecipe);
        copy._material       = copy._materialRecipe;

        return copy;
    }

    public AnyLot WithInitialQuantity(Quantity value) {
        AnyLot copy = new AnyLot(this);

        copy._initialQuantity = Any.Fixed(value);

        return copy;
    }

    public AnyLot WithInitialQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._initialQuantityRecipe = configure(_initialQuantityRecipe);
        copy._initialQuantity       = copy._initialQuantityRecipe;

        return copy;
    }

    /// <summary>Pins the manufacture date; the expiry date follows from the shelf life.</summary>
    public AnyLot WithManufacturedOn(DateOnly value) {
        AnyLot copy = new AnyLot(this);

        copy._manufacturedOn       = Any.Fixed(value);
        copy._manufacturedOnPinned = true;

        return copy;
    }

    public AnyLot WithManufacturedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._manufacturedOnRecipe = configure(_manufacturedOnRecipe);
        copy._manufacturedOn       = copy._manufacturedOnRecipe;
        copy._manufacturedOnPinned = false;

        return copy;
    }

    /// <summary>
    ///     Pins the expiry date, whatever the manufacture date is. With no pinned manufacture date, it is
    ///     drawn as the expiry date minus a shelf life, so the limit holds; with one, the pair is the
    ///     caller's, and the constructor judges it.
    /// </summary>
    public AnyLot WithExpiresOn(DateOnly value) {
        AnyLot copy = new AnyLot(this);

        copy._expiresOnPin = value;

        return copy;
    }

    /// <summary>Pins the shelf life, in days, between manufacture and expiry.</summary>
    public AnyLot WithShelfLifeDays(int value) {
        AnyLot copy = new AnyLot(this);

        copy._shelfLifeDays = Any.Fixed(value);

        return copy;
    }

    public AnyLot WithShelfLifeDays(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._shelfLifeDaysRecipe = configure(_shelfLifeDaysRecipe);
        copy._shelfLifeDays       = copy._shelfLifeDaysRecipe;

        return copy;
    }

    public Lot Generate() {
        DateOnly manufacturedOn;
        DateOnly expiresOn;
        if (_expiresOnPin is { } pinnedExpiry && !_manufacturedOnPinned) {
            expiresOn      = pinnedExpiry;
            manufacturedOn = expiresOn.AddDays(-_shelfLifeDays.Generate());
        } else if (_expiresOnPin is { } expiry) {
            manufacturedOn = _manufacturedOn.Generate();
            expiresOn      = expiry;
        } else {
            manufacturedOn = _manufacturedOn.Generate();
            expiresOn      = manufacturedOn.AddDays(_shelfLifeDays.Generate());
        }

        return new Lot(_number.Generate(),
                       _material.Generate(),
                       _initialQuantity.Generate(),
                       manufacturedOn,
                       expiresOn);
    }

}
