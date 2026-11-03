// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BoundaryCondition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BoundaryCondition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBoundaryCondition : IAny<BoundaryCondition> {

    private IAny<Pressure>    _farFieldPressure;
    private IAny<Temperature> _farFieldTemperature;
    private IAny<double>      _transmissibilityMultiplier;

    private AnyPressure    _farFieldPressureRecipe;
    private AnyTemperature _farFieldTemperatureRecipe;
    private AnyDouble      _transmissibilityMultiplierRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBoundaryCondition()
        : this(farFieldPressure:           AnyValidFarFieldPressure(),
               farFieldTemperature:        AnyValidFarFieldTemperature(),
               transmissibilityMultiplier: AnyValidTransmissibilityMultiplier()) { }

    private static AnyPressure AnyValidFarFieldPressure() {
        return new AnyPressure();
    }

    private static AnyTemperature AnyValidFarFieldTemperature() {
        return new AnyTemperature();
    }

    private static AnyDouble AnyValidTransmissibilityMultiplier() {
        return Any.Double().Between(0d, BoundaryCondition.MaximumMultiplier);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBoundaryCondition(AnyPressure    farFieldPressure,
                                 AnyTemperature farFieldTemperature,
                                 AnyDouble      transmissibilityMultiplier) {
        _farFieldPressure                 = farFieldPressure;
        _farFieldPressureRecipe           = farFieldPressure;
        _farFieldTemperature              = farFieldTemperature;
        _farFieldTemperatureRecipe        = farFieldTemperature;
        _transmissibilityMultiplier       = transmissibilityMultiplier;
        _transmissibilityMultiplierRecipe = transmissibilityMultiplier;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBoundaryCondition(AnyBoundaryCondition from) {
        _farFieldPressure                 = from._farFieldPressure;
        _farFieldPressureRecipe           = from._farFieldPressureRecipe;
        _farFieldTemperature              = from._farFieldTemperature;
        _farFieldTemperatureRecipe        = from._farFieldTemperatureRecipe;
        _transmissibilityMultiplier       = from._transmissibilityMultiplier;
        _transmissibilityMultiplierRecipe = from._transmissibilityMultiplierRecipe;
    }

    /// <summary>Pins <c>farFieldPressure</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBoundaryCondition WithFarFieldPressure(Pressure value) {
        AnyBoundaryCondition copy = new AnyBoundaryCondition(this);

        copy._farFieldPressure = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>farFieldPressure</c> is drawn from — <c>AnyValidFarFieldPressure()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPressure, AnyPressure&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBoundaryCondition WithFarFieldPressure(Func<AnyPressure, AnyPressure> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBoundaryCondition copy = new AnyBoundaryCondition(this);

        copy._farFieldPressureRecipe = configure(_farFieldPressureRecipe);
        copy._farFieldPressure       = copy._farFieldPressureRecipe;

        return copy;
    }

    /// <summary>Pins <c>farFieldTemperature</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBoundaryCondition WithFarFieldTemperature(Temperature value) {
        AnyBoundaryCondition copy = new AnyBoundaryCondition(this);

        copy._farFieldTemperature = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>farFieldTemperature</c> is drawn from — <c>AnyValidFarFieldTemperature()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperature, AnyTemperature&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBoundaryCondition WithFarFieldTemperature(Func<AnyTemperature, AnyTemperature> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBoundaryCondition copy = new AnyBoundaryCondition(this);

        copy._farFieldTemperatureRecipe = configure(_farFieldTemperatureRecipe);
        copy._farFieldTemperature       = copy._farFieldTemperatureRecipe;

        return copy;
    }

    /// <summary>Pins <c>transmissibilityMultiplier</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBoundaryCondition WithTransmissibilityMultiplier(double value) {
        AnyBoundaryCondition copy = new AnyBoundaryCondition(this);

        copy._transmissibilityMultiplier = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>transmissibilityMultiplier</c> is drawn from — <c>AnyValidTransmissibilityMultiplier()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBoundaryCondition WithTransmissibilityMultiplier(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBoundaryCondition copy = new AnyBoundaryCondition(this);

        copy._transmissibilityMultiplierRecipe = configure(_transmissibilityMultiplierRecipe);
        copy._transmissibilityMultiplier       = copy._transmissibilityMultiplierRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BoundaryCondition" />.</summary>
    public BoundaryCondition Generate() {
        return new BoundaryCondition(_farFieldPressure.Generate(),
                                     _farFieldTemperature.Generate(),
                                     _transmissibilityMultiplier.Generate());
    }

}
