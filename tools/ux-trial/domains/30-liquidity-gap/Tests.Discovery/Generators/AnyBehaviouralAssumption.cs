// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BehaviouralAssumption --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BehaviouralAssumption" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBehaviouralAssumption : IAny<BehaviouralAssumption> {

    private IAny<decimal> _stableShare;
    private IAny<int>     _volatileRunOffDays;
    private IAny<int>     _coreMaturityMonths;

    private AnyDecimal _stableShareRecipe;
    private AnyInt32   _volatileRunOffDaysRecipe;
    private AnyInt32   _coreMaturityMonthsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBehaviouralAssumption()
        : this(stableShare:        AnyValidStableShare(),
               volatileRunOffDays: AnyValidVolatileRunOffDays(),
               coreMaturityMonths: AnyValidCoreMaturityMonths()) { }

    private static AnyDecimal AnyValidStableShare() {
        return Any.Decimal().Between(0m, 1m);
    }

    private static AnyInt32 AnyValidVolatileRunOffDays() {
        // The tie with the core maturity is written in Generate().
        return Any.Int32().Positive().LessThanOrEqualTo(365);
    }

    private static AnyInt32 AnyValidCoreMaturityMonths() {
        // The tie with the volatile horizon is written in Generate().
        return Any.Int32().Positive().LessThanOrEqualTo(120);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBehaviouralAssumption(AnyDecimal stableShare,
                                     AnyInt32   volatileRunOffDays,
                                     AnyInt32   coreMaturityMonths) {
        _stableShare              = stableShare;
        _stableShareRecipe        = stableShare;
        _volatileRunOffDays       = volatileRunOffDays;
        _volatileRunOffDaysRecipe = volatileRunOffDays;
        _coreMaturityMonths       = coreMaturityMonths;
        _coreMaturityMonthsRecipe = coreMaturityMonths;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBehaviouralAssumption(AnyBehaviouralAssumption from) {
        _stableShare              = from._stableShare;
        _stableShareRecipe        = from._stableShareRecipe;
        _volatileRunOffDays       = from._volatileRunOffDays;
        _volatileRunOffDaysRecipe = from._volatileRunOffDaysRecipe;
        _coreMaturityMonths       = from._coreMaturityMonths;
        _coreMaturityMonthsRecipe = from._coreMaturityMonthsRecipe;
    }

    /// <summary>Pins <c>stableShare</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBehaviouralAssumption WithStableShare(decimal value) {
        AnyBehaviouralAssumption copy = new AnyBehaviouralAssumption(this);

        copy._stableShare = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>stableShare</c> is drawn from — <c>AnyValidStableShare()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBehaviouralAssumption WithStableShare(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBehaviouralAssumption copy = new AnyBehaviouralAssumption(this);

        copy._stableShareRecipe = configure(_stableShareRecipe);
        copy._stableShare       = copy._stableShareRecipe;

        return copy;
    }

    /// <summary>Pins <c>volatileRunOffDays</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBehaviouralAssumption WithVolatileRunOffDays(int value) {
        AnyBehaviouralAssumption copy = new AnyBehaviouralAssumption(this);

        copy._volatileRunOffDaysRecipe = Any.Int32().OneOf(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>volatileRunOffDays</c> is drawn from — <c>AnyValidVolatileRunOffDays()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBehaviouralAssumption WithVolatileRunOffDays(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBehaviouralAssumption copy = new AnyBehaviouralAssumption(this);

        copy._volatileRunOffDaysRecipe = configure(_volatileRunOffDaysRecipe);
        copy._volatileRunOffDays       = copy._volatileRunOffDaysRecipe;

        return copy;
    }

    /// <summary>Pins <c>coreMaturityMonths</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBehaviouralAssumption WithCoreMaturityMonths(int value) {
        AnyBehaviouralAssumption copy = new AnyBehaviouralAssumption(this);

        copy._coreMaturityMonths = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>coreMaturityMonths</c> is drawn from — <c>AnyValidCoreMaturityMonths()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBehaviouralAssumption WithCoreMaturityMonths(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBehaviouralAssumption copy = new AnyBehaviouralAssumption(this);

        copy._coreMaturityMonthsRecipe = configure(_coreMaturityMonthsRecipe);
        copy._coreMaturityMonths       = copy._coreMaturityMonthsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BehaviouralAssumption" />.</summary>
    public BehaviouralAssumption Generate() {
        int core = _coreMaturityMonths.Generate();

        // The volatile horizon is shorter than the core maturity expressed in days.
        int volatileDays = _volatileRunOffDaysRecipe.LessThan(core * 30).Generate();

        return new BehaviouralAssumption(_stableShare.Generate(),
                                         volatileDays,
                                         core);
    }

}
