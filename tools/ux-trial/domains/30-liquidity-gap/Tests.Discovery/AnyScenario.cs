// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Scenario --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Scenario" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyScenario : IAny<Scenario> {

    private readonly IAny<string>  _name;
    private readonly IAny<decimal> _inflowHaircut;
    private readonly IAny<decimal> _outflowMultiplier;
    private readonly IAny<decimal> _depositStableShareHaircut;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyScenario()
        : this(name:                      AnyValidName(),
               inflowHaircut:             AnyValidInflowHaircut(),
               outflowMultiplier:         AnyValidOutflowMultiplier(),
               depositStableShareHaircut: AnyValidDepositStableShareHaircut()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(64);
    }

    private static AnyDecimal AnyValidInflowHaircut() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).LessThan(1m);
    }

    private static AnyDecimal AnyValidOutflowMultiplier() {
        return Any.Decimal().Between(1m, 3m);
    }

    private static AnyDecimal AnyValidDepositStableShareHaircut() {
        return Any.Decimal().Between(0m, 1m);
    }

    private AnyScenario(IAny<string>  name,
                        IAny<decimal> inflowHaircut,
                        IAny<decimal> outflowMultiplier,
                        IAny<decimal> depositStableShareHaircut) {
        _name                      = name;
        _inflowHaircut             = inflowHaircut;
        _outflowMultiplier         = outflowMultiplier;
        _depositStableShareHaircut = depositStableShareHaircut;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyScenario WithName(string value) {
        return new AnyScenario(new FixedValue<string>(value), _inflowHaircut, _outflowMultiplier, _depositStableShareHaircut);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyScenario WithName(Func<AnyString, AnyString> configure) {
        return new AnyScenario(configure(AnyValidName()), _inflowHaircut, _outflowMultiplier, _depositStableShareHaircut);
    }

    /// <summary>Pins <c>inflowHaircut</c> to a fixed value.</summary>
    public AnyScenario WithInflowHaircut(decimal value) {
        return new AnyScenario(_name, new FixedValue<decimal>(value), _outflowMultiplier, _depositStableShareHaircut);
    }

    /// <summary>Draws <c>inflowHaircut</c> from <c>AnyValidInflowHaircut()</c>, refined by <paramref name="configure" />.</summary>
    public AnyScenario WithInflowHaircut(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyScenario(_name, configure(AnyValidInflowHaircut()), _outflowMultiplier, _depositStableShareHaircut);
    }

    /// <summary>Pins <c>outflowMultiplier</c> to a fixed value.</summary>
    public AnyScenario WithOutflowMultiplier(decimal value) {
        return new AnyScenario(_name, _inflowHaircut, new FixedValue<decimal>(value), _depositStableShareHaircut);
    }

    /// <summary>Draws <c>outflowMultiplier</c> from <c>AnyValidOutflowMultiplier()</c>, refined by <paramref name="configure" />.</summary>
    public AnyScenario WithOutflowMultiplier(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyScenario(_name, _inflowHaircut, configure(AnyValidOutflowMultiplier()), _depositStableShareHaircut);
    }

    /// <summary>Pins <c>depositStableShareHaircut</c> to a fixed value.</summary>
    public AnyScenario WithDepositStableShareHaircut(decimal value) {
        return new AnyScenario(_name, _inflowHaircut, _outflowMultiplier, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>depositStableShareHaircut</c> from <c>AnyValidDepositStableShareHaircut()</c>, refined by <paramref name="configure" />.</summary>
    public AnyScenario WithDepositStableShareHaircut(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyScenario(_name, _inflowHaircut, _outflowMultiplier, configure(AnyValidDepositStableShareHaircut()));
    }

    /// <summary>Produces one arbitrary <see cref="Scenario" />.</summary>
    public Scenario Generate() {
        return new Scenario(_name.Generate(),
                            _inflowHaircut.Generate(),
                            _outflowMultiplier.Generate(),
                            _depositStableShareHaircut.Generate());
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
