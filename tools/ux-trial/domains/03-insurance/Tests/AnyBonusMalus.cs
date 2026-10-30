// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BonusMalus --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BonusMalus" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBonusMalus : IAny<BonusMalus> {

    private readonly IAny<double> _coefficient;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBonusMalus()
        : this(coefficient: AnyValidCoefficient()) { }

    private static AnyDouble AnyValidCoefficient() {
        // BonusMalus delegates its guard to a private Validate method: 0.50 to 3.50, never NaN.
        return Any.Double().Between(BonusMalus.Floor, BonusMalus.Ceiling);
    }

    private AnyBonusMalus(IAny<double> coefficient) {
        _coefficient = coefficient;
    }

    /// <summary>Pins <c>coefficient</c> to a fixed value.</summary>
    public AnyBonusMalus WithCoefficient(double value) {
        return new AnyBonusMalus(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>coefficient</c> from <c>AnyValidCoefficient()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBonusMalus WithCoefficient(Func<AnyDouble, AnyDouble> configure) {
        return new AnyBonusMalus(configure(AnyValidCoefficient()));
    }

    /// <summary>Produces one arbitrary <see cref="BonusMalus" />.</summary>
    public BonusMalus Generate() {
        return new BonusMalus(_coefficient.Generate());
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
