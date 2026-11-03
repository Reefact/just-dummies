// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Stake --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Stake" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStake : IAny<Stake> {

    private IAny<Combination> _combination;
    private IAny<int>         _units;

    private AnyCombination _combinationRecipe;
    private AnyInt32       _unitsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStake()
        : this(combination: AnyValidCombination(),
               units:       AnyValidUnits()) { }

    private static AnyCombination AnyValidCombination() {
        return new AnyCombination();
    }

    private static AnyInt32 AnyValidUnits() {
        return Any.Int32().Positive().LessThanOrEqualTo(Stake.MaximumUnits);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStake(AnyCombination combination,
                     AnyInt32       units) {
        _combination       = combination;
        _combinationRecipe = combination;
        _units             = units;
        _unitsRecipe       = units;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStake(AnyStake from) {
        _combination       = from._combination;
        _combinationRecipe = from._combinationRecipe;
        _units             = from._units;
        _unitsRecipe       = from._unitsRecipe;
    }

    /// <summary>Pins <c>combination</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStake WithCombination(Combination value) {
        AnyStake copy = new AnyStake(this);

        copy._combination = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>combination</c> is drawn from — <c>AnyValidCombination()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCombination, AnyCombination&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStake WithCombination(Func<AnyCombination, AnyCombination> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStake copy = new AnyStake(this);

        copy._combinationRecipe = configure(_combinationRecipe);
        copy._combination       = copy._combinationRecipe;

        return copy;
    }

    /// <summary>Pins <c>units</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStake WithUnits(int value) {
        AnyStake copy = new AnyStake(this);

        copy._units = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>units</c> is drawn from — <c>AnyValidUnits()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStake WithUnits(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStake copy = new AnyStake(this);

        copy._unitsRecipe = configure(_unitsRecipe);
        copy._units       = copy._unitsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Stake" />.</summary>
    public Stake Generate() {
        return new Stake(_combination.Generate(),
                         _units.Generate());
    }

}
