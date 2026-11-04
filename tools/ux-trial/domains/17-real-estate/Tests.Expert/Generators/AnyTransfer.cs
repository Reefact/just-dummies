// Scaffolded by `any` (JustDummies), then completed by hand: `ThrowIfLessThan(registeredOn, signedOn)` is a
// rule between two parameters that the scaffolder does not pair, so the two dates are drawn as an ordered pair.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>A generator of arbitrary <see cref="Transfer" /> values.</summary>
public sealed partial class AnyTransfer : IAny<Transfer> {

    private IAny<CadastralReference> _parcel;
    private IAny<decimal>            _price;

    private AnyCadastralReference _parcelRecipe;
    private AnyDecimal            _priceRecipe;
    private AnyDateOnly           _signedOnRecipe;
    private AnyDateOnly           _registeredOnRecipe;

    private DateOnly? _signedOnPin;
    private DateOnly? _registeredOnPin;

    public AnyTransfer()
        : this(parcel:       AnyValidParcel(),
               price:        AnyValidPrice(),
               signedOn:     AnyValidSignedOn(),
               registeredOn: AnyValidRegisteredOn()) { }

    private static AnyCadastralReference AnyValidParcel() {
        return new AnyCadastralReference();
    }

    private static AnyDecimal AnyValidPrice() {
        return Any.Decimal().Positive().LessThanOrEqualTo(1000000000m);
    }

    private static AnyDateOnly AnyValidSignedOn() {
        return Any.DateOnly();
    }

    private static AnyDateOnly AnyValidRegisteredOn() {
        return Any.DateOnly();
    }

    private AnyTransfer(AnyCadastralReference parcel,
                        AnyDecimal            price,
                        AnyDateOnly           signedOn,
                        AnyDateOnly           registeredOn) {
        _parcel                  = parcel;
        _parcelRecipe            = parcel;
        _price                   = price;
        _priceRecipe             = price;
        _signedOnRecipe          = signedOn;
        _registeredOnRecipe      = registeredOn;
    }

    private AnyTransfer(AnyTransfer from) {
        _parcel                  = from._parcel;
        _parcelRecipe            = from._parcelRecipe;
        _price                   = from._price;
        _priceRecipe             = from._priceRecipe;
        _signedOnRecipe          = from._signedOnRecipe;
        _signedOnPin             = from._signedOnPin;
        _registeredOnRecipe      = from._registeredOnRecipe;
        _registeredOnPin         = from._registeredOnPin;
    }

    /// <summary>Draws <c>signedOn</c> and <c>registeredOn</c>, <c>signedOn</c> never after <c>registeredOn</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (DateOnly SignedOn, DateOnly RegisteredOn) SignedOnAndRegisteredOn() {
        if (_signedOnPin is { } signedOn) { return (signedOn, _registeredOnPin ?? _registeredOnRecipe.AfterOrEqualTo(signedOn).Generate()); }
        if (_registeredOnPin is { } registeredOn) { return (_signedOnRecipe.BeforeOrEqualTo(registeredOn).Generate(), registeredOn); }

        return Any.Pair(_signedOnRecipe, _registeredOnRecipe).Having.Item1BeforeOrEqualToItem2().Generate();
    }

    public AnyTransfer WithParcel(CadastralReference value) {
        AnyTransfer copy = new AnyTransfer(this);

        copy._parcel = Any.Fixed(value);

        return copy;
    }

    public AnyTransfer WithParcel(Func<AnyCadastralReference, AnyCadastralReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransfer copy = new AnyTransfer(this);

        copy._parcelRecipe = configure(_parcelRecipe);
        copy._parcel       = copy._parcelRecipe;

        return copy;
    }

    public AnyTransfer WithPrice(decimal value) {
        AnyTransfer copy = new AnyTransfer(this);

        copy._price = Any.Fixed(value);

        return copy;
    }

    public AnyTransfer WithPrice(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransfer copy = new AnyTransfer(this);

        copy._priceRecipe = configure(_priceRecipe);
        copy._price       = copy._priceRecipe;

        return copy;
    }

    public AnyTransfer WithSignedOn(DateOnly value) {
        AnyTransfer copy = new AnyTransfer(this);

        copy._signedOnPin             = value;

        return copy;
    }

    public AnyTransfer WithSignedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransfer copy = new AnyTransfer(this);

        copy._signedOnRecipe          = configure(_signedOnRecipe);
        copy._signedOnPin             = null;

        return copy;
    }

    public AnyTransfer WithRegisteredOn(DateOnly value) {
        AnyTransfer copy = new AnyTransfer(this);

        copy._registeredOnPin         = value;

        return copy;
    }

    public AnyTransfer WithRegisteredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransfer copy = new AnyTransfer(this);

        copy._registeredOnRecipe      = configure(_registeredOnRecipe);
        copy._registeredOnPin         = null;

        return copy;
    }

    public Transfer Generate() {
        (DateOnly signedOn, DateOnly registeredOn) = SignedOnAndRegisteredOn();

        return new Transfer(_parcel.Generate(),
                            _price.Generate(),
                            signedOn,
                            registeredOn);
    }

}
