// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Line --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Line" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLine : IAny<Line> {

    private IAny<string>    _id;
    private IAny<BusId>     _fromBus;
    private IAny<BusId>     _toBus;
    private IAny<Impedance> _impedance;
    private IAny<double>    _chargingSusceptancePu;
    private IAny<double>    _ratingMva;

    private AnyString    _idRecipe;
    private AnyBusId     _fromBusRecipe;
    private AnyBusId     _toBusRecipe;
    private AnyImpedance _impedanceRecipe;
    private AnyDouble    _chargingSusceptancePuRecipe;
    private AnyDouble    _ratingMvaRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLine()
        : this(id:                    AnyValidId(),
               fromBus:               AnyValidFromBus(),
               toBus:                 AnyValidToBus(),
               impedance:             AnyValidImpedance(),
               chargingSusceptancePu: AnyValidChargingSusceptancePu(),
               ratingMva:             AnyValidRatingMva()) { }

    private static AnyString AnyValidId() {
        return Any.String().NotBlank().WithMaxLength(32);
    }

    private static AnyBusId AnyValidFromBus() {
        return new AnyBusId();
    }

    private static AnyBusId AnyValidToBus() {
        return new AnyBusId();
    }

    private static AnyImpedance AnyValidImpedance() {
        return new AnyImpedance();
    }

    private static AnyDouble AnyValidChargingSusceptancePu() {
        return Any.Double().Between(0d, Line.MaximumChargingSusceptancePu);
    }

    private static AnyDouble AnyValidRatingMva() {
        return Any.Double().Positive().LessThanOrEqualTo(Line.MaximumRatingMva);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLine(AnyString    id,
                    AnyBusId     fromBus,
                    AnyBusId     toBus,
                    AnyImpedance impedance,
                    AnyDouble    chargingSusceptancePu,
                    AnyDouble    ratingMva) {
        _id                          = id;
        _idRecipe                    = id;
        _fromBus                     = fromBus;
        _fromBusRecipe               = fromBus;
        _toBus                       = toBus;
        _toBusRecipe                 = toBus;
        _impedance                   = impedance;
        _impedanceRecipe             = impedance;
        _chargingSusceptancePu       = chargingSusceptancePu;
        _chargingSusceptancePuRecipe = chargingSusceptancePu;
        _ratingMva                   = ratingMva;
        _ratingMvaRecipe             = ratingMva;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLine(AnyLine from) {
        _id                          = from._id;
        _idRecipe                    = from._idRecipe;
        _fromBus                     = from._fromBus;
        _fromBusRecipe               = from._fromBusRecipe;
        _toBus                       = from._toBus;
        _toBusRecipe                 = from._toBusRecipe;
        _impedance                   = from._impedance;
        _impedanceRecipe             = from._impedanceRecipe;
        _chargingSusceptancePu       = from._chargingSusceptancePu;
        _chargingSusceptancePuRecipe = from._chargingSusceptancePuRecipe;
        _ratingMva                   = from._ratingMva;
        _ratingMvaRecipe             = from._ratingMvaRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLine WithId(string value) {
        AnyLine copy = new AnyLine(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLine WithId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLine copy = new AnyLine(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>fromBus</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLine WithFromBus(BusId value) {
        AnyLine copy = new AnyLine(this);

        copy._fromBus = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fromBus</c> is drawn from — <c>AnyValidFromBus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBusId, AnyBusId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLine WithFromBus(Func<AnyBusId, AnyBusId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLine copy = new AnyLine(this);

        copy._fromBusRecipe = configure(_fromBusRecipe);
        copy._fromBus       = copy._fromBusRecipe;

        return copy;
    }

    /// <summary>Pins <c>toBus</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLine WithToBus(BusId value) {
        AnyLine copy = new AnyLine(this);

        copy._toBus = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>toBus</c> is drawn from — <c>AnyValidToBus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBusId, AnyBusId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLine WithToBus(Func<AnyBusId, AnyBusId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLine copy = new AnyLine(this);

        copy._toBusRecipe = configure(_toBusRecipe);
        copy._toBus       = copy._toBusRecipe;

        return copy;
    }

    /// <summary>Pins <c>impedance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLine WithImpedance(Impedance value) {
        AnyLine copy = new AnyLine(this);

        copy._impedance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>impedance</c> is drawn from — <c>AnyValidImpedance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyImpedance, AnyImpedance&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLine WithImpedance(Func<AnyImpedance, AnyImpedance> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLine copy = new AnyLine(this);

        copy._impedanceRecipe = configure(_impedanceRecipe);
        copy._impedance       = copy._impedanceRecipe;

        return copy;
    }

    /// <summary>Pins <c>chargingSusceptancePu</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLine WithChargingSusceptancePu(double value) {
        AnyLine copy = new AnyLine(this);

        copy._chargingSusceptancePu = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>chargingSusceptancePu</c> is drawn from — <c>AnyValidChargingSusceptancePu()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLine WithChargingSusceptancePu(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLine copy = new AnyLine(this);

        copy._chargingSusceptancePuRecipe = configure(_chargingSusceptancePuRecipe);
        copy._chargingSusceptancePu       = copy._chargingSusceptancePuRecipe;

        return copy;
    }

    /// <summary>Pins <c>ratingMva</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLine WithRatingMva(double value) {
        AnyLine copy = new AnyLine(this);

        copy._ratingMva = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ratingMva</c> is drawn from — <c>AnyValidRatingMva()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLine WithRatingMva(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLine copy = new AnyLine(this);

        copy._ratingMvaRecipe = configure(_ratingMvaRecipe);
        copy._ratingMva       = copy._ratingMvaRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Line" />.</summary>
    public Line Generate() {
        BusId fromBus = _fromBus.Generate();
        BusId toBus   = _toBus.DifferentFrom(fromBus).Generate();

        return new Line(_id.Generate(),
                        fromBus,
                        toBus,
                        _impedance.Generate(),
                        _chargingSusceptancePu.Generate(),
                        _ratingMva.Generate());
    }

}
