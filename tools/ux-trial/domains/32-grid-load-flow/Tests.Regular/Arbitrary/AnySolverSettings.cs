// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SolverSettings --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SolverSettings" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySolverSettings : IAny<SolverSettings> {

    private readonly IAny<double> _tolerancePu;
    private readonly IAny<int>    _maximumIterations;
    private readonly IAny<double> _accelerationFactor;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySolverSettings()
        : this(tolerancePu:        AnyValidTolerancePu(),
               maximumIterations:  AnyValidMaximumIterations(),
               accelerationFactor: AnyValidAccelerationFactor()) { }

    private static AnyDouble AnyValidTolerancePu() {
        return Any.Double().Between(1e-12, 1e-2);
    }

    private static AnyInt32 AnyValidMaximumIterations() {
        return Any.Int32().Between(1, 10_000);
    }

    private static AnyDouble AnyValidAccelerationFactor() {
        return Any.Double().GreaterThanOrEqualTo(1.0).LessThan(2.0);
    }

    private AnySolverSettings(IAny<double> tolerancePu,
                              IAny<int>    maximumIterations,
                              IAny<double> accelerationFactor) {
        _tolerancePu        = tolerancePu;
        _maximumIterations  = maximumIterations;
        _accelerationFactor = accelerationFactor;
    }

    /// <summary>Pins <c>tolerancePu</c> to a fixed value.</summary>
    public AnySolverSettings WithTolerancePu(double value) {
        return new AnySolverSettings(new FixedValue<double>(value), _maximumIterations, _accelerationFactor);
    }

    /// <summary>Draws <c>tolerancePu</c> from <c>AnyValidTolerancePu()</c>, refined by <paramref name="configure" />.</summary>
    public AnySolverSettings WithTolerancePu(Func<AnyDouble, AnyDouble> configure) {
        return new AnySolverSettings(configure(AnyValidTolerancePu()), _maximumIterations, _accelerationFactor);
    }

    /// <summary>Pins <c>maximumIterations</c> to a fixed value.</summary>
    public AnySolverSettings WithMaximumIterations(int value) {
        return new AnySolverSettings(_tolerancePu, new FixedValue<int>(value), _accelerationFactor);
    }

    /// <summary>Draws <c>maximumIterations</c> from <c>AnyValidMaximumIterations()</c>, refined by <paramref name="configure" />.</summary>
    public AnySolverSettings WithMaximumIterations(Func<AnyInt32, AnyInt32> configure) {
        return new AnySolverSettings(_tolerancePu, configure(AnyValidMaximumIterations()), _accelerationFactor);
    }

    /// <summary>Pins <c>accelerationFactor</c> to a fixed value.</summary>
    public AnySolverSettings WithAccelerationFactor(double value) {
        return new AnySolverSettings(_tolerancePu, _maximumIterations, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>accelerationFactor</c> from <c>AnyValidAccelerationFactor()</c>, refined by <paramref name="configure" />.</summary>
    public AnySolverSettings WithAccelerationFactor(Func<AnyDouble, AnyDouble> configure) {
        return new AnySolverSettings(_tolerancePu, _maximumIterations, configure(AnyValidAccelerationFactor()));
    }

    /// <summary>Produces one arbitrary <see cref="SolverSettings" />.</summary>
    public SolverSettings Generate() {
        return new SolverSettings(_tolerancePu.Generate(),
                                  _maximumIterations.Generate(),
                                  _accelerationFactor.Generate());
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
