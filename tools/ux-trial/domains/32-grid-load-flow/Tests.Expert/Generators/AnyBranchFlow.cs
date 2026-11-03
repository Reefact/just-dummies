// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BranchFlow --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Numerics;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BranchFlow" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBranchFlow : IAny<BranchFlow> {

    private IAny<string>  _branchId;
    private IAny<BusId>   _fromBus;
    private IAny<BusId>   _toBus;
    private IAny<Complex> _fromEndMva;
    private IAny<Complex> _toEndMva;
    private IAny<double>  _ratingMva;

    private AnyString  _branchIdRecipe;
    private AnyBusId   _fromBusRecipe;
    private AnyBusId   _toBusRecipe;
    private AnyComplex _fromEndMvaRecipe;
    private AnyComplex _toEndMvaRecipe;
    private AnyDouble  _ratingMvaRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBranchFlow()
        : this(branchId:   AnyValidBranchId(),
               fromBus:    AnyValidFromBus(),
               toBus:      AnyValidToBus(),
               fromEndMva: AnyValidFromEndMva(),
               toEndMva:   AnyValidToEndMva(),
               ratingMva:  AnyValidRatingMva()) { }

    private static AnyString AnyValidBranchId() {
        return Any.String().NotBlank();
    }

    private static AnyBusId AnyValidFromBus() {
        return new AnyBusId();
    }

    private static AnyBusId AnyValidToBus() {
        return new AnyBusId();
    }

    private static AnyComplex AnyValidFromEndMva() {
        return new AnyComplex();
    }

    private static AnyComplex AnyValidToEndMva() {
        return new AnyComplex();
    }

    private static AnyDouble AnyValidRatingMva() {
        return Any.Double().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBranchFlow(AnyString  branchId,
                          AnyBusId   fromBus,
                          AnyBusId   toBus,
                          AnyComplex fromEndMva,
                          AnyComplex toEndMva,
                          AnyDouble  ratingMva) {
        _branchId         = branchId;
        _branchIdRecipe   = branchId;
        _fromBus          = fromBus;
        _fromBusRecipe    = fromBus;
        _toBus            = toBus;
        _toBusRecipe      = toBus;
        _fromEndMva       = fromEndMva;
        _fromEndMvaRecipe = fromEndMva;
        _toEndMva         = toEndMva;
        _toEndMvaRecipe   = toEndMva;
        _ratingMva        = ratingMva;
        _ratingMvaRecipe  = ratingMva;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBranchFlow(AnyBranchFlow from) {
        _branchId         = from._branchId;
        _branchIdRecipe   = from._branchIdRecipe;
        _fromBus          = from._fromBus;
        _fromBusRecipe    = from._fromBusRecipe;
        _toBus            = from._toBus;
        _toBusRecipe      = from._toBusRecipe;
        _fromEndMva       = from._fromEndMva;
        _fromEndMvaRecipe = from._fromEndMvaRecipe;
        _toEndMva         = from._toEndMva;
        _toEndMvaRecipe   = from._toEndMvaRecipe;
        _ratingMva        = from._ratingMva;
        _ratingMvaRecipe  = from._ratingMvaRecipe;
    }

    /// <summary>Pins <c>branchId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBranchFlow WithBranchId(string value) {
        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._branchId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>branchId</c> is drawn from — <c>AnyValidBranchId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBranchFlow WithBranchId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._branchIdRecipe = configure(_branchIdRecipe);
        copy._branchId       = copy._branchIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>fromBus</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBranchFlow WithFromBus(BusId value) {
        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._fromBus = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fromBus</c> is drawn from — <c>AnyValidFromBus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBusId, AnyBusId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBranchFlow WithFromBus(Func<AnyBusId, AnyBusId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._fromBusRecipe = configure(_fromBusRecipe);
        copy._fromBus       = copy._fromBusRecipe;

        return copy;
    }

    /// <summary>Pins <c>toBus</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBranchFlow WithToBus(BusId value) {
        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._toBus = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>toBus</c> is drawn from — <c>AnyValidToBus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBusId, AnyBusId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBranchFlow WithToBus(Func<AnyBusId, AnyBusId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._toBusRecipe = configure(_toBusRecipe);
        copy._toBus       = copy._toBusRecipe;

        return copy;
    }

    /// <summary>Pins <c>fromEndMva</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBranchFlow WithFromEndMva(Complex value) {
        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._fromEndMva = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fromEndMva</c> is drawn from — <c>AnyValidFromEndMva()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyComplex, AnyComplex&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBranchFlow WithFromEndMva(Func<AnyComplex, AnyComplex> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._fromEndMvaRecipe = configure(_fromEndMvaRecipe);
        copy._fromEndMva       = copy._fromEndMvaRecipe;

        return copy;
    }

    /// <summary>Pins <c>toEndMva</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBranchFlow WithToEndMva(Complex value) {
        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._toEndMva = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>toEndMva</c> is drawn from — <c>AnyValidToEndMva()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyComplex, AnyComplex&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBranchFlow WithToEndMva(Func<AnyComplex, AnyComplex> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._toEndMvaRecipe = configure(_toEndMvaRecipe);
        copy._toEndMva       = copy._toEndMvaRecipe;

        return copy;
    }

    /// <summary>Pins <c>ratingMva</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBranchFlow WithRatingMva(double value) {
        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._ratingMva = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ratingMva</c> is drawn from — <c>AnyValidRatingMva()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBranchFlow WithRatingMva(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBranchFlow copy = new AnyBranchFlow(this);

        copy._ratingMvaRecipe = configure(_ratingMvaRecipe);
        copy._ratingMva       = copy._ratingMvaRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BranchFlow" />.</summary>
    public BranchFlow Generate() {
        BusId fromBus = _fromBus.Generate();
        BusId toBus   = ReferenceEquals(_toBus, _toBusRecipe) ? _toBus.DifferentFrom(fromBus).Generate() : _toBus.Generate();

        return new BranchFlow(_branchId.Generate(),
                              fromBus,
                              toBus,
                              _fromEndMva.Generate(),
                              _toEndMva.Generate(),
                              _ratingMva.Generate());
    }

}
