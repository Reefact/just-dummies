// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Weight --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using Freight.Domain;
using JustDummies;

namespace Freight.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Weight" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWeight : IAny<Weight> {

    private IAny<double> _kilograms;

    private AnyDouble _kilogramsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWeight()
        : this(kilograms: AnyValidKilograms()) { }

    private static AnyDouble AnyValidKilograms() {
        return Any.Double().Positive().LessThanOrEqualTo(Weight.MaxGrossKilograms);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyWeight(AnyDouble kilograms) {
        _kilograms       = kilograms;
        _kilogramsRecipe = kilograms;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyWeight(AnyWeight from) {
        _kilograms       = from._kilograms;
        _kilogramsRecipe = from._kilogramsRecipe;
    }

    /// <summary>Pins <c>kilograms</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWeight WithKilograms(double value) {
        AnyWeight copy = new AnyWeight(this);

        copy._kilograms = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kilograms</c> is drawn from — <c>AnyValidKilograms()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWeight WithKilograms(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWeight copy = new AnyWeight(this);

        copy._kilogramsRecipe = configure(_kilogramsRecipe);
        copy._kilograms       = copy._kilogramsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Weight" />.</summary>
    public Weight Generate() {
        return new Weight(_kilograms.Generate());
    }

}
