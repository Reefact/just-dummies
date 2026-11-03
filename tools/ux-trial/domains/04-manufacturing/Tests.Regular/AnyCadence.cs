// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Cadence --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Cadence" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCadence : IAny<Cadence> {

    private IAny<int> _unitsPerHour;

    private AnyInt32 _unitsPerHourRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCadence()
        : this(unitsPerHour: AnyValidUnitsPerHour()) { }

    private static AnyInt32 AnyValidUnitsPerHour() {
        return Any.Int32().Positive().LessThanOrEqualTo(Cadence.MaximumUnitsPerHour);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCadence(AnyInt32 unitsPerHour) {
        _unitsPerHour       = unitsPerHour;
        _unitsPerHourRecipe = unitsPerHour;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCadence(AnyCadence from) {
        _unitsPerHour       = from._unitsPerHour;
        _unitsPerHourRecipe = from._unitsPerHourRecipe;
    }

    /// <summary>Pins <c>unitsPerHour</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCadence WithUnitsPerHour(int value) {
        AnyCadence copy = new AnyCadence(this);

        copy._unitsPerHour = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unitsPerHour</c> is drawn from — <c>AnyValidUnitsPerHour()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCadence WithUnitsPerHour(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCadence copy = new AnyCadence(this);

        copy._unitsPerHourRecipe = configure(_unitsPerHourRecipe);
        copy._unitsPerHour       = copy._unitsPerHourRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Cadence" />.</summary>
    public Cadence Generate() {
        return new Cadence(_unitsPerHour.Generate());
    }

}
