// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IrrigationPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IrrigationPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIrrigationPolicy : IAny<IrrigationPolicy> {

    private readonly IAny<double> _dryThresholdPercent;
    private readonly IAny<int>    _minimumReadings;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIrrigationPolicy()
        : this(dryThresholdPercent: AnyValidDryThresholdPercent(),
               minimumReadings:     AnyValidMinimumReadings()) { }

    private static AnyDouble AnyValidDryThresholdPercent() {
        return Any.Double().Between(0d, 100d);
    }

    private static AnyInt32 AnyValidMinimumReadings() {
        return Any.Int32().Positive();
    }

    private AnyIrrigationPolicy(IAny<double> dryThresholdPercent,
                                IAny<int>    minimumReadings) {
        _dryThresholdPercent = dryThresholdPercent;
        _minimumReadings     = minimumReadings;
    }

    /// <summary>Pins <c>dryThresholdPercent</c> to a fixed value.</summary>
    public AnyIrrigationPolicy WithDryThresholdPercent(double value) {
        return new AnyIrrigationPolicy(new FixedValue<double>(value), _minimumReadings);
    }

    /// <summary>Draws <c>dryThresholdPercent</c> from <c>AnyValidDryThresholdPercent()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIrrigationPolicy WithDryThresholdPercent(Func<AnyDouble, AnyDouble> configure) {
        return new AnyIrrigationPolicy(configure(AnyValidDryThresholdPercent()), _minimumReadings);
    }

    /// <summary>Pins <c>minimumReadings</c> to a fixed value.</summary>
    public AnyIrrigationPolicy WithMinimumReadings(int value) {
        return new AnyIrrigationPolicy(_dryThresholdPercent, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>minimumReadings</c> from <c>AnyValidMinimumReadings()</c>, refined by <paramref name="configure" />.</summary>
    public AnyIrrigationPolicy WithMinimumReadings(Func<AnyInt32, AnyInt32> configure) {
        return new AnyIrrigationPolicy(_dryThresholdPercent, configure(AnyValidMinimumReadings()));
    }

    /// <summary>Produces one arbitrary <see cref="IrrigationPolicy" />.</summary>
    public IrrigationPolicy Generate() {
        return new IrrigationPolicy(_dryThresholdPercent.Generate(),
                                    _minimumReadings.Generate());
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
