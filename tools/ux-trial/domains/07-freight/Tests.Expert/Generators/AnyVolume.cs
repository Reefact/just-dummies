// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Volume --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using Freight.Domain;
using JustDummies;

namespace Freight.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Volume" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVolume : IAny<Volume> {

    private IAny<double> _cubicMetres;

    private AnyDouble _cubicMetresRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVolume()
        : this(cubicMetres: AnyValidCubicMetres()) { }

    private static AnyDouble AnyValidCubicMetres() {
        return Any.Double().Positive().LessThanOrEqualTo(Volume.MaxCubicMetres);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVolume(AnyDouble cubicMetres) {
        _cubicMetres       = cubicMetres;
        _cubicMetresRecipe = cubicMetres;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVolume(AnyVolume from) {
        _cubicMetres       = from._cubicMetres;
        _cubicMetresRecipe = from._cubicMetresRecipe;
    }

    /// <summary>Pins <c>cubicMetres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVolume WithCubicMetres(double value) {
        AnyVolume copy = new AnyVolume(this);

        copy._cubicMetres = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cubicMetres</c> is drawn from — <c>AnyValidCubicMetres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVolume WithCubicMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVolume copy = new AnyVolume(this);

        copy._cubicMetresRecipe = configure(_cubicMetresRecipe);
        copy._cubicMetres       = copy._cubicMetresRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Volume" />.</summary>
    public Volume Generate() {
        return new Volume(_cubicMetres.Generate());
    }

}
