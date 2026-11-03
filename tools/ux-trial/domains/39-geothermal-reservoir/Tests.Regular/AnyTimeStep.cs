// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeStep --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TimeStep" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTimeStep : IAny<TimeStep> {

    private IAny<double> _seconds;

    private AnyDouble _secondsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTimeStep()
        : this(seconds: AnyValidSeconds()) { }

    private static AnyDouble AnyValidSeconds() {
        return Any.Double().Positive().LessThanOrEqualTo(TimeStep.MaximumSeconds);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTimeStep(AnyDouble seconds) {
        _seconds       = seconds;
        _secondsRecipe = seconds;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTimeStep(AnyTimeStep from) {
        _seconds       = from._seconds;
        _secondsRecipe = from._secondsRecipe;
    }

    /// <summary>Pins <c>seconds</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTimeStep WithSeconds(double value) {
        AnyTimeStep copy = new AnyTimeStep(this);

        copy._seconds = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>seconds</c> is drawn from — <c>AnyValidSeconds()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTimeStep WithSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTimeStep copy = new AnyTimeStep(this);

        copy._secondsRecipe = configure(_secondsRecipe);
        copy._seconds       = copy._secondsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TimeStep" />.</summary>
    public TimeStep Generate() {
        return new TimeStep(_seconds.Generate());
    }

}
