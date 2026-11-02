// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Surface --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Surface" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySurface : IAny<Surface> {

    private IAny<double> _squareMeters;

    private IAny<double> _squareMetersRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySurface()
        : this(squareMeters: AnyValidSquareMeters()) { }

    private static IAny<double> AnyValidSquareMeters() {
        // A surface is measured to the hundredth: draw a whole number of hundredths, up to the registry maximum.
        return Any.Int32().Between(1, (int)(Surface.MaxSquareMeters * 100)).As(hundredths => hundredths / 100d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySurface(IAny<double> squareMeters) {
        _squareMeters       = squareMeters;
        _squareMetersRecipe = squareMeters;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySurface(AnySurface from) {
        _squareMeters       = from._squareMeters;
        _squareMetersRecipe = from._squareMetersRecipe;
    }

    /// <summary>Pins <c>squareMeters</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySurface WithSquareMeters(double value) {
        AnySurface copy = new AnySurface(this);

        copy._squareMeters = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>squareMeters</c> is drawn from — <c>AnyValidSquareMeters()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny&lt;double&gt;, IAny&lt;double&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySurface WithSquareMeters(Func<IAny<double>, IAny<double>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySurface copy = new AnySurface(this);

        copy._squareMetersRecipe = configure(_squareMetersRecipe);
        copy._squareMeters       = copy._squareMetersRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Surface" />.</summary>
    public Surface Generate() {
        return new Surface(_squareMeters.Generate());
    }

}
