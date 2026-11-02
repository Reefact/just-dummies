// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Transfer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Transfer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTransfer : IAny<Transfer> {

    private IAny<CadastralReference> _parcel;
    private IAny<decimal>            _price;
    private AnyOrderedPair<DateOnly> _signedOnAndRegisteredOn;

    private AnyCadastralReference _parcelRecipe;
    private AnyDecimal            _priceRecipe;
    private AnyDateOnly           _signedOnRecipe;
    private AnyDateOnly           _registeredOnRecipe;

    private DateOnly? _signedOnPin;
    private DateOnly? _registeredOnPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
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

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTransfer(AnyCadastralReference parcel,
                        AnyDecimal            price,
                        AnyDateOnly           signedOn,
                        AnyDateOnly           registeredOn) {
        _parcel             = parcel;
        _parcelRecipe       = parcel;
        _price              = price;
        _priceRecipe        = price;
        _signedOnRecipe     = signedOn;
        _registeredOnRecipe = registeredOn;
        _signedOnAndRegisteredOn = SignedOnAndRegisteredOn();
    }

    // The tie: a deed is never registered before it is signed. Equal days are allowed, hence not strict.
    private AnyOrderedPair<DateOnly> SignedOnAndRegisteredOn() {
        AnyOrderedPair<DateOnly> pair = Any.OrderedPair(_signedOnRecipe, _registeredOnRecipe);

        if (_signedOnPin is { } signedOn) { pair = pair.WithFirst(signedOn); }
        if (_registeredOnPin is { } registeredOn) { pair = pair.WithSecond(registeredOn); }

        return pair;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTransfer(AnyTransfer from) {
        _parcel             = from._parcel;
        _parcelRecipe       = from._parcelRecipe;
        _price              = from._price;
        _priceRecipe        = from._priceRecipe;
        _signedOnAndRegisteredOn = from._signedOnAndRegisteredOn;
        _signedOnRecipe     = from._signedOnRecipe;
        _signedOnPin        = from._signedOnPin;
        _registeredOnRecipe = from._registeredOnRecipe;
        _registeredOnPin    = from._registeredOnPin;
    }

    /// <summary>Pins <c>parcel</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransfer WithParcel(CadastralReference value) {
        AnyTransfer copy = new AnyTransfer(this);

        copy._parcel = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>parcel</c> is drawn from — <c>AnyValidParcel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCadastralReference, AnyCadastralReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransfer WithParcel(Func<AnyCadastralReference, AnyCadastralReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransfer copy = new AnyTransfer(this);

        copy._parcelRecipe = configure(_parcelRecipe);
        copy._parcel       = copy._parcelRecipe;

        return copy;
    }

    /// <summary>Pins <c>price</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransfer WithPrice(decimal value) {
        AnyTransfer copy = new AnyTransfer(this);

        copy._price = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>price</c> is drawn from — <c>AnyValidPrice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransfer WithPrice(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransfer copy = new AnyTransfer(this);

        copy._priceRecipe = configure(_priceRecipe);
        copy._price       = copy._priceRecipe;

        return copy;
    }

    /// <summary>Pins <c>signedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransfer WithSignedOn(DateOnly value) {
        AnyTransfer copy = new AnyTransfer(this);

        copy._signedOnPin = value;
        copy._signedOnAndRegisteredOn = copy.SignedOnAndRegisteredOn();

        return copy;
    }

    /// <summary>Refines the recipe <c>signedOn</c> is drawn from — <c>AnyValidSignedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransfer WithSignedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransfer copy = new AnyTransfer(this);

        copy._signedOnRecipe = configure(_signedOnRecipe);
        copy._signedOnPin    = null;
        copy._signedOnAndRegisteredOn = copy.SignedOnAndRegisteredOn();

        return copy;
    }

    /// <summary>Pins <c>registeredOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransfer WithRegisteredOn(DateOnly value) {
        AnyTransfer copy = new AnyTransfer(this);

        copy._registeredOnPin = value;
        copy._signedOnAndRegisteredOn = copy.SignedOnAndRegisteredOn();

        return copy;
    }

    /// <summary>Refines the recipe <c>registeredOn</c> is drawn from — <c>AnyValidRegisteredOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransfer WithRegisteredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransfer copy = new AnyTransfer(this);

        copy._registeredOnRecipe = configure(_registeredOnRecipe);
        copy._registeredOnPin = null;
        copy._signedOnAndRegisteredOn = copy.SignedOnAndRegisteredOn();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Transfer" />.</summary>
    public Transfer Generate() {
        (DateOnly signedOn, DateOnly registeredOn) = _signedOnAndRegisteredOn.Generate();

        return new Transfer(_parcel.Generate(),
                            _price.Generate(),
                            signedOn,
                            registeredOn);
    }

}
