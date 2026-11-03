using JustDummies;

using TaxRules.Domain;

using Xunit;

namespace TaxRules.Tests;

/// <summary>
///     The generators are code the suite owns: their bases are rules the scaffolder could not all read, so
///     they are drawn many times here to show that none of them hands a constructor a value it refuses.
///     This checks the test support, not the domain.
/// </summary>
public sealed class GeneratorTests {

    private const int Draws = 300;

    private static void DrawsWithoutRefusal<T>(IAny<T> generator) {
        for (int i = 0; i < Draws; i++) {
            Assert.NotNull(generator.Generate());
        }
    }

    [Fact]
    public void The_value_object_generators_draw_values_their_constructors_accept() {
        DrawsWithoutRefusal(Any.Money());
        DrawsWithoutRefusal(Any.Rate());
        DrawsWithoutRefusal(Any.JurisdictionCode());
        DrawsWithoutRefusal(Any.RuleCode());
        DrawsWithoutRefusal(Any.TaxpayerId());
        DrawsWithoutRefusal(Any.TaxBracket());
    }

    [Fact]
    public void The_income_and_condition_generators_draw_values_their_constructors_accept() {
        DrawsWithoutRefusal(Any.EmploymentIncome());
        DrawsWithoutRefusal(Any.SelfEmploymentIncome());
        DrawsWithoutRefusal(Any.InvestmentIncome());
        DrawsWithoutRefusal(Any.IncomeComponent());
        DrawsWithoutRefusal(Any.IncomeAboveCondition());
        DrawsWithoutRefusal(Any.ResidentOfCondition());
        DrawsWithoutRefusal(Any.AgeAtLeastCondition());
        DrawsWithoutRefusal(Any.HouseholdSizeAtLeastCondition());
        DrawsWithoutRefusal(Any.AndCondition());
        DrawsWithoutRefusal(Any.OrCondition());
        DrawsWithoutRefusal(Any.NotCondition());
        DrawsWithoutRefusal(Any.Condition());
    }

    [Fact]
    public void The_rule_generators_draw_values_their_constructors_accept() {
        DrawsWithoutRefusal(Any.FlatRateRule());
        DrawsWithoutRefusal(Any.ProgressiveRule());
        DrawsWithoutRefusal(Any.ThresholdExemptionRule());
        DrawsWithoutRefusal(Any.CappedRule());
        DrawsWithoutRefusal(Any.CompositeRule());
        DrawsWithoutRefusal(Any.TaxCreditRule());
        DrawsWithoutRefusal(Any.TaxRule());
        DrawsWithoutRefusal(Any.TaxRule().Leaves());
        DrawsWithoutRefusal(Any.TaxRule().NotCapped());
        DrawsWithoutRefusal(Any.TaxRule().Credits());
    }

    [Fact]
    public void The_entity_generators_draw_values_their_constructors_accept() {
        DrawsWithoutRefusal(Any.Taxpayer());
        DrawsWithoutRefusal(Any.TaxSchedule());
    }

    [Fact]
    public void An_arbitrary_taxpayer_is_always_alive_and_computable_for_any_schedule_year() {
        for (int i = 0; i < Draws; i++) {
            Taxpayer taxpayer = Any.Taxpayer().Generate();

            Assert.True(taxpayer.DateOfBirth.Year <= TaxSchedule.FirstYear);
            Assert.Equal(taxpayer.TaxableIncome(IncomeKind.All), taxpayer.Incomes.Aggregate(Money.Zero, (sum, income) => sum.Plus(income.TaxableAmount)));
        }
    }

    [Fact]
    public void An_arbitrary_rule_of_each_shape_can_be_evaluated_for_an_arbitrary_taxpayer() {
        for (int i = 0; i < Draws; i++) {
            TaxRule rule = Any.TaxRule().Generate();
            Taxpayer taxpayer = Any.Taxpayer().Generate();

            BreakdownLine line = rule.Evaluate(taxpayer, Any.Int32().Between(TaxSchedule.FirstYear, TaxSchedule.LastYear).Generate());

            Assert.Equal(rule.Code, line.Code);
        }
    }

    [Fact]
    public void An_arbitrary_condition_is_never_deeper_than_the_domain_allows() {
        for (int i = 0; i < Draws; i++) {
            Assert.InRange(Any.Condition().Generate().Depth, 1, ICondition.MaxDepth);
        }
    }

    [Fact]
    public void Arbitrary_rule_codes_of_one_tree_do_not_collide() {
        for (int i = 0; i < Draws; i++) {
            TaxRule rule = Any.TaxRule().Generate();
            List<RuleCode> codes = rule.SelfAndDescendants().Select(r => r.Code).ToList();

            Assert.Equal(codes.Count, codes.Distinct().Count());
        }
    }

    [Fact]
    public void An_employment_whose_gross_is_pinned_at_either_end_of_what_money_holds_still_draws_a_deduction() {
        EmploymentIncome nothing = Any.EmploymentIncome().WithGross(Money.Zero).Generate();
        EmploymentIncome everything = Any.EmploymentIncome().WithGross(new Money(Money.MaxAmount)).Generate();

        Assert.True(nothing.ExpenseDeduction.IsZero);
        Assert.True(everything.ExpenseDeduction.CompareTo(everything.Gross) <= 0);
    }

}
