// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tolerance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Tolerance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTolerance : IAny<Tolerance> {

    private IAny<double> _percent;

    private AnyDouble _percentRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTolerance()
        : this(percent: AnyValidPercent()) { }

    private static AnyDouble AnyValidPercent() {
        return Any.Double().Between(Tolerance.MinPercent, Tolerance.MaxPercent);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTolerance(AnyDouble percent) {
        _percent       = percent;
        _percentRecipe = percent;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTolerance(AnyTolerance from) {
        _percent       = from._percent;
        _percentRecipe = from._percentRecipe;
    }

    /// <summary>Pins <c>percent</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTolerance WithPercent(double value) {
        AnyTolerance copy = new AnyTolerance(this);

        copy._percent = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>percent</c> is drawn from — <c>AnyValidPercent()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTolerance WithPercent(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTolerance copy = new AnyTolerance(this);

        copy._percentRecipe = configure(_percentRecipe);
        copy._percent       = copy._percentRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Tolerance" />.</summary>
    public Tolerance Generate() {
        return new Tolerance(_percent.Generate());
    }

}
