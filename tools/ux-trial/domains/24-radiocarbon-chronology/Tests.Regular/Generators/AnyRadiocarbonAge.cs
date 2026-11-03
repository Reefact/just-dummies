// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RadiocarbonAge --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="RadiocarbonAge" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRadiocarbonAge : IAny<RadiocarbonAge> {

    private IAny<int> _yearsBp;
    private IAny<int> _uncertainty;

    private AnyInt32 _yearsBpRecipe;
    private AnyInt32 _uncertaintyRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRadiocarbonAge()
        : this(yearsBp:     AnyValidYearsBp(),
               uncertainty: AnyValidUncertainty()) { }

    private static AnyInt32 AnyValidYearsBp() {
        return Any.Int32().Between(0, RadiocarbonAge.MaximumYearsBp);
    }

    private static AnyInt32 AnyValidUncertainty() {
        return Any.Int32().Positive().LessThanOrEqualTo(RadiocarbonAge.MaximumUncertainty);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRadiocarbonAge(AnyInt32 yearsBp,
                              AnyInt32 uncertainty) {
        _yearsBp           = yearsBp;
        _yearsBpRecipe     = yearsBp;
        _uncertainty       = uncertainty;
        _uncertaintyRecipe = uncertainty;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRadiocarbonAge(AnyRadiocarbonAge from) {
        _yearsBp           = from._yearsBp;
        _yearsBpRecipe     = from._yearsBpRecipe;
        _uncertainty       = from._uncertainty;
        _uncertaintyRecipe = from._uncertaintyRecipe;
    }

    /// <summary>Pins <c>yearsBp</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRadiocarbonAge WithYearsBp(int value) {
        AnyRadiocarbonAge copy = new AnyRadiocarbonAge(this);

        copy._yearsBp = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>yearsBp</c> is drawn from — <c>AnyValidYearsBp()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRadiocarbonAge WithYearsBp(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRadiocarbonAge copy = new AnyRadiocarbonAge(this);

        copy._yearsBpRecipe = configure(_yearsBpRecipe);
        copy._yearsBp       = copy._yearsBpRecipe;

        return copy;
    }

    /// <summary>Pins <c>uncertainty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRadiocarbonAge WithUncertainty(int value) {
        AnyRadiocarbonAge copy = new AnyRadiocarbonAge(this);

        copy._uncertainty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>uncertainty</c> is drawn from — <c>AnyValidUncertainty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRadiocarbonAge WithUncertainty(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRadiocarbonAge copy = new AnyRadiocarbonAge(this);

        copy._uncertaintyRecipe = configure(_uncertaintyRecipe);
        copy._uncertainty       = copy._uncertaintyRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RadiocarbonAge" />.</summary>
    public RadiocarbonAge Generate() {
        return new RadiocarbonAge(_yearsBp.Generate(),
                                  _uncertainty.Generate());
    }

}
