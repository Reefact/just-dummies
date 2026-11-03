using System;
using System.Collections.Generic;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     Written by hand: an arbitrary rule among the concrete ones. By default a tax at the top of a small tree:
///     a flat rate, a progressive scale, a threshold exemption, a cap over one of those, or a composite of two or
///     three of them. <see cref="Credits" /> asks for a credit instead.
/// </summary>
public sealed class AnyTaxRule : IAny<TaxRule> {

    private enum Shape { AnyTax, Leaf, NotCapped, Credit }

    private readonly Shape _shape;

    public AnyTaxRule()
        : this(Shape.AnyTax) { }

    private AnyTaxRule(Shape shape) {
        _shape = shape;
    }

    /// <summary>A rule with no nested rule: flat rate, progressive scale or threshold exemption.</summary>
    public AnyTaxRule Leaves() {
        return new AnyTaxRule(Shape.Leaf);
    }

    /// <summary>A tax that is not a cap (a cap does not wrap another cap): a leaf or a composite.</summary>
    public AnyTaxRule NotCapped() {
        return new AnyTaxRule(Shape.NotCapped);
    }

    /// <summary>A credit.</summary>
    public AnyTaxRule Credits() {
        return new AnyTaxRule(Shape.Credit);
    }

    public static IAny<TaxRule> LeafRule() {
        return Any.Int32().Between(0, 2).Then(choice => choice switch {
            0 => new AnyFlatRateRule().As(r => (TaxRule)r),
            1 => new AnyProgressiveRule().As(r => (TaxRule)r),
            _ => new AnyThresholdExemptionRule().As(r => (TaxRule)r)
        });
    }

    public TaxRule Generate() {
        return Build(_shape).Generate();
    }

    private static IAny<TaxRule> Build(Shape shape) {
        switch (shape) {
            case Shape.Leaf:
                return LeafRule();
            case Shape.Credit:
                return new AnyTaxCreditRule().As(r => (TaxRule)r);
            case Shape.NotCapped:
                return Any.Boolean().Then(composite => composite ? CompositeOfLeaves() : LeafRule());
            default:
                return Any.Int32().Between(0, 3).Then(choice => choice switch {
                    0 => LeafRule(),
                    1 => LeafRule(),
                    2 => LeafRule().Then(inner => new AnyCappedRule().WithInner(inner).As(r => (TaxRule)r)),
                    _ => CompositeOfLeaves()
                });
        }
    }

    private static IAny<TaxRule> CompositeOfLeaves() {
        return Any.ListOf(LeafRule()).WithCountBetween(2, 3).Then(components => new AnyCompositeRule().WithComponents(components).As(r => (TaxRule)r));
    }

}
