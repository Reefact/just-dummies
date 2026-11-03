// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Fit --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Fit" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFit : IAny<Fit> {

    private IAny<Dimension> _hole;
    private IAny<Dimension> _shaft;

    private AnyDimension _holeRecipe;
    private AnyDimension _shaftRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFit()
        : this(hole:  AnyValidHole(),
               shaft: AnyValidShaft()) { }

    private static AnyDimension AnyValidHole() {
        return new AnyDimension().WithTolerance(t => t.Hole());
    }

    private static AnyDimension AnyValidShaft() {
        return new AnyDimension().WithTolerance(t => t.Shaft());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFit(AnyDimension hole,
                   AnyDimension shaft) {
        _hole        = hole;
        _holeRecipe  = hole;
        _shaft       = shaft;
        _shaftRecipe = shaft;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFit(AnyFit from) {
        _hole        = from._hole;
        _holeRecipe  = from._holeRecipe;
        _shaft       = from._shaft;
        _shaftRecipe = from._shaftRecipe;
    }

    /// <summary>Pins <c>hole</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFit WithHole(Dimension value) {
        AnyFit copy = new AnyFit(this);

        copy._hole = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>hole</c> is drawn from — <c>AnyValidHole()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDimension, AnyDimension&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFit WithHole(Func<AnyDimension, AnyDimension> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFit copy = new AnyFit(this);

        copy._holeRecipe = configure(_holeRecipe);
        copy._hole       = copy._holeRecipe;

        return copy;
    }

    /// <summary>Pins <c>shaft</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFit WithShaft(Dimension value) {
        AnyFit copy = new AnyFit(this);

        copy._shaft = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>shaft</c> is drawn from — <c>AnyValidShaft()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDimension, AnyDimension&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFit WithShaft(Func<AnyDimension, AnyDimension> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFit copy = new AnyFit(this);

        copy._shaftRecipe = configure(_shaftRecipe);
        copy._shaft       = copy._shaftRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Fit" />.</summary>
    public Fit Generate() {
        // A fit pairs two features of the same nominal size: the shaft keeps its drawn tolerance and unit,
        // but takes the hole's size (a pin on the shaft's nominal is therefore not honoured).
        Dimension hole       = _hole.Generate();
        Dimension drawnShaft = _shaft.Generate();
        double    shaftSize  = drawnShaft.Unit == LengthUnit.Inch ? hole.NominalInMillimetres / Dimension.MillimetresPerInch : hole.NominalInMillimetres;
        Dimension shaft      = new Dimension(shaftSize, drawnShaft.Unit, drawnShaft.Tolerance);

        return new Fit(hole, shaft);
    }

}
