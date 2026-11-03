// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Well --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Well" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWell : IAny<Well> {

    private IAny<string>         _id;
    private string?                _fixedId;
    private IAny<WellKind>       _kind;
    private IAny<CellCoordinate> _completion;
    private IAny<double>         _rateCubicMetresPerSecond;
    private IAny<Temperature>    _injectionTemperature;

    private AnyPattern        _idRecipe;
    private AnyEnum<WellKind> _kindRecipe;
    private AnyCellCoordinate _completionRecipe;
    private AnyDouble         _rateCubicMetresPerSecondRecipe;
    private AnyTemperature    _injectionTemperatureRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWell()
        : this(id:                       AnyValidId(),
               kind:                     AnyValidKind(),
               completion:               AnyValidCompletion(),
               rateCubicMetresPerSecond: AnyValidRateCubicMetresPerSecond(),
               injectionTemperature:     AnyValidInjectionTemperature()) { }

    // The id is "INJ-nn" or "PRD-nn": the prefix follows the kind, so only the two digits are drawn here.
    private static AnyPattern AnyValidId() {
        return Any.StringMatching("[0-9]{2}");
    }

    private static AnyEnum<WellKind> AnyValidKind() {
        return Any.Enum<WellKind>();
    }

    private static AnyCellCoordinate AnyValidCompletion() {
        return new AnyCellCoordinate();
    }

    private static AnyDouble AnyValidRateCubicMetresPerSecond() {
        return Any.Double().Positive().LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond);
    }

    private static AnyTemperature AnyValidInjectionTemperature() {
        return new AnyTemperature();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyWell(AnyPattern        id,
                    AnyEnum<WellKind> kind,
                    AnyCellCoordinate completion,
                    AnyDouble         rateCubicMetresPerSecond,
                    AnyTemperature    injectionTemperature) {
        _id                             = id;
        _idRecipe                       = id;
        _kind                           = kind;
        _kindRecipe                     = kind;
        _completion                     = completion;
        _completionRecipe               = completion;
        _rateCubicMetresPerSecond       = rateCubicMetresPerSecond;
        _rateCubicMetresPerSecondRecipe = rateCubicMetresPerSecond;
        _injectionTemperature           = injectionTemperature;
        _injectionTemperatureRecipe     = injectionTemperature;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyWell(AnyWell from) {
        _id                             = from._id;
        _idRecipe                       = from._idRecipe;
        _fixedId                        = from._fixedId;
        _kind                           = from._kind;
        _kindRecipe                     = from._kindRecipe;
        _completion                     = from._completion;
        _completionRecipe               = from._completionRecipe;
        _rateCubicMetresPerSecond       = from._rateCubicMetresPerSecond;
        _rateCubicMetresPerSecondRecipe = from._rateCubicMetresPerSecondRecipe;
        _injectionTemperature           = from._injectionTemperature;
        _injectionTemperatureRecipe     = from._injectionTemperatureRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWell WithId(string value) {
        AnyWell copy = new AnyWell(this);

        copy._fixedId = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWell WithId(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWell copy = new AnyWell(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWell WithKind(WellKind value) {
        AnyWell copy = new AnyWell(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;WellKind&gt;, AnyEnum&lt;WellKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWell WithKind(Func<AnyEnum<WellKind>, AnyEnum<WellKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWell copy = new AnyWell(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>completion</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWell WithCompletion(CellCoordinate value) {
        AnyWell copy = new AnyWell(this);

        copy._completion = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>completion</c> is drawn from — <c>AnyValidCompletion()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCellCoordinate, AnyCellCoordinate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWell WithCompletion(Func<AnyCellCoordinate, AnyCellCoordinate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWell copy = new AnyWell(this);

        copy._completionRecipe = configure(_completionRecipe);
        copy._completion       = copy._completionRecipe;

        return copy;
    }

    /// <summary>Pins <c>rateCubicMetresPerSecond</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWell WithRateCubicMetresPerSecond(double value) {
        AnyWell copy = new AnyWell(this);

        copy._rateCubicMetresPerSecond = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rateCubicMetresPerSecond</c> is drawn from — <c>AnyValidRateCubicMetresPerSecond()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWell WithRateCubicMetresPerSecond(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWell copy = new AnyWell(this);

        copy._rateCubicMetresPerSecondRecipe = configure(_rateCubicMetresPerSecondRecipe);
        copy._rateCubicMetresPerSecond       = copy._rateCubicMetresPerSecondRecipe;

        return copy;
    }

    /// <summary>Pins <c>injectionTemperature</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWell WithInjectionTemperature(Temperature value) {
        AnyWell copy = new AnyWell(this);

        copy._injectionTemperature = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>injectionTemperature</c> is drawn from — <c>AnyValidInjectionTemperature()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperature, AnyTemperature&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWell WithInjectionTemperature(Func<AnyTemperature, AnyTemperature> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWell copy = new AnyWell(this);

        copy._injectionTemperatureRecipe = configure(_injectionTemperatureRecipe);
        copy._injectionTemperature       = copy._injectionTemperatureRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Well" />.</summary>
    public Well Generate() {
        WellKind kind   = _kind.Generate();
        string   prefix = kind == WellKind.Injector ? "INJ-" : "PRD-";

        return new Well(_fixedId ?? prefix + _id.Generate(),
                        kind,
                        _completion.Generate(),
                        _rateCubicMetresPerSecond.Generate(),
                        kind == WellKind.Injector ? _injectionTemperature.Generate() : null);
    }

}
