// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Permeability --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Permeability" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPermeability : IAny<Permeability> {

    private IAny<double> _squareMetres;

    private AnyDouble _squareMetresRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPermeability()
        : this(squareMetres: AnyValidSquareMetres()) { }

    private static AnyDouble AnyValidSquareMetres() {
        return Any.Double().Between(Permeability.MinimumSquareMetres, Permeability.MaximumSquareMetres);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPermeability(AnyDouble squareMetres) {
        _squareMetres       = squareMetres;
        _squareMetresRecipe = squareMetres;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPermeability(AnyPermeability from) {
        _squareMetres       = from._squareMetres;
        _squareMetresRecipe = from._squareMetresRecipe;
    }

    /// <summary>Pins <c>squareMetres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPermeability WithSquareMetres(double value) {
        AnyPermeability copy = new AnyPermeability(this);

        copy._squareMetres = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>squareMetres</c> is drawn from — <c>AnyValidSquareMetres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPermeability WithSquareMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPermeability copy = new AnyPermeability(this);

        copy._squareMetresRecipe = configure(_squareMetresRecipe);
        copy._squareMetres       = copy._squareMetresRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Permeability" />.</summary>
    public Permeability Generate() {
        return new Permeability(_squareMetres.Generate());
    }

}
