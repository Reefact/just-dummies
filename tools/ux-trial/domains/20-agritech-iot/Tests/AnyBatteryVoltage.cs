// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BatteryVoltage --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BatteryVoltage" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBatteryVoltage : IAny<BatteryVoltage> {

    private readonly IAny<Half> _volts;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBatteryVoltage()
        : this(volts: AnyValidVolts()) { }

    private static AnyHalf AnyValidVolts() {
        return Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum);
    }

    private AnyBatteryVoltage(IAny<Half> volts) {
        _volts = volts;
    }

    /// <summary>Pins <c>volts</c> to a fixed value.</summary>
    public AnyBatteryVoltage WithVolts(Half value) {
        return new AnyBatteryVoltage(new FixedValue<Half>(value));
    }

    /// <summary>Draws <c>volts</c> from <c>AnyValidVolts()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatteryVoltage WithVolts(Func<AnyHalf, AnyHalf> configure) {
        return new AnyBatteryVoltage(configure(AnyValidVolts()));
    }

    /// <summary>Produces one arbitrary <see cref="BatteryVoltage" />.</summary>
    public BatteryVoltage Generate() {
        return new BatteryVoltage(_volts.Generate());
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
