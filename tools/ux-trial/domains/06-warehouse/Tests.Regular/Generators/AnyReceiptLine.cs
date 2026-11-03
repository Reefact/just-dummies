// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReceiptLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReceiptLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReceiptLine : IAny<ReceiptLine> {

    private IAny<Ean13>     _gtin;
    private IAny<Quantity>  _expected;
    private IAny<Quantity>  _received;
    private IAny<LotNumber> _lot;
    private IAny<DateOnly>  _bestBefore;
    private IAny<decimal>   _unitWeightKg;

    private AnyEan13     _gtinRecipe;
    private AnyQuantity  _expectedRecipe;
    private AnyQuantity  _receivedRecipe;
    private AnyLotNumber _lotRecipe;
    private AnyDateOnly  _bestBeforeRecipe;
    private AnyDecimal   _unitWeightKgRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReceiptLine()
        : this(gtin:         AnyValidGtin(),
               expected:     AnyValidExpected(),
               received:     AnyValidReceived(),
               lot:          AnyValidLot(),
               bestBefore:   AnyValidBestBefore(),
               unitWeightKg: AnyValidUnitWeightKg()) { }

    private static AnyEan13 AnyValidGtin() {
        return new AnyEan13();
    }

    private static AnyQuantity AnyValidExpected() {
        return new AnyQuantity();
    }

    private static AnyQuantity AnyValidReceived() {
        return new AnyQuantity();
    }

    private static AnyLotNumber AnyValidLot() {
        return new AnyLotNumber();
    }

    private static AnyDateOnly AnyValidBestBefore() {
        return Any.DateOnly();
    }

    private static AnyDecimal AnyValidUnitWeightKg() {
        // The constructor accepts any positive weight, but WeightKg overflows a decimal past ~7.9e24 kg per unit (units can reach 10 000):
        // a unit of a real trade item stays under a tonne.
        return Any.Decimal().Positive().LessThanOrEqualTo(1_000m);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReceiptLine(AnyEan13     gtin,
                           AnyQuantity  expected,
                           AnyQuantity  received,
                           AnyLotNumber lot,
                           AnyDateOnly  bestBefore,
                           AnyDecimal   unitWeightKg) {
        _gtin               = gtin;
        _gtinRecipe         = gtin;
        _expected           = expected;
        _expectedRecipe     = expected;
        _received           = received;
        _receivedRecipe     = received;
        _lot                = lot;
        _lotRecipe          = lot;
        _bestBefore         = bestBefore;
        _bestBeforeRecipe   = bestBefore;
        _unitWeightKg       = unitWeightKg;
        _unitWeightKgRecipe = unitWeightKg;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReceiptLine(AnyReceiptLine from) {
        _gtin               = from._gtin;
        _gtinRecipe         = from._gtinRecipe;
        _expected           = from._expected;
        _expectedRecipe     = from._expectedRecipe;
        _received           = from._received;
        _receivedRecipe     = from._receivedRecipe;
        _lot                = from._lot;
        _lotRecipe          = from._lotRecipe;
        _bestBefore         = from._bestBefore;
        _bestBeforeRecipe   = from._bestBeforeRecipe;
        _unitWeightKg       = from._unitWeightKg;
        _unitWeightKgRecipe = from._unitWeightKgRecipe;
    }

    /// <summary>Pins <c>gtin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceiptLine WithGtin(Ean13 value) {
        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._gtin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gtin</c> is drawn from — <c>AnyValidGtin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEan13, AnyEan13&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceiptLine WithGtin(Func<AnyEan13, AnyEan13> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._gtinRecipe = configure(_gtinRecipe);
        copy._gtin       = copy._gtinRecipe;

        return copy;
    }

    /// <summary>Pins <c>expected</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceiptLine WithExpected(Quantity value) {
        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._expected = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>expected</c> is drawn from — <c>AnyValidExpected()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyQuantity, AnyQuantity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceiptLine WithExpected(Func<AnyQuantity, AnyQuantity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._expectedRecipe = configure(_expectedRecipe);
        copy._expected       = copy._expectedRecipe;

        return copy;
    }

    /// <summary>Pins <c>received</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceiptLine WithReceived(Quantity value) {
        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._received = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>received</c> is drawn from — <c>AnyValidReceived()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyQuantity, AnyQuantity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceiptLine WithReceived(Func<AnyQuantity, AnyQuantity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._receivedRecipe = configure(_receivedRecipe);
        copy._received       = copy._receivedRecipe;

        return copy;
    }

    /// <summary>Pins <c>lot</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceiptLine WithLot(LotNumber value) {
        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._lot = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lot</c> is drawn from — <c>AnyValidLot()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLotNumber, AnyLotNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceiptLine WithLot(Func<AnyLotNumber, AnyLotNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._lotRecipe = configure(_lotRecipe);
        copy._lot       = copy._lotRecipe;

        return copy;
    }

    /// <summary>Pins <c>bestBefore</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceiptLine WithBestBefore(DateOnly value) {
        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._bestBefore = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bestBefore</c> is drawn from — <c>AnyValidBestBefore()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceiptLine WithBestBefore(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._bestBeforeRecipe = configure(_bestBeforeRecipe);
        copy._bestBefore       = copy._bestBeforeRecipe;

        return copy;
    }

    /// <summary>Pins <c>unitWeightKg</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceiptLine WithUnitWeightKg(decimal value) {
        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._unitWeightKg = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unitWeightKg</c> is drawn from — <c>AnyValidUnitWeightKg()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceiptLine WithUnitWeightKg(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceiptLine copy = new AnyReceiptLine(this);

        copy._unitWeightKgRecipe = configure(_unitWeightKgRecipe);
        copy._unitWeightKg       = copy._unitWeightKgRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReceiptLine" />.</summary>
    public ReceiptLine Generate() {
        return new ReceiptLine(_gtin.Generate(),
                               _expected.Generate(),
                               _received.Generate(),
                               _lot.Generate(),
                               _bestBefore.Generate(),
                               _unitWeightKg.Generate());
    }

}
