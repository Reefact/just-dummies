// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PoolRules --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="PoolRules" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPoolRules : IAny<PoolRules> {

    private IAny<decimal> _takeoutRate;
    private IAny<decimal> _breakageStep;
    private IAny<decimal> _minimumReturnRatio;
    private IAny<int>     _placesPaid;

    private AnyDecimal _takeoutRateRecipe;
    private AnyDecimal _breakageStepRecipe;
    private AnyDecimal _minimumReturnRatioRecipe;
    private AnyInt32   _placesPaidRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPoolRules()
        : this(takeoutRate:        AnyValidTakeoutRate(),
               breakageStep:       AnyValidBreakageStep(),
               minimumReturnRatio: AnyValidMinimumReturnRatio(),
               placesPaid:         AnyValidPlacesPaid()) { }

    private static AnyDecimal AnyValidTakeoutRate() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).LessThan(0.5m);
    }

    private static AnyDecimal AnyValidBreakageStep() {
        return Any.Decimal().Between(0.01m, 1m).WithScale(2);
    }

    private static AnyDecimal AnyValidMinimumReturnRatio() {
        return Any.Decimal().Between(1m, 1.5m);
    }

    private static AnyInt32 AnyValidPlacesPaid() {
        return Any.Int32().Positive().LessThanOrEqualTo(3);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPoolRules(AnyDecimal takeoutRate,
                         AnyDecimal breakageStep,
                         AnyDecimal minimumReturnRatio,
                         AnyInt32   placesPaid) {
        _takeoutRate              = takeoutRate;
        _takeoutRateRecipe        = takeoutRate;
        _breakageStep             = breakageStep;
        _breakageStepRecipe       = breakageStep;
        _minimumReturnRatio       = minimumReturnRatio;
        _minimumReturnRatioRecipe = minimumReturnRatio;
        _placesPaid               = placesPaid;
        _placesPaidRecipe         = placesPaid;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPoolRules(AnyPoolRules from) {
        _takeoutRate              = from._takeoutRate;
        _takeoutRateRecipe        = from._takeoutRateRecipe;
        _breakageStep             = from._breakageStep;
        _breakageStepRecipe       = from._breakageStepRecipe;
        _minimumReturnRatio       = from._minimumReturnRatio;
        _minimumReturnRatioRecipe = from._minimumReturnRatioRecipe;
        _placesPaid               = from._placesPaid;
        _placesPaidRecipe         = from._placesPaidRecipe;
    }

    /// <summary>Pins <c>takeoutRate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPoolRules WithTakeoutRate(decimal value) {
        AnyPoolRules copy = new AnyPoolRules(this);

        copy._takeoutRate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>takeoutRate</c> is drawn from — <c>AnyValidTakeoutRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPoolRules WithTakeoutRate(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPoolRules copy = new AnyPoolRules(this);

        copy._takeoutRateRecipe = configure(_takeoutRateRecipe);
        copy._takeoutRate       = copy._takeoutRateRecipe;

        return copy;
    }

    /// <summary>Pins <c>breakageStep</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPoolRules WithBreakageStep(decimal value) {
        AnyPoolRules copy = new AnyPoolRules(this);

        copy._breakageStep = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>breakageStep</c> is drawn from — <c>AnyValidBreakageStep()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPoolRules WithBreakageStep(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPoolRules copy = new AnyPoolRules(this);

        copy._breakageStepRecipe = configure(_breakageStepRecipe);
        copy._breakageStep       = copy._breakageStepRecipe;

        return copy;
    }

    /// <summary>Pins <c>minimumReturnRatio</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPoolRules WithMinimumReturnRatio(decimal value) {
        AnyPoolRules copy = new AnyPoolRules(this);

        copy._minimumReturnRatio = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minimumReturnRatio</c> is drawn from — <c>AnyValidMinimumReturnRatio()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPoolRules WithMinimumReturnRatio(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPoolRules copy = new AnyPoolRules(this);

        copy._minimumReturnRatioRecipe = configure(_minimumReturnRatioRecipe);
        copy._minimumReturnRatio       = copy._minimumReturnRatioRecipe;

        return copy;
    }

    /// <summary>Pins <c>placesPaid</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPoolRules WithPlacesPaid(int value) {
        AnyPoolRules copy = new AnyPoolRules(this);

        copy._placesPaid = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>placesPaid</c> is drawn from — <c>AnyValidPlacesPaid()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPoolRules WithPlacesPaid(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPoolRules copy = new AnyPoolRules(this);

        copy._placesPaidRecipe = configure(_placesPaidRecipe);
        copy._placesPaid       = copy._placesPaidRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PoolRules" />.</summary>
    public PoolRules Generate() {
        return new PoolRules(_takeoutRate.Generate(),
                             _breakageStep.Generate(),
                             _minimumReturnRatio.Generate(),
                             _placesPaid.Generate());
    }

}
