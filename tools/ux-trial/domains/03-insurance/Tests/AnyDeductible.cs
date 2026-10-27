// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Deductible --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Deductible" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDeductible : IAny<Deductible> {

    private readonly IAny<decimal> _amount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDeductible()
        : this(amount: AnyValidAmount()) { }

    private static AnyDecimal AnyValidAmount() {
        // The guard also demands whole cents, which the scaffolder could not read.
        return Any.Decimal().Between(0m, Deductible.Ceiling).WithScale(2);
    }

    private AnyDeductible(IAny<decimal> amount) {
        _amount = amount;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyDeductible WithAmount(decimal value) {
        return new AnyDeductible(new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDeductible WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyDeductible(configure(AnyValidAmount()));
    }

    /// <summary>Produces one arbitrary <see cref="Deductible" />.</summary>
    public Deductible Generate() {
        return new Deductible(_amount.Generate());
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
