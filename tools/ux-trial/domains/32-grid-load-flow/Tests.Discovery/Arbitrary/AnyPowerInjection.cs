// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PowerInjection --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PowerInjection" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPowerInjection : IAny<PowerInjection> {

    private readonly IAny<double> _activeMw;
    private readonly IAny<double> _reactiveMvar;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPowerInjection()
        : this(activeMw:     AnyValidActiveMw(),
               reactiveMvar: AnyValidReactiveMvar()) { }

    private static AnyDouble AnyValidActiveMw() {
        return Any.Double().Between(-10_000.0, 10_000.0);
    }

    private static AnyDouble AnyValidReactiveMvar() {
        return Any.Double().Between(-10_000.0, 10_000.0);
    }

    private AnyPowerInjection(IAny<double> activeMw,
                              IAny<double> reactiveMvar) {
        _activeMw     = activeMw;
        _reactiveMvar = reactiveMvar;
    }

    /// <summary>Pins <c>activeMw</c> to a fixed value.</summary>
    public AnyPowerInjection WithActiveMw(double value) {
        return new AnyPowerInjection(new FixedValue<double>(value), _reactiveMvar);
    }

    /// <summary>Draws <c>activeMw</c> from <c>AnyValidActiveMw()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPowerInjection WithActiveMw(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPowerInjection(configure(AnyValidActiveMw()), _reactiveMvar);
    }

    /// <summary>Pins <c>reactiveMvar</c> to a fixed value.</summary>
    public AnyPowerInjection WithReactiveMvar(double value) {
        return new AnyPowerInjection(_activeMw, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>reactiveMvar</c> from <c>AnyValidReactiveMvar()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPowerInjection WithReactiveMvar(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPowerInjection(_activeMw, configure(AnyValidReactiveMvar()));
    }

    /// <summary>Produces one arbitrary <see cref="PowerInjection" />.</summary>
    public PowerInjection Generate() {
        return new PowerInjection(_activeMw.Generate(),
                                  _reactiveMvar.Generate());
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
