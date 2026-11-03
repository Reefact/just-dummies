// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MilkingAnomaly --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MilkingAnomaly" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMilkingAnomaly : IAny<MilkingAnomaly> {

    private IAny<AnomalyKind>   _kind;
    private IAny<UdderQuarter?>? _quarter;   // null: follow the kind (a quarter for a quarter-level kind, none otherwise)
    private IAny<TimeSpan>      _elapsed;

    private AnyEnum<AnomalyKind>  _kindRecipe;
    private AnyEnum<UdderQuarter> _quarterRecipe;
    private AnyTimeSpan           _elapsedRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMilkingAnomaly()
        : this(kind:    AnyValidKind(),
               quarter: AnyValidQuarter(),
               elapsed: AnyValidElapsed()) { }

    private static AnyEnum<AnomalyKind> AnyValidKind() {
        return Any.Enum<AnomalyKind>();
    }

    private static AnyEnum<UdderQuarter> AnyValidQuarter() {
        return Any.Enum<UdderQuarter>();
    }

    private static AnyTimeSpan AnyValidElapsed() {
        return Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.Zero);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMilkingAnomaly(AnyEnum<AnomalyKind>  kind,
                              AnyEnum<UdderQuarter> quarter,
                              AnyTimeSpan           elapsed) {
        _kind          = kind;
        _kindRecipe    = kind;
        _quarter       = null;
        _quarterRecipe = quarter;
        _elapsed       = elapsed;
        _elapsedRecipe = elapsed;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMilkingAnomaly(AnyMilkingAnomaly from) {
        _kind          = from._kind;
        _kindRecipe    = from._kindRecipe;
        _quarter       = from._quarter;
        _quarterRecipe = from._quarterRecipe;
        _elapsed       = from._elapsed;
        _elapsedRecipe = from._elapsedRecipe;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingAnomaly WithKind(AnomalyKind value) {
        AnyMilkingAnomaly copy = new AnyMilkingAnomaly(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;AnomalyKind&gt;, AnyEnum&lt;AnomalyKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingAnomaly WithKind(Func<AnyEnum<AnomalyKind>, AnyEnum<AnomalyKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingAnomaly copy = new AnyMilkingAnomaly(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>quarter</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingAnomaly WithQuarter(UdderQuarter? value) {
        AnyMilkingAnomaly copy = new AnyMilkingAnomaly(this);

        copy._quarter = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quarter</c> is drawn from — <c>AnyValidQuarter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;UdderQuarter&gt;, AnyEnum&lt;UdderQuarter&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingAnomaly WithQuarter(Func<AnyEnum<UdderQuarter>, AnyEnum<UdderQuarter>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingAnomaly copy = new AnyMilkingAnomaly(this);

        copy._quarterRecipe = configure(_quarterRecipe);
        copy._quarter       = copy._quarterRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>quarter</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyMilkingAnomaly WithoutQuarter() {
        AnyMilkingAnomaly copy = new AnyMilkingAnomaly(this);

        copy._quarter = Any.Fixed<UdderQuarter?>(null);

        return copy;
    }

    /// <summary>Pins <c>elapsed</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMilkingAnomaly WithElapsed(TimeSpan value) {
        AnyMilkingAnomaly copy = new AnyMilkingAnomaly(this);

        copy._elapsed = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>elapsed</c> is drawn from — <c>AnyValidElapsed()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMilkingAnomaly WithElapsed(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMilkingAnomaly copy = new AnyMilkingAnomaly(this);

        copy._elapsedRecipe = configure(_elapsedRecipe);
        copy._elapsed       = copy._elapsedRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MilkingAnomaly" />.</summary>
    /// <remarks>
    ///     Left alone, the quarter follows the kind: quarter-level kinds get one, session-level kinds none. A quarter
    ///     set through <c>WithQuarter</c> or <c>WithoutQuarter</c> is passed as given, and the anomaly refuses it if
    ///     the kind disagrees.
    /// </remarks>
    public MilkingAnomaly Generate() {
        AnomalyKind   kind    = _kind.Generate();
        UdderQuarter? quarter = _quarter is not null                      ? _quarter.Generate()
                                : MilkingAnomaly.IsQuarterLevel(kind) ? _quarterRecipe.Generate()
                                                                      : null;

        return new MilkingAnomaly(kind, quarter, _elapsed.Generate());
    }

}
