// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Impedance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Impedance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyImpedance : IAny<Impedance> {

    private readonly IAny<double> _resistancePu;
    private readonly IAny<double> _reactancePu;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyImpedance()
        : this(resistancePu: AnyValidResistancePu(),
               reactancePu:  AnyValidReactancePu()) { }

    private static AnyDouble AnyValidResistancePu() {
        return Any.Double().Between(0.0, Impedance.MaximumPu);
    }

    private static AnyDouble AnyValidReactancePu() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Impedance.MaximumPu);
    }

    private AnyImpedance(IAny<double> resistancePu,
                         IAny<double> reactancePu) {
        _resistancePu = resistancePu;
        _reactancePu  = reactancePu;
    }

    /// <summary>Pins <c>resistancePu</c> to a fixed value.</summary>
    public AnyImpedance WithResistancePu(double value) {
        return new AnyImpedance(new FixedValue<double>(value), _reactancePu);
    }

    /// <summary>Draws <c>resistancePu</c> from <c>AnyValidResistancePu()</c>, refined by <paramref name="configure" />.</summary>
    public AnyImpedance WithResistancePu(Func<AnyDouble, AnyDouble> configure) {
        return new AnyImpedance(configure(AnyValidResistancePu()), _reactancePu);
    }

    /// <summary>Pins <c>reactancePu</c> to a fixed value.</summary>
    public AnyImpedance WithReactancePu(double value) {
        return new AnyImpedance(_resistancePu, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>reactancePu</c> from <c>AnyValidReactancePu()</c>, refined by <paramref name="configure" />.</summary>
    public AnyImpedance WithReactancePu(Func<AnyDouble, AnyDouble> configure) {
        return new AnyImpedance(_resistancePu, configure(AnyValidReactancePu()));
    }

    /// <summary>Produces one arbitrary <see cref="Impedance" />.</summary>
    public Impedance Generate() {
        return new Impedance(_resistancePu.Generate(),
                             _reactancePu.Generate());
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
