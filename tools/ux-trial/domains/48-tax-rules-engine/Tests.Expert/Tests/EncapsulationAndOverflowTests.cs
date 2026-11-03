using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

/// <summary>Two behaviours the model does not describe, pinned as the refusals a reviewer would expect.</summary>
public sealed class EncapsulationAndOverflowTests {

    [Fact(Skip = "domain defect: TaxSchedule.Rules hands out its private List<TaxRule> as IReadOnlyList, so a caller casting it can add or remove rules of a validated schedule, bypassing every refusal of AddRule and RemoveRule; the same holds for CompositeRule.Children, ProgressiveRule.Brackets, Taxpayer.Incomes and TaxBreakdown.Lines; structural, no seed involved")]
    public void The_rules_of_a_schedule_cannot_be_changed_through_the_list_it_exposes() {
        TaxSchedule schedule = Any.TaxSchedule().Generate();
        schedule.AddRule(Any.FlatRateRule().WithCondition(null).Generate());
        schedule.Validate();

        Assert.False(schedule.Rules is List<TaxRule>);
    }

    [Fact(Skip = "domain defect: incomes whose taxable amounts add up beyond Money's ceiling make Taxpayer.TaxableIncome, and so TaxSchedule.Evaluate, throw a bare ArgumentOutOfRangeException from Money's constructor instead of a domain refusal; a Taxpayer holding such incomes can be built; literal amounts, no seed involved")]
    public void A_schedule_refuses_a_taxpayer_whose_incomes_cannot_be_summed_with_a_domain_refusal() {
        TaxSchedule schedule = Any.TaxSchedule().Generate();
        schedule.AddRule(Any.FlatRateRule().WithTaxBase(IncomeKind.Employment).WithCondition(null).Generate());
        schedule.Validate();
        Taxpayer overflowing = TaxpayerEarning(Salary(600_000_000_000m), Salary(600_000_000_000m));

        Assert.Throws<TaxScheduleException>(() => schedule.Evaluate(overflowing));
    }

}
