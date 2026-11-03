// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Dividend --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Dividend" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDividend : IAny<Dividend> {

    private IAny<Combination> _combination;
    private IAny<decimal>     _amountPerUnit;
    private IAny<int>         _winningUnits;

    private AnyCombination _combinationRecipe;
    private AnyDecimal     _amountPerUnitRecipe;
    private AnyInt32       _winningUnitsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDividend()
        : this(combination:   AnyValidCombination(),
               amountPerUnit: AnyValidAmountPerUnit(),
               winningUnits:  AnyValidWinningUnits()) { }

    private static AnyCombination AnyValidCombination() {
        return new AnyCombination();
    }

    private static AnyDecimal AnyValidAmountPerUnit() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyInt32 AnyValidWinningUnits() {
        return Any.Int32().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDividend(AnyCombination combination,
                        AnyDecimal     amountPerUnit,
                        AnyInt32       winningUnits) {
        _combination         = combination;
        _combinationRecipe   = combination;
        _amountPerUnit       = amountPerUnit;
        _amountPerUnitRecipe = amountPerUnit;
        _winningUnits        = winningUnits;
        _winningUnitsRecipe  = winningUnits;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDividend(AnyDividend from) {
        _combination         = from._combination;
        _combinationRecipe   = from._combinationRecipe;
        _amountPerUnit       = from._amountPerUnit;
        _amountPerUnitRecipe = from._amountPerUnitRecipe;
        _winningUnits        = from._winningUnits;
        _winningUnitsRecipe  = from._winningUnitsRecipe;
    }

    /// <summary>Pins <c>combination</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDividend WithCombination(Combination value) {
        AnyDividend copy = new AnyDividend(this);

        copy._combination = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>combination</c> is drawn from — <c>AnyValidCombination()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCombination, AnyCombination&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDividend WithCombination(Func<AnyCombination, AnyCombination> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDividend copy = new AnyDividend(this);

        copy._combinationRecipe = configure(_combinationRecipe);
        copy._combination       = copy._combinationRecipe;

        return copy;
    }

    /// <summary>Pins <c>amountPerUnit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDividend WithAmountPerUnit(decimal value) {
        AnyDividend copy = new AnyDividend(this);

        copy._amountPerUnit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amountPerUnit</c> is drawn from — <c>AnyValidAmountPerUnit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDividend WithAmountPerUnit(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDividend copy = new AnyDividend(this);

        copy._amountPerUnitRecipe = configure(_amountPerUnitRecipe);
        copy._amountPerUnit       = copy._amountPerUnitRecipe;

        return copy;
    }

    /// <summary>Pins <c>winningUnits</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDividend WithWinningUnits(int value) {
        AnyDividend copy = new AnyDividend(this);

        copy._winningUnits = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>winningUnits</c> is drawn from — <c>AnyValidWinningUnits()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDividend WithWinningUnits(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDividend copy = new AnyDividend(this);

        copy._winningUnitsRecipe = configure(_winningUnitsRecipe);
        copy._winningUnits       = copy._winningUnitsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Dividend" />.</summary>
    public Dividend Generate() {
        return new Dividend(_combination.Generate(),
                            _amountPerUnit.Generate(),
                            _winningUnits.Generate());
    }

}
