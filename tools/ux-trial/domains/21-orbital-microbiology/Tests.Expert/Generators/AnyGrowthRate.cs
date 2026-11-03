// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GrowthRate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="GrowthRate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGrowthRate : IAny<GrowthRate> {

    private IAny<double> _perHour;

    private AnyDouble _perHourRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGrowthRate()
        : this(perHour: AnyValidPerHour()) { }

    private static AnyDouble AnyValidPerHour() {
        return Any.Double().Between(0d, GrowthRate.MaxPerHour);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGrowthRate(AnyDouble perHour) {
        _perHour       = perHour;
        _perHourRecipe = perHour;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGrowthRate(AnyGrowthRate from) {
        _perHour       = from._perHour;
        _perHourRecipe = from._perHourRecipe;
    }

    /// <summary>Pins <c>perHour</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGrowthRate WithPerHour(double value) {
        AnyGrowthRate copy = new AnyGrowthRate(this);

        copy._perHour = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>perHour</c> is drawn from — <c>AnyValidPerHour()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGrowthRate WithPerHour(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGrowthRate copy = new AnyGrowthRate(this);

        copy._perHourRecipe = configure(_perHourRecipe);
        copy._perHour       = copy._perHourRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="GrowthRate" />.</summary>
    public GrowthRate Generate() {
        return new GrowthRate(_perHour.Generate());
    }

}
