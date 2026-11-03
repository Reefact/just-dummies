// Written by hand: the generators `any` cannot scaffold (abstract types, a tree, a contiguous scale).

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>A label a rule or a line can carry: not blank, at most 80 characters.</summary>
public static class AnyLabel {

    public static AnyString Create() {
        return Any.String().NotBlank().WithMaxLength(80);
    }

}

/// <summary>Any leaf condition: one that tests the taxpayer and has no operand.</summary>
public sealed class AnyLeafCondition : IAny<ICondition> {

    public ICondition Generate() {
        return Any.Int32().Between(0, 3).Generate() switch {
            0 => new AnyIncomeAboveCondition().Generate(),
            1 => new AnyResidentOfCondition().Generate(),
            2 => new AnyAgeAtLeastCondition().Generate(),
            _ => new AnyHouseholdSizeAtLeastCondition().Generate()
        };
    }

}

/// <summary>Any condition: a leaf, or a shallow And / Or / Not over leaves.</summary>
public sealed class AnyICondition : IAny<ICondition> {

    public ICondition Generate() {
        return Any.Int32().Between(0, 3).Generate() switch {
            0 => new AnyLeafCondition().Generate(),
            1 => new AnyAndCondition().Generate(),
            2 => new AnyOrCondition().Generate(),
            _ => new AnyNotCondition().Generate()
        };
    }

}

/// <summary>Any income component of the three kinds.</summary>
public sealed class AnyIncomeComponent : IAny<IncomeComponent> {

    public IncomeComponent Generate() {
        return Any.Int32().Between(0, 2).Generate() switch {
            0 => new AnyEmploymentIncome().Generate(),
            1 => new AnySelfEmploymentIncome().Generate(),
            _ => new AnyInvestmentIncome().Generate()
        };
    }

}

/// <summary>Any tax rule that is neither a cap, a composite nor a credit.</summary>
public sealed class AnyLeafTaxRule : IAny<TaxRule> {

    public TaxRule Generate() {
        return Any.Int32().Between(0, 2).Generate() switch {
            0 => new AnyFlatRateRule().Generate(),
            1 => new AnyProgressiveRule().Generate(),
            _ => new AnyThresholdExemptionRule().Generate()
        };
    }

}

/// <summary>A valid progressive scale: from zero, contiguous, rates never decreasing, the last open-ended.</summary>
public sealed class AnyBracketScale : IAny<IEnumerable<TaxBracket>> {

    public IEnumerable<TaxBracket> Generate() {
        int bracketCount = Any.Int32().Between(1, 6).Generate();

        List<decimal> bounds = Any.ListOf(Any.Decimal().Between(1m, 1_000_000m).WithScale(2))
                                  .WithCount(bracketCount - 1)
                                  .Ordered()
                                  .Distinct()
                                  .Generate();
        List<decimal> rates = Any.ListOf(Any.Decimal().Between(0m, 1m).WithScale(4))
                                 .WithCount(bracketCount)
                                 .Ordered()
                                 .Generate();

        List<TaxBracket> brackets = [];
        decimal          lower    = 0m;
        for (int i = 0; i < bracketCount; i++) {
            Money? upper = i < bounds.Count ? new Money(bounds[i]) : null;
            brackets.Add(new TaxBracket(new Money(lower), upper, new Rate(rates[i])));
            if (i < bounds.Count) { lower = bounds[i]; }
        }

        return brackets;
    }

}

public static class HandWrittenEntries {

    extension(Any) {

        public static AnyString Label() => AnyLabel.Create();

        public static AnyLeafCondition LeafCondition() => new AnyLeafCondition();

        public static AnyICondition Condition() => new AnyICondition();

        public static AnyIncomeComponent IncomeComponent() => new AnyIncomeComponent();

        public static AnyLeafTaxRule LeafTaxRule() => new AnyLeafTaxRule();

        public static AnyBracketScale BracketScale() => new AnyBracketScale();

    }

}
