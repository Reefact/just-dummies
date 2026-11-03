// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SolverSettings --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GridLoadFlow.Domain;
using JustDummies;

namespace GridLoadFlow.Tests.Regular;

/// <summary>
///     A generator of arbitrary <see cref="SolverSettings" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySolverSettings : IAny<SolverSettings> {

    private IAny<double> _tolerancePu;
    private IAny<int>    _maximumIterations;
    private IAny<double> _accelerationFactor;

    private AnyDouble _tolerancePuRecipe;
    private AnyInt32  _maximumIterationsRecipe;
    private AnyDouble _accelerationFactorRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySolverSettings()
        : this(tolerancePu:        AnyValidTolerancePu(),
               maximumIterations:  AnyValidMaximumIterations(),
               accelerationFactor: AnyValidAccelerationFactor()) { }

    private static AnyDouble AnyValidTolerancePu() {
        return Any.Double().Between(1E-12d, 0.01d);
    }

    private static AnyInt32 AnyValidMaximumIterations() {
        return Any.Int32().Positive().LessThanOrEqualTo(10000);
    }

    private static AnyDouble AnyValidAccelerationFactor() {
        return Any.Double().GreaterThanOrEqualTo(1d).LessThan(2d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySolverSettings(AnyDouble tolerancePu,
                              AnyInt32  maximumIterations,
                              AnyDouble accelerationFactor) {
        _tolerancePu              = tolerancePu;
        _tolerancePuRecipe        = tolerancePu;
        _maximumIterations        = maximumIterations;
        _maximumIterationsRecipe  = maximumIterations;
        _accelerationFactor       = accelerationFactor;
        _accelerationFactorRecipe = accelerationFactor;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySolverSettings(AnySolverSettings from) {
        _tolerancePu              = from._tolerancePu;
        _tolerancePuRecipe        = from._tolerancePuRecipe;
        _maximumIterations        = from._maximumIterations;
        _maximumIterationsRecipe  = from._maximumIterationsRecipe;
        _accelerationFactor       = from._accelerationFactor;
        _accelerationFactorRecipe = from._accelerationFactorRecipe;
    }

    /// <summary>Pins <c>tolerancePu</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySolverSettings WithTolerancePu(double value) {
        AnySolverSettings copy = new AnySolverSettings(this);

        copy._tolerancePu = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tolerancePu</c> is drawn from — <c>AnyValidTolerancePu()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySolverSettings WithTolerancePu(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySolverSettings copy = new AnySolverSettings(this);

        copy._tolerancePuRecipe = configure(_tolerancePuRecipe);
        copy._tolerancePu       = copy._tolerancePuRecipe;

        return copy;
    }

    /// <summary>Pins <c>maximumIterations</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySolverSettings WithMaximumIterations(int value) {
        AnySolverSettings copy = new AnySolverSettings(this);

        copy._maximumIterations = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maximumIterations</c> is drawn from — <c>AnyValidMaximumIterations()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySolverSettings WithMaximumIterations(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySolverSettings copy = new AnySolverSettings(this);

        copy._maximumIterationsRecipe = configure(_maximumIterationsRecipe);
        copy._maximumIterations       = copy._maximumIterationsRecipe;

        return copy;
    }

    /// <summary>Pins <c>accelerationFactor</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySolverSettings WithAccelerationFactor(double value) {
        AnySolverSettings copy = new AnySolverSettings(this);

        copy._accelerationFactor = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>accelerationFactor</c> is drawn from — <c>AnyValidAccelerationFactor()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySolverSettings WithAccelerationFactor(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySolverSettings copy = new AnySolverSettings(this);

        copy._accelerationFactorRecipe = configure(_accelerationFactorRecipe);
        copy._accelerationFactor       = copy._accelerationFactorRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SolverSettings" />.</summary>
    public SolverSettings Generate() {
        return new SolverSettings(_tolerancePu.Generate(),
                                  _maximumIterations.Generate(),
                                  _accelerationFactor.Generate());
    }

}
