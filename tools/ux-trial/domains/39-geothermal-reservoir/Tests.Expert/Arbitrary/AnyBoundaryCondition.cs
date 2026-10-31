// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BoundaryCondition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="BoundaryCondition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBoundaryCondition : IAny<BoundaryCondition> {

    private readonly IAny<Pressure>    _farFieldPressure;
    private readonly IAny<Temperature> _farFieldTemperature;
    private readonly IAny<double>      _transmissibilityMultiplier;

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
        // 0 is a sealed boundary, 1 a fully open one.
        return Any.Double().Between(0.0, BoundaryCondition.MaximumMultiplier);
    }

    private AnyBoundaryCondition(IAny<Pressure>    farFieldPressure,
                                 IAny<Temperature> farFieldTemperature,
                                 IAny<double>      transmissibilityMultiplier) {
        _farFieldPressure           = farFieldPressure;
        _farFieldTemperature        = farFieldTemperature;
        _transmissibilityMultiplier = transmissibilityMultiplier;
    }

    /// <summary>Pins <c>farFieldPressure</c> to a fixed value.</summary>
    public AnyBoundaryCondition WithFarFieldPressure(Pressure value) {
        return new AnyBoundaryCondition(new FixedValue<Pressure>(value), _farFieldTemperature, _transmissibilityMultiplier);
    }

    /// <summary>Draws <c>farFieldPressure</c> from <c>AnyValidFarFieldPressure()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBoundaryCondition WithFarFieldPressure(Func<AnyPressure, AnyPressure> configure) {
        return new AnyBoundaryCondition(configure(AnyValidFarFieldPressure()), _farFieldTemperature, _transmissibilityMultiplier);
    }

    /// <summary>Pins <c>farFieldTemperature</c> to a fixed value.</summary>
    public AnyBoundaryCondition WithFarFieldTemperature(Temperature value) {
        return new AnyBoundaryCondition(_farFieldPressure, new FixedValue<Temperature>(value), _transmissibilityMultiplier);
    }

    /// <summary>Draws <c>farFieldTemperature</c> from <c>AnyValidFarFieldTemperature()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBoundaryCondition WithFarFieldTemperature(Func<AnyTemperature, AnyTemperature> configure) {
        return new AnyBoundaryCondition(_farFieldPressure, configure(AnyValidFarFieldTemperature()), _transmissibilityMultiplier);
    }

    /// <summary>Pins <c>transmissibilityMultiplier</c> to a fixed value.</summary>
    public AnyBoundaryCondition WithTransmissibilityMultiplier(double value) {
        return new AnyBoundaryCondition(_farFieldPressure, _farFieldTemperature, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>transmissibilityMultiplier</c> from <c>AnyValidTransmissibilityMultiplier()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBoundaryCondition WithTransmissibilityMultiplier(Func<AnyDouble, AnyDouble> configure) {
        return new AnyBoundaryCondition(_farFieldPressure, _farFieldTemperature, configure(AnyValidTransmissibilityMultiplier()));
    }

    /// <summary>Produces one arbitrary <see cref="BoundaryCondition" />.</summary>
    public BoundaryCondition Generate() {
        return new BoundaryCondition(_farFieldPressure.Generate(),
                                     _farFieldTemperature.Generate(),
                                     _transmissibilityMultiplier.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
