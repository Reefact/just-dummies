// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StabilityBudget --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ColdChain.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StabilityBudget" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStabilityBudget : IAny<StabilityBudget> {


    private AnyInt32 _cumulativeMinutesRecipe;
    private AnyInt32 _maxSingleExcursionMinutesRecipe;

    private int? _cumulativeMinutesPin;
    private int? _maxSingleExcursionMinutesPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStabilityBudget()
        : this(cumulativeMinutes:         AnyValidCumulativeMinutes(),
               maxSingleExcursionMinutes: AnyValidMaxSingleExcursionMinutes()) { }

    private static AnyInt32 AnyValidCumulativeMinutes() {
        return Any.Int32().Between(0, StabilityBudget.MaxCumulativeMinutes);
    }

    private static AnyInt32 AnyValidMaxSingleExcursionMinutes() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStabilityBudget(AnyInt32 cumulativeMinutes,
                               AnyInt32 maxSingleExcursionMinutes) {
        _cumulativeMinutesRecipe                       = cumulativeMinutes;
        _maxSingleExcursionMinutesRecipe               = maxSingleExcursionMinutes;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStabilityBudget(AnyStabilityBudget from) {
        _cumulativeMinutesRecipe                       = from._cumulativeMinutesRecipe;
        _cumulativeMinutesPin                          = from._cumulativeMinutesPin;
        _maxSingleExcursionMinutesRecipe               = from._maxSingleExcursionMinutesRecipe;
        _maxSingleExcursionMinutesPin                  = from._maxSingleExcursionMinutesPin;
    }

    /// <summary>Draws <c>maxSingleExcursionMinutes</c> and <c>cumulativeMinutes</c>, <c>maxSingleExcursionMinutes</c> never after <c>cumulativeMinutes</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (int MaxSingleExcursionMinutes, int CumulativeMinutes) MaxSingleExcursionMinutesAndCumulativeMinutes() {
        if (_maxSingleExcursionMinutesPin is { } maxSingleExcursionMinutes) { return (maxSingleExcursionMinutes, _cumulativeMinutesPin ?? _cumulativeMinutesRecipe.GreaterThanOrEqualTo(maxSingleExcursionMinutes).Generate()); }
        if (_cumulativeMinutesPin is { } cumulativeMinutes) { return (_maxSingleExcursionMinutesRecipe.LessThanOrEqualTo(cumulativeMinutes).Generate(), cumulativeMinutes); }

        return Any.Pair(_maxSingleExcursionMinutesRecipe, _cumulativeMinutesRecipe).Having.Item1LessThanOrEqualToItem2().Generate();
    }

    /// <summary>Pins <c>cumulativeMinutes</c> to a fixed value, which bounds <c>maxSingleExcursionMinutes</c>; its recipe is kept for a later refinement.</summary>
    public AnyStabilityBudget WithCumulativeMinutes(int value) {
        AnyStabilityBudget copy = new AnyStabilityBudget(this);

        copy._cumulativeMinutesPin                          = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>cumulativeMinutes</c> is drawn from — <c>AnyValidCumulativeMinutes()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>maxSingleExcursionMinutes</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStabilityBudget WithCumulativeMinutes(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStabilityBudget copy = new AnyStabilityBudget(this);

        copy._cumulativeMinutesRecipe                       = configure(_cumulativeMinutesRecipe);
        copy._cumulativeMinutesPin                          = null;

        return copy;
    }

    /// <summary>Pins <c>maxSingleExcursionMinutes</c> to a fixed value, which bounds <c>cumulativeMinutes</c>; its recipe is kept for a later refinement.</summary>
    public AnyStabilityBudget WithMaxSingleExcursionMinutes(int value) {
        AnyStabilityBudget copy = new AnyStabilityBudget(this);

        copy._maxSingleExcursionMinutesPin                  = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>maxSingleExcursionMinutes</c> is drawn from — <c>AnyValidMaxSingleExcursionMinutes()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>cumulativeMinutes</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStabilityBudget WithMaxSingleExcursionMinutes(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStabilityBudget copy = new AnyStabilityBudget(this);

        copy._maxSingleExcursionMinutesRecipe               = configure(_maxSingleExcursionMinutesRecipe);
        copy._maxSingleExcursionMinutesPin                  = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="StabilityBudget" />.</summary>
    public StabilityBudget Generate() {
        (int maxSingleExcursionMinutes, int cumulativeMinutes) = MaxSingleExcursionMinutesAndCumulativeMinutes();

        return new StabilityBudget(cumulativeMinutes,
                                   maxSingleExcursionMinutes);
    }

}
