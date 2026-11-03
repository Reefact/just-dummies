// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Transformer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Transformer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTransformer : IAny<Transformer> {

    private IAny<string>    _id;
    private IAny<BusId>     _highVoltageBus;
    private IAny<BusId>     _lowVoltageBus;
    private IAny<Impedance> _impedance;
    private IAny<double>    _ratingMva;
    private IAny<double>    _tapRatio;

    private AnyString    _idRecipe;
    private AnyBusId     _highVoltageBusRecipe;
    private AnyBusId     _lowVoltageBusRecipe;
    private AnyImpedance _impedanceRecipe;
    private AnyDouble    _ratingMvaRecipe;
    private AnyDouble    _tapRatioRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTransformer()
        : this(id:             AnyValidId(),
               highVoltageBus: AnyValidHighVoltageBus(),
               lowVoltageBus:  AnyValidLowVoltageBus(),
               impedance:      AnyValidImpedance(),
               ratingMva:      AnyValidRatingMva(),
               tapRatio:       AnyValidTapRatio()) { }

    private static AnyString AnyValidId() {
        return Any.String().NotBlank().WithMaxLength(32);
    }

    private static AnyBusId AnyValidHighVoltageBus() {
        return new AnyBusId();
    }

    private static AnyBusId AnyValidLowVoltageBus() {
        return new AnyBusId();
    }

    private static AnyImpedance AnyValidImpedance() {
        return new AnyImpedance();
    }

    private static AnyDouble AnyValidRatingMva() {
        return Any.Double().Positive().LessThanOrEqualTo(Line.MaximumRatingMva);
    }

    private static AnyDouble AnyValidTapRatio() {
        return Any.Double().Between(Transformer.MinimumTap, Transformer.MaximumTap);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTransformer(AnyString    id,
                           AnyBusId     highVoltageBus,
                           AnyBusId     lowVoltageBus,
                           AnyImpedance impedance,
                           AnyDouble    ratingMva,
                           AnyDouble    tapRatio) {
        _id                   = id;
        _idRecipe             = id;
        _highVoltageBus       = highVoltageBus;
        _highVoltageBusRecipe = highVoltageBus;
        _lowVoltageBus        = lowVoltageBus;
        _lowVoltageBusRecipe  = lowVoltageBus;
        _impedance            = impedance;
        _impedanceRecipe      = impedance;
        _ratingMva            = ratingMva;
        _ratingMvaRecipe      = ratingMva;
        _tapRatio             = tapRatio;
        _tapRatioRecipe       = tapRatio;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTransformer(AnyTransformer from) {
        _id                   = from._id;
        _idRecipe             = from._idRecipe;
        _highVoltageBus       = from._highVoltageBus;
        _highVoltageBusRecipe = from._highVoltageBusRecipe;
        _lowVoltageBus        = from._lowVoltageBus;
        _lowVoltageBusRecipe  = from._lowVoltageBusRecipe;
        _impedance            = from._impedance;
        _impedanceRecipe      = from._impedanceRecipe;
        _ratingMva            = from._ratingMva;
        _ratingMvaRecipe      = from._ratingMvaRecipe;
        _tapRatio             = from._tapRatio;
        _tapRatioRecipe       = from._tapRatioRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformer WithId(string value) {
        AnyTransformer copy = new AnyTransformer(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformer WithId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformer copy = new AnyTransformer(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>highVoltageBus</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformer WithHighVoltageBus(BusId value) {
        AnyTransformer copy = new AnyTransformer(this);

        copy._highVoltageBus = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>highVoltageBus</c> is drawn from — <c>AnyValidHighVoltageBus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBusId, AnyBusId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformer WithHighVoltageBus(Func<AnyBusId, AnyBusId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformer copy = new AnyTransformer(this);

        copy._highVoltageBusRecipe = configure(_highVoltageBusRecipe);
        copy._highVoltageBus       = copy._highVoltageBusRecipe;

        return copy;
    }

    /// <summary>Pins <c>lowVoltageBus</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformer WithLowVoltageBus(BusId value) {
        AnyTransformer copy = new AnyTransformer(this);

        copy._lowVoltageBus = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lowVoltageBus</c> is drawn from — <c>AnyValidLowVoltageBus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBusId, AnyBusId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformer WithLowVoltageBus(Func<AnyBusId, AnyBusId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformer copy = new AnyTransformer(this);

        copy._lowVoltageBusRecipe = configure(_lowVoltageBusRecipe);
        copy._lowVoltageBus       = copy._lowVoltageBusRecipe;

        return copy;
    }

    /// <summary>Pins <c>impedance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformer WithImpedance(Impedance value) {
        AnyTransformer copy = new AnyTransformer(this);

        copy._impedance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>impedance</c> is drawn from — <c>AnyValidImpedance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyImpedance, AnyImpedance&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformer WithImpedance(Func<AnyImpedance, AnyImpedance> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformer copy = new AnyTransformer(this);

        copy._impedanceRecipe = configure(_impedanceRecipe);
        copy._impedance       = copy._impedanceRecipe;

        return copy;
    }

    /// <summary>Pins <c>ratingMva</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformer WithRatingMva(double value) {
        AnyTransformer copy = new AnyTransformer(this);

        copy._ratingMva = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ratingMva</c> is drawn from — <c>AnyValidRatingMva()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformer WithRatingMva(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformer copy = new AnyTransformer(this);

        copy._ratingMvaRecipe = configure(_ratingMvaRecipe);
        copy._ratingMva       = copy._ratingMvaRecipe;

        return copy;
    }

    /// <summary>Pins <c>tapRatio</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformer WithTapRatio(double value) {
        AnyTransformer copy = new AnyTransformer(this);

        copy._tapRatio = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tapRatio</c> is drawn from — <c>AnyValidTapRatio()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformer WithTapRatio(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformer copy = new AnyTransformer(this);

        copy._tapRatioRecipe = configure(_tapRatioRecipe);
        copy._tapRatio       = copy._tapRatioRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Transformer" />.</summary>
    public Transformer Generate() {
        BusId highVoltageBus = _highVoltageBus.Generate();
        BusId lowVoltageBus  = _lowVoltageBus.DifferentFrom(highVoltageBus).Generate();

        return new Transformer(_id.Generate(),
                               highVoltageBus,
                               lowVoltageBus,
                               _impedance.Generate(),
                               _ratingMva.Generate(),
                               _tapRatio.Generate());
    }

}
