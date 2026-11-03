// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CatastropheEvent --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CatastropheEvent" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCatastropheEvent : IAny<CatastropheEvent> {

    private IAny<string>    _id;
    private IAny<Peril>     _peril;
    private IAny<Footprint> _footprint;
    private IAny<double>    _annualRate;

    private AnyPattern      _idRecipe;
    private AnyEnum<Peril> _perilRecipe;
    private AnyFootprint   _footprintRecipe;
    private AnyDouble      _annualRateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCatastropheEvent()
        : this(id:         AnyValidId(),
               peril:      AnyValidPeril(),
               footprint:  AnyValidFootprint(),
               annualRate: AnyValidAnnualRate()) { }

    private static AnyPattern AnyValidId() {
        return Any.StringMatching("EV-[0-9]{4,8}");
    }

    private static AnyEnum<Peril> AnyValidPeril() {
        return Any.Enum<Peril>();
    }

    private static AnyFootprint AnyValidFootprint() {
        return new AnyFootprint();
    }

    private static AnyDouble AnyValidAnnualRate() {
        return Any.Double().Positive().LessThanOrEqualTo(CatastropheEvent.MaxAnnualRate);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCatastropheEvent(AnyPattern      id,
                                AnyEnum<Peril> peril,
                                AnyFootprint   footprint,
                                AnyDouble      annualRate) {
        _id               = id;
        _idRecipe         = id;
        _peril            = peril;
        _perilRecipe      = peril;
        _footprint        = footprint;
        _footprintRecipe  = footprint;
        _annualRate       = annualRate;
        _annualRateRecipe = annualRate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCatastropheEvent(AnyCatastropheEvent from) {
        _id               = from._id;
        _idRecipe         = from._idRecipe;
        _peril            = from._peril;
        _perilRecipe      = from._perilRecipe;
        _footprint        = from._footprint;
        _footprintRecipe  = from._footprintRecipe;
        _annualRate       = from._annualRate;
        _annualRateRecipe = from._annualRateRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCatastropheEvent WithId(string value) {
        AnyCatastropheEvent copy = new AnyCatastropheEvent(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCatastropheEvent WithId(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCatastropheEvent copy = new AnyCatastropheEvent(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>peril</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCatastropheEvent WithPeril(Peril value) {
        AnyCatastropheEvent copy = new AnyCatastropheEvent(this);

        copy._peril = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>peril</c> is drawn from — <c>AnyValidPeril()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;Peril&gt;, AnyEnum&lt;Peril&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCatastropheEvent WithPeril(Func<AnyEnum<Peril>, AnyEnum<Peril>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCatastropheEvent copy = new AnyCatastropheEvent(this);

        copy._perilRecipe = configure(_perilRecipe);
        copy._peril       = copy._perilRecipe;

        return copy;
    }

    /// <summary>Pins <c>footprint</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCatastropheEvent WithFootprint(Footprint value) {
        AnyCatastropheEvent copy = new AnyCatastropheEvent(this);

        copy._footprint = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>footprint</c> is drawn from — <c>AnyValidFootprint()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyFootprint, AnyFootprint&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCatastropheEvent WithFootprint(Func<AnyFootprint, AnyFootprint> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCatastropheEvent copy = new AnyCatastropheEvent(this);

        copy._footprintRecipe = configure(_footprintRecipe);
        copy._footprint       = copy._footprintRecipe;

        return copy;
    }

    /// <summary>Pins <c>annualRate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCatastropheEvent WithAnnualRate(double value) {
        AnyCatastropheEvent copy = new AnyCatastropheEvent(this);

        copy._annualRate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>annualRate</c> is drawn from — <c>AnyValidAnnualRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCatastropheEvent WithAnnualRate(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCatastropheEvent copy = new AnyCatastropheEvent(this);

        copy._annualRateRecipe = configure(_annualRateRecipe);
        copy._annualRate       = copy._annualRateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CatastropheEvent" />.</summary>
    public CatastropheEvent Generate() {
        return new CatastropheEvent(_id.Generate(),
                                    _peril.Generate(),
                                    _footprint.Generate(),
                                    _annualRate.Generate());
    }

}
