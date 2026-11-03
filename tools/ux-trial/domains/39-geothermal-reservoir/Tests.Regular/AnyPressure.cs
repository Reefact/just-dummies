// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Pressure --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Pressure" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPressure : IAny<Pressure> {

    private IAny<double> _pascals;

    private AnyDouble _pascalsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPressure()
        : this(pascals: AnyValidPascals()) { }

    private static AnyDouble AnyValidPascals() {
        return Any.Double().Positive().LessThanOrEqualTo(Pressure.MaximumPascals);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPressure(AnyDouble pascals) {
        _pascals       = pascals;
        _pascalsRecipe = pascals;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPressure(AnyPressure from) {
        _pascals       = from._pascals;
        _pascalsRecipe = from._pascalsRecipe;
    }

    /// <summary>Pins <c>pascals</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPressure WithPascals(double value) {
        AnyPressure copy = new AnyPressure(this);

        copy._pascals = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pascals</c> is drawn from — <c>AnyValidPascals()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPressure WithPascals(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPressure copy = new AnyPressure(this);

        copy._pascalsRecipe = configure(_pascalsRecipe);
        copy._pascals       = copy._pascalsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Pressure" />.</summary>
    public Pressure Generate() {
        return new Pressure(_pascals.Generate());
    }

}
