// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LossDistribution --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LossDistribution" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLossDistribution : IAny<LossDistribution> {

    private readonly IAny<IReadOnlyList<decimal>> _annualLosses;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLossDistribution()
        : this(annualLosses: AnyValidAnnualLosses()) { }

    private static AnyList<decimal> AnyValidAnnualLosses() {
        return Any.ListOf(Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2)).NonEmpty();
    }

    private AnyLossDistribution(IAny<IReadOnlyList<decimal>> annualLosses) {
        _annualLosses = annualLosses;
    }

    /// <summary>Pins <c>annualLosses</c> to a fixed value.</summary>
    public AnyLossDistribution WithAnnualLosses(IReadOnlyList<decimal> value) {
        return new AnyLossDistribution(new FixedValue<IReadOnlyList<decimal>>(value));
    }

    /// <summary>Draws <c>annualLosses</c> from <c>AnyValidAnnualLosses()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLossDistribution WithAnnualLosses(Func<AnyList<decimal>, AnyList<decimal>> configure) {
        return new AnyLossDistribution(configure(AnyValidAnnualLosses()));
    }

    /// <summary>Produces one arbitrary <see cref="LossDistribution" />.</summary>
    public LossDistribution Generate() {
        return new LossDistribution(_annualLosses.Generate());
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
