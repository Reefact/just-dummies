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

    private IAny<Gtin>               _gtin;
    private IAny<BatchNumber>        _number;
    private AnyOrderedPair<DateOnly> _manufacturedOnAndExpiresOn;
    private IAny<int>                _plannedQuantity;
    private IAny<ReimbursementPrice> _price;

    private AnyGtin               _gtinRecipe;
    private AnyBatchNumber        _numberRecipe;
    private AnyDateOnly           _manufacturedOnRecipe;
    private AnyDateOnly           _expiresOnRecipe;
    private AnyInt32              _plannedQuantityRecipe;
    private AnyReimbursementPrice _priceRecipe;

    private DateOnly? _manufacturedOnPin;
    private DateOnly? _expiresOnPin;

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
        return Any.DateOnly();
    }

    private static AnyDateOnly AnyValidExpiresOn() {
        return Any.DateOnly();
    }

    private static AnyInt32 AnyValidPlannedQuantity() {
        return Any.Int32().Positive().LessThanOrEqualTo(500000);
    }

    private static AnyReimbursementPrice AnyValidPrice() {
        return new AnyReimbursementPrice();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBatch(AnyGtin               gtin,
                     AnyBatchNumber        number,
                     AnyDateOnly           manufacturedOn,
                     AnyDateOnly           expiresOn,
                     AnyInt32              plannedQuantity,
                     AnyReimbursementPrice price) {
        _gtin                       = gtin;
        _gtinRecipe                 = gtin;
        _number                     = number;
        _numberRecipe               = number;
        _manufacturedOnRecipe       = manufacturedOn;
        _expiresOnRecipe            = expiresOn;
        _plannedQuantity            = plannedQuantity;
        _plannedQuantityRecipe      = plannedQuantity;
        _price                      = price;
        _priceRecipe                = price;
        _manufacturedOnAndExpiresOn = ManufacturedOnAndExpiresOn();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBatch(AnyBatch from) {
        _gtin                       = from._gtin;
        _gtinRecipe                 = from._gtinRecipe;
        _number                     = from._number;
        _numberRecipe               = from._numberRecipe;
        _manufacturedOnAndExpiresOn = from._manufacturedOnAndExpiresOn;
        _manufacturedOnRecipe       = from._manufacturedOnRecipe;
        _manufacturedOnPin          = from._manufacturedOnPin;
        _expiresOnRecipe            = from._expiresOnRecipe;
        _expiresOnPin               = from._expiresOnPin;
        _plannedQuantity            = from._plannedQuantity;
        _plannedQuantityRecipe      = from._plannedQuantityRecipe;
        _price                      = from._price;
        _priceRecipe                = from._priceRecipe;
    }

    /// <summary>Draws <c>manufacturedOn</c> and <c>expiresOn</c> together, <c>manufacturedOn</c> always before <c>expiresOn</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<DateOnly> ManufacturedOnAndExpiresOn() {
        AnyOrderedPair<DateOnly> pair = Any.OrderedPair(_manufacturedOnRecipe, _expiresOnRecipe, strict: true);

        if (_manufacturedOnPin is { } manufacturedOn) { pair = pair.WithFirst(manufacturedOn); }
        if (_expiresOnPin is { } expiresOn) { pair = pair.WithSecond(expiresOn); }

        return pair;
    }

    /// <summary>Pins <c>gtin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBatch WithGtin(Gtin value) {
        AnyBatch copy = new AnyBatch(this);

        copy._gtin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gtin</c> is drawn from — <c>AnyValidGtin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGtin, AnyGtin&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBatch WithGtin(Func<AnyGtin, AnyGtin> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBatch copy = new AnyBatch(this);

        copy._gtinRecipe = configure(_gtinRecipe);
        copy._gtin       = copy._gtinRecipe;

        return copy;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBatch WithNumber(BatchNumber value) {
        AnyBatch copy = new AnyBatch(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBatchNumber, AnyBatchNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBatch WithNumber(Func<AnyBatchNumber, AnyBatchNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBatch copy = new AnyBatch(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>manufacturedOn</c> to a fixed value, which bounds <c>expiresOn</c>; its recipe is kept for a later refinement.</summary>
    public AnyBatch WithManufacturedOn(DateOnly value) {
        AnyBatch copy = new AnyBatch(this);

        copy._manufacturedOnPin          = value;
        copy._manufacturedOnAndExpiresOn = copy.ManufacturedOnAndExpiresOn();

        return copy;
    }

    /// <summary>Refines the recipe <c>manufacturedOn</c> is drawn from — <c>AnyValidManufacturedOn()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>expiresOn</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBatch WithManufacturedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBatch copy = new AnyBatch(this);

        copy._manufacturedOnRecipe       = configure(_manufacturedOnRecipe);
        copy._manufacturedOnPin          = null;
        copy._manufacturedOnAndExpiresOn = copy.ManufacturedOnAndExpiresOn();

        return copy;
    }

    /// <summary>Pins <c>expiresOn</c> to a fixed value, which bounds <c>manufacturedOn</c>; its recipe is kept for a later refinement.</summary>
    public AnyBatch WithExpiresOn(DateOnly value) {
        AnyBatch copy = new AnyBatch(this);

        copy._expiresOnPin               = value;
        copy._manufacturedOnAndExpiresOn = copy.ManufacturedOnAndExpiresOn();

        return copy;
    }

    /// <summary>Refines the recipe <c>expiresOn</c> is drawn from — <c>AnyValidExpiresOn()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>manufacturedOn</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBatch WithExpiresOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBatch copy = new AnyBatch(this);

        copy._expiresOnRecipe            = configure(_expiresOnRecipe);
        copy._expiresOnPin               = null;
        copy._manufacturedOnAndExpiresOn = copy.ManufacturedOnAndExpiresOn();

        return copy;
    }

    /// <summary>Pins <c>plannedQuantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBatch WithPlannedQuantity(int value) {
        AnyBatch copy = new AnyBatch(this);

        copy._plannedQuantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>plannedQuantity</c> is drawn from — <c>AnyValidPlannedQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBatch WithPlannedQuantity(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBatch copy = new AnyBatch(this);

        copy._plannedQuantityRecipe = configure(_plannedQuantityRecipe);
        copy._plannedQuantity       = copy._plannedQuantityRecipe;

        return copy;
    }

    /// <summary>Pins <c>price</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBatch WithPrice(ReimbursementPrice value) {
        AnyBatch copy = new AnyBatch(this);

        copy._price = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>price</c> is drawn from — <c>AnyValidPrice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyReimbursementPrice, AnyReimbursementPrice&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBatch WithPrice(Func<AnyReimbursementPrice, AnyReimbursementPrice> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBatch copy = new AnyBatch(this);

        copy._priceRecipe = configure(_priceRecipe);
        copy._price       = copy._priceRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Batch" />.</summary>
    public Batch Generate() {
        (DateOnly manufacturedOn, DateOnly expiresOn) = _manufacturedOnAndExpiresOn.Generate();

        return new Batch(_gtin.Generate(),
                         _number.Generate(),
                         manufacturedOn,
                         expiresOn,
                         _plannedQuantity.Generate(),
                         _price.Generate());
    }

}
