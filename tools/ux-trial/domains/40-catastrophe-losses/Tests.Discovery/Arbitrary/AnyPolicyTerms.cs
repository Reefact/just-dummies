// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PolicyTerms --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PolicyTerms" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPolicyTerms : IAny<PolicyTerms> {

    private readonly IAny<decimal> _deductible;
    private readonly IAny<decimal> _limit;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPolicyTerms()
        : this(deductible: AnyValidDeductible(),
               limit:      AnyValidLimit()) { }

    private static AnyDecimal AnyValidDeductible() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2);
    }

    private static AnyDecimal AnyValidLimit() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private AnyPolicyTerms(IAny<decimal> deductible,
                           IAny<decimal> limit) {
        _deductible = deductible;
        _limit      = limit;
    }

    /// <summary>Pins <c>deductible</c> to a fixed value.</summary>
    public AnyPolicyTerms WithDeductible(decimal value) {
        return new AnyPolicyTerms(new FixedValue<decimal>(value), _limit);
    }

    /// <summary>Draws <c>deductible</c> from <c>AnyValidDeductible()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPolicyTerms WithDeductible(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPolicyTerms(configure(AnyValidDeductible()), _limit);
    }

    /// <summary>Pins <c>limit</c> to a fixed value.</summary>
    public AnyPolicyTerms WithLimit(decimal value) {
        return new AnyPolicyTerms(_deductible, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>limit</c> from <c>AnyValidLimit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPolicyTerms WithLimit(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPolicyTerms(_deductible, configure(AnyValidLimit()));
    }

    /// <summary>Produces one arbitrary <see cref="PolicyTerms" />.</summary>
    public PolicyTerms Generate() {
        return new PolicyTerms(_deductible.Generate(),
                               _limit.Generate());
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
