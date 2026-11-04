// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Lot --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Lot" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLot : IAny<Lot> {

    private IAny<LotNumber>          _number;
    private IAny<MaterialCode>       _material;
    private IAny<Quantity>           _initialQuantity;

    private AnyLotNumber    _numberRecipe;
    private AnyMaterialCode _materialRecipe;
    private AnyQuantity     _initialQuantityRecipe;
    private AnyDateOnly     _manufacturedOnRecipe;
    private AnyDateOnly     _expiresOnRecipe;

    private DateOnly? _manufacturedOnPin;
    private DateOnly? _expiresOnPin;

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
        return new AnyQuantity().WithAmount(amount => amount.GreaterThan(0m));
    }

    private static AnyDateOnly AnyValidManufacturedOn() {
        // Both ends of a lot's life come from one window of ten years: its widest span, 3651 days, stays under the 3653 days a shelf life may reach.
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31));
    }

    private static AnyDateOnly AnyValidExpiresOn() {
        // Both ends of a lot's life come from one window of ten years: its widest span, 3651 days, stays under the 3653 days a shelf life may reach.
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLot(AnyLotNumber    number,
                   AnyMaterialCode material,
                   AnyQuantity     initialQuantity,
                   AnyDateOnly     manufacturedOn,
                   AnyDateOnly     expiresOn) {
        _number                     = number;
        _numberRecipe               = number;
        _material                   = material;
        _materialRecipe             = material;
        _initialQuantity            = initialQuantity;
        _initialQuantityRecipe      = initialQuantity;
        _manufacturedOnRecipe       = manufacturedOn;
        _expiresOnRecipe            = expiresOn;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLot(AnyLot from) {
        _number                     = from._number;
        _numberRecipe               = from._numberRecipe;
        _material                   = from._material;
        _materialRecipe             = from._materialRecipe;
        _initialQuantity            = from._initialQuantity;
        _initialQuantityRecipe      = from._initialQuantityRecipe;
        _manufacturedOnRecipe       = from._manufacturedOnRecipe;
        _manufacturedOnPin          = from._manufacturedOnPin;
        _expiresOnRecipe            = from._expiresOnRecipe;
        _expiresOnPin               = from._expiresOnPin;
    }

    /// <summary>Draws <c>manufacturedOn</c> and <c>expiresOn</c>, <c>manufacturedOn</c> always before <c>expiresOn</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (DateOnly ManufacturedOn, DateOnly ExpiresOn) ManufacturedOnAndExpiresOn() {
        if (_manufacturedOnPin is { } manufacturedOn) { return (manufacturedOn, _expiresOnPin ?? _expiresOnRecipe.After(manufacturedOn).Generate()); }
        if (_expiresOnPin is { } expiresOn) { return (_manufacturedOnRecipe.Before(expiresOn).Generate(), expiresOn); }

        return Any.Pair(_manufacturedOnRecipe, _expiresOnRecipe).Having.Item1BeforeItem2().Generate();
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLot WithNumber(LotNumber value) {
        AnyLot copy = new AnyLot(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLotNumber, AnyLotNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLot WithNumber(Func<AnyLotNumber, AnyLotNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>material</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLot WithMaterial(MaterialCode value) {
        AnyLot copy = new AnyLot(this);

        copy._material = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>material</c> is drawn from — <c>AnyValidMaterial()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMaterialCode, AnyMaterialCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLot WithMaterial(Func<AnyMaterialCode, AnyMaterialCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._materialRecipe = configure(_materialRecipe);
        copy._material       = copy._materialRecipe;

        return copy;
    }

    /// <summary>Pins <c>initialQuantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLot WithInitialQuantity(Quantity value) {
        AnyLot copy = new AnyLot(this);

        copy._initialQuantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>initialQuantity</c> is drawn from — <c>AnyValidInitialQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyQuantity, AnyQuantity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLot WithInitialQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._initialQuantityRecipe = configure(_initialQuantityRecipe);
        copy._initialQuantity       = copy._initialQuantityRecipe;

        return copy;
    }

    /// <summary>Pins <c>manufacturedOn</c> to a fixed value, which bounds <c>expiresOn</c>; its recipe is kept for a later refinement.</summary>
    public AnyLot WithManufacturedOn(DateOnly value) {
        AnyLot copy = new AnyLot(this);

        copy._manufacturedOnPin          = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>manufacturedOn</c> is drawn from — <c>AnyValidManufacturedOn()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>expiresOn</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLot WithManufacturedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._manufacturedOnRecipe       = configure(_manufacturedOnRecipe);
        copy._manufacturedOnPin          = null;

        return copy;
    }

    /// <summary>Pins <c>expiresOn</c> to a fixed value, which bounds <c>manufacturedOn</c>; its recipe is kept for a later refinement.</summary>
    public AnyLot WithExpiresOn(DateOnly value) {
        AnyLot copy = new AnyLot(this);

        copy._expiresOnPin               = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>expiresOn</c> is drawn from — <c>AnyValidExpiresOn()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>manufacturedOn</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLot WithExpiresOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLot copy = new AnyLot(this);

        copy._expiresOnRecipe            = configure(_expiresOnRecipe);
        copy._expiresOnPin               = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Lot" />.</summary>
    public Lot Generate() {
        (DateOnly manufacturedOn, DateOnly expiresOn) = ManufacturedOnAndExpiresOn();

        return new Lot(_number.Generate(),
                       _material.Generate(),
                       _initialQuantity.Generate(),
                       manufacturedOn,
                       expiresOn);
    }

}
