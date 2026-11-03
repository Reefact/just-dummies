using JustDummies;

using TaxRules.Domain;

using Xunit;

using static TaxRules.Tests.Build;

namespace TaxRules.Tests;

public sealed class IncomeAboveConditionTests {

    [Fact]
    public void An_income_equal_to_the_threshold_is_not_above_it() {
        Taxpayer taxpayer = TaxpayerEarning(Salary(30_000m));
        IncomeAboveCondition condition = new IncomeAboveCondition(IncomeKind.Employment, M(30_000m));

        Assert.False(condition.IsSatisfiedBy(taxpayer, Any.Int32().Between(2000, 2099).Generate()));
    }

    [Fact]
    public void An_income_one_cent_above_the_threshold_is_above_it() {
        Taxpayer taxpayer = TaxpayerEarning(Salary(30_000.01m));
        IncomeAboveCondition condition = new IncomeAboveCondition(IncomeKind.Employment, M(30_000m));

        Assert.True(condition.IsSatisfiedBy(taxpayer, Any.Int32().Between(2000, 2099).Generate()));
    }

    [Fact]
    public void Only_the_taxable_income_of_the_kinds_asked_for_counts() {
        Taxpayer taxpayer = TaxpayerEarning(Salary(20_000m), Business(15_000m, 2_000m), Interest(500m));
        int anyYear = Any.Int32().Between(2000, 2099).Generate();

        Assert.True(new IncomeAboveCondition(IncomeKind.Employment, M(19_999.99m)).IsSatisfiedBy(taxpayer, anyYear));
        Assert.False(new IncomeAboveCondition(IncomeKind.Employment, M(20_000m)).IsSatisfiedBy(taxpayer, anyYear));
        Assert.True(new IncomeAboveCondition(IncomeKind.Employment | IncomeKind.SelfEmployment, M(32_999.99m)).IsSatisfiedBy(taxpayer, anyYear));
        Assert.False(new IncomeAboveCondition(IncomeKind.Employment | IncomeKind.SelfEmployment, M(33_000m)).IsSatisfiedBy(taxpayer, anyYear));
        Assert.True(new IncomeAboveCondition(IncomeKind.All, M(33_499.99m)).IsSatisfiedBy(taxpayer, anyYear));
        Assert.False(new IncomeAboveCondition(IncomeKind.All, M(33_500m)).IsSatisfiedBy(taxpayer, anyYear));
    }

    [Fact]
    public void No_income_is_above_a_threshold_of_zero() {
        Taxpayer taxpayer = TaxpayerEarning();

        Assert.False(new IncomeAboveCondition(IncomeKind.All, Money.Zero).IsSatisfiedBy(taxpayer, Any.Int32().Between(2000, 2099).Generate()));
    }

    [Fact]
    public void A_condition_asks_for_at_least_one_known_kind() {
        Money anyThreshold = Any.Money().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new IncomeAboveCondition(IncomeKind.None, anyThreshold));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IncomeAboveCondition((IncomeKind)8, anyThreshold));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IncomeAboveCondition(IncomeKind.Employment | (IncomeKind)8, anyThreshold));
    }

    [Fact]
    public void A_condition_needs_its_threshold() {
        Assert.Throws<ArgumentNullException>(() => new IncomeAboveCondition(IncomeKind.All, null!));
    }

    [Fact]
    public void A_leaf_has_depth_one_and_no_operands() {
        IncomeAboveCondition anyCondition = Any.IncomeAboveCondition().Generate();

        Assert.Equal(1, anyCondition.Depth);
        Assert.Empty(anyCondition.Operands);
    }

    [Fact]
    public void Two_conditions_on_the_same_kinds_and_threshold_are_equal() {
        IncomeAboveCondition condition = new IncomeAboveCondition(IncomeKind.Employment, M(100m));

        Assert.Equal(condition, new IncomeAboveCondition(IncomeKind.Employment, M(100.00m)));
        Assert.Equal(condition.GetHashCode(), new IncomeAboveCondition(IncomeKind.Employment, M(100m)).GetHashCode());
        Assert.NotEqual(condition, new IncomeAboveCondition(IncomeKind.Investment, M(100m)));
        Assert.NotEqual(condition, new IncomeAboveCondition(IncomeKind.Employment, M(101m)));
    }

    [Fact]
    public void The_condition_is_described_by_its_kinds_and_threshold() {
        Assert.Equal("Employment, Investment income above 100.00", new IncomeAboveCondition(IncomeKind.Employment | IncomeKind.Investment, M(100m)).ToString());
    }

}

public sealed class ResidentOfConditionTests {

    [Theory]
    [InlineData("FR", "FR", true)]
    [InlineData("FR", "FR-75", true)]
    [InlineData("FR-75", "FR-75", true)]
    [InlineData("FR-75", "FR", false)]
    [InlineData("FR", "DE", false)]
    public void The_taxpayer_resides_where_the_jurisdiction_covers_their_residence(string condition, string residence, bool expected) {
        Taxpayer taxpayer = Any.Taxpayer().WithResidence(Jurisdiction(residence)).Generate();

        Assert.Equal(expected, new ResidentOfCondition(Jurisdiction(condition)).IsSatisfiedBy(taxpayer, Any.Int32().Between(2000, 2099).Generate()));
    }

    [Fact]
    public void A_condition_needs_its_jurisdiction() {
        Assert.Throws<ArgumentNullException>(() => new ResidentOfCondition(null!));
    }

    [Fact]
    public void A_leaf_has_depth_one_and_no_operands() {
        ResidentOfCondition anyCondition = Any.ResidentOfCondition().Generate();

        Assert.Equal(1, anyCondition.Depth);
        Assert.Empty(anyCondition.Operands);
    }

    [Fact]
    public void Two_conditions_on_the_same_jurisdiction_are_equal() {
        Assert.Equal(new ResidentOfCondition(Jurisdiction("US-CA")), new ResidentOfCondition(Jurisdiction("US-CA")));
        Assert.Equal(new ResidentOfCondition(Jurisdiction("US-CA")).GetHashCode(), new ResidentOfCondition(Jurisdiction("US-CA")).GetHashCode());
        Assert.NotEqual(new ResidentOfCondition(Jurisdiction("US-CA")), new ResidentOfCondition(Jurisdiction("US")));
        Assert.Equal("resident of US-CA", new ResidentOfCondition(Jurisdiction("US-CA")).ToString());
    }

}

public sealed class AgeAtLeastConditionTests {

    [Theory]
    [InlineData(1)]
    [InlineData(65)]
    [InlineData(120)]
    public void An_age_from_one_to_one_hundred_and_twenty_years_is_accepted(int years) {
        Assert.Equal(years, new AgeAtLeastCondition(years).Years);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void Any_other_age_is_refused(int years) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AgeAtLeastCondition(years));
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(31, false)]
    [InlineData(29, true)]
    public void The_age_is_the_tax_year_less_the_birth_year(int years, bool expected) {
        Taxpayer bornIn1990 = Any.Taxpayer().WithDateOfBirth(new DateOnly(1990, 6, 15)).Generate();

        Assert.Equal(expected, new AgeAtLeastCondition(years).IsSatisfiedBy(bornIn1990, 2020));
    }

    [Fact]
    public void A_taxpayer_born_on_the_first_of_january_and_one_born_on_the_last_of_december_are_the_same_age() {
        Taxpayer newYearBaby = Any.Taxpayer().WithDateOfBirth(new DateOnly(1990, 1, 1)).Generate();
        Taxpayer newYearsEveBaby = Any.Taxpayer().WithDateOfBirth(new DateOnly(1990, 12, 31)).Generate();
        AgeAtLeastCondition thirty = new AgeAtLeastCondition(30);

        Assert.True(thirty.IsSatisfiedBy(newYearBaby, 2020));
        Assert.True(thirty.IsSatisfiedBy(newYearsEveBaby, 2020));
        Assert.False(thirty.IsSatisfiedBy(newYearBaby, 2019));
        Assert.False(thirty.IsSatisfiedBy(newYearsEveBaby, 2019));
    }

    [Fact]
    public void A_taxpayer_is_not_yet_of_age_in_their_birth_year() {
        Taxpayer newborn = Any.Taxpayer().WithDateOfBirth(new DateOnly(2021, 3, 4)).Generate();

        Assert.False(new AgeAtLeastCondition(1).IsSatisfiedBy(newborn, 2021));
        Assert.True(new AgeAtLeastCondition(1).IsSatisfiedBy(newborn, 2022));
    }

    [Fact]
    public void A_leaf_has_depth_one_and_no_operands() {
        AgeAtLeastCondition anyCondition = Any.AgeAtLeastCondition().Generate();

        Assert.Equal(1, anyCondition.Depth);
        Assert.Empty(anyCondition.Operands);
    }

    [Fact]
    public void Two_conditions_on_the_same_age_are_equal_and_described_by_it() {
        Assert.Equal(new AgeAtLeastCondition(65), new AgeAtLeastCondition(65));
        Assert.Equal(new AgeAtLeastCondition(65).GetHashCode(), new AgeAtLeastCondition(65).GetHashCode());
        Assert.NotEqual(new AgeAtLeastCondition(65), new AgeAtLeastCondition(64));
        Assert.Equal("aged 65 or more", new AgeAtLeastCondition(65).ToString());
    }

}

public sealed class HouseholdSizeAtLeastConditionTests {

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(20)]
    public void A_household_of_two_to_twenty_members_is_accepted(int members) {
        Assert.Equal(members, new HouseholdSizeAtLeastCondition(members).Members);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(21)]
    public void Any_other_size_is_refused_a_household_of_one_being_every_household(int members) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HouseholdSizeAtLeastCondition(members));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void The_condition_holds_from_the_given_size_up(int householdSize, bool expected) {
        Taxpayer taxpayer = Any.Taxpayer().WithHouseholdSize(householdSize).Generate();

        Assert.Equal(expected, new HouseholdSizeAtLeastCondition(3).IsSatisfiedBy(taxpayer, Any.Int32().Between(2000, 2099).Generate()));
    }

    [Fact]
    public void A_leaf_has_depth_one_and_no_operands() {
        HouseholdSizeAtLeastCondition anyCondition = Any.HouseholdSizeAtLeastCondition().Generate();

        Assert.Equal(1, anyCondition.Depth);
        Assert.Empty(anyCondition.Operands);
    }

    [Fact]
    public void Two_conditions_on_the_same_size_are_equal_and_described_by_it() {
        Assert.Equal(new HouseholdSizeAtLeastCondition(4), new HouseholdSizeAtLeastCondition(4));
        Assert.Equal(new HouseholdSizeAtLeastCondition(4).GetHashCode(), new HouseholdSizeAtLeastCondition(4).GetHashCode());
        Assert.NotEqual(new HouseholdSizeAtLeastCondition(4), new HouseholdSizeAtLeastCondition(5));
        Assert.Equal("household of 4 or more", new HouseholdSizeAtLeastCondition(4).ToString());
    }

}

public sealed class CombinedConditionTests {

    private static ICondition True() => new AgeAtLeastCondition(1);

    private static ICondition False() => new HouseholdSizeAtLeastCondition(20);

    private static Taxpayer ElderlySingle() => Any.Taxpayer().WithDateOfBirth(new DateOnly(1950, 5, 5)).WithHouseholdSize(1).Generate();

    [Fact]
    public void The_fixtures_say_what_they_mean() {
        Taxpayer taxpayer = ElderlySingle();

        Assert.True(True().IsSatisfiedBy(taxpayer, 2020));
        Assert.False(False().IsSatisfiedBy(taxpayer, 2020));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void An_and_holds_when_every_operand_does(bool first, bool second, bool expected) {
        AndCondition and = new AndCondition([first ? True() : False(), second ? True() : False()]);

        Assert.Equal(expected, and.IsSatisfiedBy(ElderlySingle(), 2020));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void An_or_holds_when_one_operand_does(bool first, bool second, bool expected) {
        OrCondition or = new OrCondition([first ? True() : False(), second ? True() : False()]);

        Assert.Equal(expected, or.IsSatisfiedBy(ElderlySingle(), 2020));
    }

    [Fact]
    public void An_and_of_three_needs_all_three_and_an_or_of_three_needs_one() {
        Assert.False(new AndCondition([True(), True(), False()]).IsSatisfiedBy(ElderlySingle(), 2020));
        Assert.True(new AndCondition([True(), True(), True()]).IsSatisfiedBy(ElderlySingle(), 2020));
        Assert.True(new OrCondition([False(), False(), True()]).IsSatisfiedBy(ElderlySingle(), 2020));
        Assert.False(new OrCondition([False(), False(), False()]).IsSatisfiedBy(ElderlySingle(), 2020));
    }

    [Fact]
    public void A_not_holds_when_its_operand_does_not() {
        Assert.True(new NotCondition(False()).IsSatisfiedBy(ElderlySingle(), 2020));
        Assert.False(new NotCondition(True()).IsSatisfiedBy(ElderlySingle(), 2020));
    }

    [Fact]
    public void A_combination_exposes_its_operands_in_order() {
        ICondition first = Any.Condition().Leaves().Generate();
        ICondition second = new HouseholdSizeAtLeastCondition(3);
        ICondition third = new AgeAtLeastCondition(18);

        Assert.Equal(new[] { first, second, third }, new AndCondition([first, second, third]).Operands);
        Assert.Equal(new[] { first, second }, new OrCondition([first, second]).Operands);
        Assert.Equal(new[] { second }, new NotCondition(second).Operands);
    }

    [Fact]
    public void A_combination_of_fewer_than_two_operands_is_refused() {
        ICondition anyLeaf = Any.Condition().Leaves().Generate();

        Assert.Throws<ArgumentException>(() => new AndCondition([]));
        Assert.Throws<ArgumentException>(() => new AndCondition([anyLeaf]));
        Assert.Throws<ArgumentException>(() => new OrCondition([]));
        Assert.Throws<ArgumentException>(() => new OrCondition([anyLeaf]));
    }

    [Fact]
    public void A_combination_without_a_list_of_operands_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AndCondition(null!));
        Assert.Throws<ArgumentNullException>(() => new OrCondition(null!));
    }

    [Fact]
    public void A_combination_with_a_null_operand_is_refused() {
        ICondition anyLeaf = Any.Condition().Leaves().Generate();

        Assert.Throws<ArgumentException>(() => new AndCondition([anyLeaf, null!]));
        Assert.Throws<ArgumentException>(() => new OrCondition([null!, anyLeaf]));
    }

    [Fact]
    public void A_negation_needs_an_operand() {
        Assert.Throws<ArgumentNullException>(() => new NotCondition(null!));
    }

    [Fact]
    public void A_negation_of_a_negation_is_refused() {
        NotCondition negation = new NotCondition(Any.Condition().Leaves().Generate());

        Assert.Throws<ArgumentException>(() => new NotCondition(negation));
    }

    [Fact]
    public void A_negation_of_a_combination_holding_a_negation_is_accepted() {
        ICondition anyLeaf = Any.Condition().Leaves().Generate();
        AndCondition and = new AndCondition([new NotCondition(anyLeaf), anyLeaf]);

        NotCondition negation = new NotCondition(and);

        Assert.Equal(4, negation.Depth);
    }

    [Fact]
    public void The_depth_of_a_combination_is_one_more_than_its_deepest_operand() {
        ICondition anyLeaf = Any.Condition().Leaves().Generate();
        ICondition twoDeep = new AndCondition([anyLeaf, anyLeaf]);
        ICondition threeDeep = new OrCondition([anyLeaf, twoDeep]);

        Assert.Equal(1, anyLeaf.Depth);
        Assert.Equal(2, twoDeep.Depth);
        Assert.Equal(3, threeDeep.Depth);
        Assert.Equal(4, new NotCondition(threeDeep).Depth);
        Assert.Equal(4, new AndCondition([threeDeep, anyLeaf, anyLeaf]).Depth);
    }

    [Fact]
    public void A_condition_eight_levels_deep_is_accepted() {
        ICondition eightDeep = ConditionOfDepth(ICondition.MaxDepth);

        Assert.Equal(8, ICondition.MaxDepth);
        Assert.Equal(8, eightDeep.Depth);
    }

    [Fact]
    public void A_condition_nine_levels_deep_is_refused_whatever_the_combination() {
        ICondition eightDeep = ConditionOfDepth(ICondition.MaxDepth);
        ICondition anyLeaf = Any.Condition().Leaves().Generate();

        Assert.Throws<ArgumentException>(() => new AndCondition([eightDeep, anyLeaf]));
        Assert.Throws<ArgumentException>(() => new OrCondition([anyLeaf, eightDeep]));
        Assert.Throws<ArgumentException>(() => new NotCondition(eightDeep));
    }

    [Fact]
    public void Two_ands_over_the_same_operands_in_the_same_order_are_equal() {
        AndCondition and = new AndCondition([new AgeAtLeastCondition(18), new HouseholdSizeAtLeastCondition(3)]);

        Assert.Equal(and, new AndCondition([new AgeAtLeastCondition(18), new HouseholdSizeAtLeastCondition(3)]));
        Assert.Equal(and.GetHashCode(), new AndCondition([new AgeAtLeastCondition(18), new HouseholdSizeAtLeastCondition(3)]).GetHashCode());
    }

    [Fact]
    public void An_and_and_an_or_over_the_same_operands_are_not_equal() {
        ICondition[] operands = [new AgeAtLeastCondition(18), new HouseholdSizeAtLeastCondition(3)];

        Assert.False(new AndCondition(operands).Equals((object)new OrCondition(operands)));
        Assert.False(new OrCondition(operands).Equals((object)new AndCondition(operands)));
    }

    [Fact]
    public void The_order_of_the_operands_matters_to_equality() {
        AndCondition and = new AndCondition([new AgeAtLeastCondition(18), new HouseholdSizeAtLeastCondition(3)]);

        Assert.NotEqual(and, new AndCondition([new HouseholdSizeAtLeastCondition(3), new AgeAtLeastCondition(18)]));
        Assert.NotEqual(and, new AndCondition([new AgeAtLeastCondition(18), new HouseholdSizeAtLeastCondition(3), new AgeAtLeastCondition(18)]));
    }

    [Fact]
    public void Equality_looks_through_nested_combinations() {
        ICondition Nested() => new OrCondition([new NotCondition(new AgeAtLeastCondition(18)), new AndCondition([new HouseholdSizeAtLeastCondition(3), new ResidentOfCondition(Jurisdiction("FR"))])]);

        Assert.Equal(Nested(), Nested());
        Assert.Equal(Nested().GetHashCode(), Nested().GetHashCode());
    }

    [Fact]
    public void Two_negations_of_the_same_operand_are_equal_and_a_negation_is_not_its_operand() {
        Assert.Equal(new NotCondition(new AgeAtLeastCondition(18)), new NotCondition(new AgeAtLeastCondition(18)));
        Assert.Equal(new NotCondition(new AgeAtLeastCondition(18)).GetHashCode(), new NotCondition(new AgeAtLeastCondition(18)).GetHashCode());
        Assert.NotEqual(new NotCondition(new AgeAtLeastCondition(18)), new NotCondition(new AgeAtLeastCondition(19)));
        Assert.False(new NotCondition(new AgeAtLeastCondition(18)).Equals((object)new AgeAtLeastCondition(18)));
    }

    [Fact]
    public void A_combination_is_described_by_its_operands() {
        ICondition and = new AndCondition([new AgeAtLeastCondition(18), new HouseholdSizeAtLeastCondition(3)]);

        Assert.Equal("(aged 18 or more and household of 3 or more)", and.ToString());
        Assert.Equal("not aged 18 or more", new NotCondition(new AgeAtLeastCondition(18)).ToString());
    }

    [Fact]
    public void Satisfying_a_condition_requires_a_taxpayer() {
        ICondition anyLeaf = Any.Condition().Leaves().Generate();
        ICondition anyCombination = new AndCondition([anyLeaf, anyLeaf]);

        Assert.Throws<ArgumentNullException>(() => anyCombination.IsSatisfiedBy(null!, 2020));
        Assert.Throws<ArgumentNullException>(() => new NotCondition(anyLeaf).IsSatisfiedBy(null!, 2020));
        Assert.Throws<ArgumentNullException>(() => new OrCondition([anyLeaf, anyLeaf]).IsSatisfiedBy(null!, 2020));
    }

}
