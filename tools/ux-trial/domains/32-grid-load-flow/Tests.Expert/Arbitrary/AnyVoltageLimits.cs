// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VoltageLimits --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VoltageLimits" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVoltageLimits : IAny<VoltageLimits> {

    private readonly IAny<double> _minimumPu;
    private readonly IAny<double> _maximumPu;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVoltageLimits()
        : this(minimumPu: AnyValidMinimumPu(),
               maximumPu: AnyValidMaximumPu()) { }

    private static AnyDouble AnyValidMinimumPu() {
        // In [AbsoluteMinimumPu, 1.0). Kept strictly below nominal so that min < max holds by construction
        // whatever the maximum draws (the rule ties the two parameters).
        return Any.Double().GreaterThanOrEqualTo(VoltageLimits.AbsoluteMinimumPu).LessThan(1.0);
    }

    private static AnyDouble AnyValidMaximumPu() {
        // In (1.0, AbsoluteMaximumPu]: strictly above nominal, see the minimum.
        return Any.Double().GreaterThan(1.0).LessThanOrEqualTo(VoltageLimits.AbsoluteMaximumPu);
    }

    private AnyVoltageLimits(IAny<double> minimumPu,
                             IAny<double> maximumPu) {
        _minimumPu = minimumPu;
        _maximumPu = maximumPu;
    }

    /// <summary>Pins <c>minimumPu</c> to a fixed value.</summary>
    public AnyVoltageLimits WithMinimumPu(double value) {
        return new AnyVoltageLimits(new FixedValue<double>(value), _maximumPu);
    }

    /// <summary>Draws <c>minimumPu</c> from <c>AnyValidMinimumPu()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoltageLimits WithMinimumPu(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVoltageLimits(configure(AnyValidMinimumPu()), _maximumPu);
    }

    /// <summary>Pins <c>maximumPu</c> to a fixed value.</summary>
    public AnyVoltageLimits WithMaximumPu(double value) {
        return new AnyVoltageLimits(_minimumPu, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>maximumPu</c> from <c>AnyValidMaximumPu()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoltageLimits WithMaximumPu(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVoltageLimits(_minimumPu, configure(AnyValidMaximumPu()));
    }

    /// <summary>Produces one arbitrary <see cref="VoltageLimits" />.</summary>
    public VoltageLimits Generate() {
        return new VoltageLimits(_minimumPu.Generate(),
                                 _maximumPu.Generate());
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
