// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TraceEntry --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TraceEntry" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTraceEntry : IAny<TraceEntry> {

    private IAny<LotNumber> _lot;
    private IAny<int>       _depth;
    private IAny<decimal>   _share;
    private IAny<Quantity>  _exposedQuantity;

    private AnyLotNumber _lotRecipe;
    private AnyInt32     _depthRecipe;
    private AnyDecimal   _shareRecipe;
    private AnyQuantity  _exposedQuantityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTraceEntry()
        : this(lot:             AnyValidLot(),
               depth:           AnyValidDepth(),
               share:           AnyValidShare(),
               exposedQuantity: AnyValidExposedQuantity()) { }

    private static AnyLotNumber AnyValidLot() {
        return new AnyLotNumber();
    }

    private static AnyInt32 AnyValidDepth() {
        return Any.Int32().Positive();
    }

    private static AnyDecimal AnyValidShare() {
        return Any.Decimal().Positive().LessThanOrEqualTo(1m);
    }

    private static AnyQuantity AnyValidExposedQuantity() {
        return new AnyQuantity();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTraceEntry(AnyLotNumber lot,
                          AnyInt32     depth,
                          AnyDecimal   share,
                          AnyQuantity  exposedQuantity) {
        _lot                   = lot;
        _lotRecipe             = lot;
        _depth                 = depth;
        _depthRecipe           = depth;
        _share                 = share;
        _shareRecipe           = share;
        _exposedQuantity       = exposedQuantity;
        _exposedQuantityRecipe = exposedQuantity;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTraceEntry(AnyTraceEntry from) {
        _lot                   = from._lot;
        _lotRecipe             = from._lotRecipe;
        _depth                 = from._depth;
        _depthRecipe           = from._depthRecipe;
        _share                 = from._share;
        _shareRecipe           = from._shareRecipe;
        _exposedQuantity       = from._exposedQuantity;
        _exposedQuantityRecipe = from._exposedQuantityRecipe;
    }

    /// <summary>Pins <c>lot</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTraceEntry WithLot(LotNumber value) {
        AnyTraceEntry copy = new AnyTraceEntry(this);

        copy._lot = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lot</c> is drawn from — <c>AnyValidLot()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLotNumber, AnyLotNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTraceEntry WithLot(Func<AnyLotNumber, AnyLotNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTraceEntry copy = new AnyTraceEntry(this);

        copy._lotRecipe = configure(_lotRecipe);
        copy._lot       = copy._lotRecipe;

        return copy;
    }

    /// <summary>Pins <c>depth</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTraceEntry WithDepth(int value) {
        AnyTraceEntry copy = new AnyTraceEntry(this);

        copy._depth = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>depth</c> is drawn from — <c>AnyValidDepth()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTraceEntry WithDepth(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTraceEntry copy = new AnyTraceEntry(this);

        copy._depthRecipe = configure(_depthRecipe);
        copy._depth       = copy._depthRecipe;

        return copy;
    }

    /// <summary>Pins <c>share</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTraceEntry WithShare(decimal value) {
        AnyTraceEntry copy = new AnyTraceEntry(this);

        copy._share = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>share</c> is drawn from — <c>AnyValidShare()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTraceEntry WithShare(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTraceEntry copy = new AnyTraceEntry(this);

        copy._shareRecipe = configure(_shareRecipe);
        copy._share       = copy._shareRecipe;

        return copy;
    }

    /// <summary>Pins <c>exposedQuantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTraceEntry WithExposedQuantity(Quantity value) {
        AnyTraceEntry copy = new AnyTraceEntry(this);

        copy._exposedQuantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>exposedQuantity</c> is drawn from — <c>AnyValidExposedQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyQuantity, AnyQuantity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTraceEntry WithExposedQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTraceEntry copy = new AnyTraceEntry(this);

        copy._exposedQuantityRecipe = configure(_exposedQuantityRecipe);
        copy._exposedQuantity       = copy._exposedQuantityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TraceEntry" />.</summary>
    public TraceEntry Generate() {
        return new TraceEntry(_lot.Generate(),
                              _depth.Generate(),
                              _share.Generate(),
                              _exposedQuantity.Generate());
    }

}
