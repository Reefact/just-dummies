// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Concentration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="Concentration" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyConcentration : IAny<Concentration> {

    private IAny<double> _cellsPerMillilitre;

    private AnyDouble _cellsPerMillilitreRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyConcentration()
        : this(cellsPerMillilitre: AnyValidCellsPerMillilitre()) { }

    private static AnyDouble AnyValidCellsPerMillilitre() {
        return Any.Double().Between(0d, Concentration.MaxCellsPerMillilitre);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyConcentration(AnyDouble cellsPerMillilitre) {
        _cellsPerMillilitre       = cellsPerMillilitre;
        _cellsPerMillilitreRecipe = cellsPerMillilitre;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyConcentration(AnyConcentration from) {
        _cellsPerMillilitre       = from._cellsPerMillilitre;
        _cellsPerMillilitreRecipe = from._cellsPerMillilitreRecipe;
    }

    /// <summary>Pins <c>cellsPerMillilitre</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyConcentration WithCellsPerMillilitre(double value) {
        AnyConcentration copy = new AnyConcentration(this);

        copy._cellsPerMillilitre = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cellsPerMillilitre</c> is drawn from — <c>AnyValidCellsPerMillilitre()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyConcentration WithCellsPerMillilitre(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyConcentration copy = new AnyConcentration(this);

        copy._cellsPerMillilitreRecipe = configure(_cellsPerMillilitreRecipe);
        copy._cellsPerMillilitre       = copy._cellsPerMillilitreRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Concentration" />.</summary>
    public Concentration Generate() {
        return new Concentration(_cellsPerMillilitre.Generate());
    }

}
