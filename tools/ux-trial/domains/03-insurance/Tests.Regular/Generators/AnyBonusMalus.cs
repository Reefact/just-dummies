// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BonusMalus --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BonusMalus" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBonusMalus : IAny<BonusMalus> {

    private IAny<double> _coefficient;

    private AnyDouble _coefficientRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBonusMalus()
        : this(coefficient: AnyValidCoefficient()) { }

    private static AnyDouble AnyValidCoefficient() {
        return Any.Double().Between(BonusMalus.Floor, BonusMalus.Ceiling);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBonusMalus(AnyDouble coefficient) {
        _coefficient       = coefficient;
        _coefficientRecipe = coefficient;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBonusMalus(AnyBonusMalus from) {
        _coefficient       = from._coefficient;
        _coefficientRecipe = from._coefficientRecipe;
    }

    /// <summary>Pins <c>coefficient</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBonusMalus WithCoefficient(double value) {
        AnyBonusMalus copy = new AnyBonusMalus(this);

        copy._coefficient = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>coefficient</c> is drawn from — <c>AnyValidCoefficient()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBonusMalus WithCoefficient(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBonusMalus copy = new AnyBonusMalus(this);

        copy._coefficientRecipe = configure(_coefficientRecipe);
        copy._coefficient       = copy._coefficientRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BonusMalus" />.</summary>
    public BonusMalus Generate() {
        return new BonusMalus(_coefficient.Generate());
    }

}
