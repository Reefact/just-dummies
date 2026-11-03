using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

public sealed class LeafConditionTests {

    private const int AnyYear = 2024;

    private static Taxpayer TaxpayerWith(params IncomeComponent[] incomes) {
        return new AnyTaxpayer().WithIncomes(incomes).Generate();
    }

    [Theory]
    [InlineData("999.99", false)]
    [InlineData("1000.00", false)]
    [InlineData("1000.01", true)]
    [InlineData("2000.00", true)]
    public void Income_above_a_threshold_is_strict(string gross, bool expected) {
        // Arrange
        IncomeAboveCondition condition = new IncomeAboveCondition(IncomeKind.Employment, Lit.M("1000"));
        Taxpayer             taxpayer  = TaxpayerWith(new EmploymentIncome("ACME", Lit.M(gross), Lit.M("0")));

        // Act / Assert
        Assert.Equal(expected, condition.IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Income_above_a_threshold_counts_only_the_kinds_it_names() {
        // Arrange
        Taxpayer taxpayer = TaxpayerWith(
            new EmploymentIncome("ACME", Lit.M("600"), Lit.M("0")),
            new SelfEmploymentIncome("shop", Lit.M("600"), Lit.M("0")));

        // Act / Assert
        Assert.False(new IncomeAboveCondition(IncomeKind.Employment, Lit.M("1000")).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new IncomeAboveCondition(IncomeKind.SelfEmployment, Lit.M("1000")).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new IncomeAboveCondition(IncomeKind.Employment | IncomeKind.SelfEmployment, Lit.M("1000")).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new IncomeAboveCondition(IncomeKind.All, Lit.M("1000")).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new IncomeAboveCondition(IncomeKind.Investment, Lit.M("0")).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Income_above_a_threshold_counts_dividends_at_their_taxable_share() {
        // Arrange: 1 500 of dividends is 900 taxable
        Taxpayer taxpayer = TaxpayerWith(new InvestmentIncome("fund", Lit.M("1500"), InvestmentKind.Dividend));

        // Act / Assert
        Assert.False(new IncomeAboveCondition(IncomeKind.Investment, Lit.M("900")).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new IncomeAboveCondition(IncomeKind.Investment, Lit.M("899.99")).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void A_taxpayer_without_income_is_never_above_a_threshold_of_zero() {
        Taxpayer anyTaxpayer = TaxpayerWith();

        Assert.False(new IncomeAboveCondition(IncomeKind.All, Money.Zero).IsSatisfiedBy(anyTaxpayer, AnyYear));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    public void Income_above_a_threshold_refuses_a_base_without_a_known_kind(int kinds) {
        Money anyThreshold = new AnyMoney().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new IncomeAboveCondition((IncomeKind)kinds, anyThreshold));
    }

    [Fact]
    public void Income_above_a_threshold_refuses_a_null_threshold() {
        Assert.Throws<ArgumentNullException>(() => new IncomeAboveCondition(IncomeKind.All, null!));
    }

    [Fact]
    public void A_leaf_has_depth_one_and_no_operands() {
        ICondition anyLeaf = Dummies.LeafCondition().Generate();

        Assert.Equal(1, anyLeaf.Depth);
        Assert.Empty(anyLeaf.Operands);
    }

    [Fact]
    public void Residency_in_a_subdivision_is_satisfied_by_residents_of_exactly_that_subdivision() {
        // Arrange
        ResidentOfCondition condition = new ResidentOfCondition(new JurisdictionCode("US-CA"));

        // Act / Assert
        Assert.True(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("US-CA")).Generate(), AnyYear));
        Assert.False(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("US-NY")).Generate(), AnyYear));
        Assert.False(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("US")).Generate(), AnyYear));
    }

    [Fact]
    public void Residency_in_a_country_is_satisfied_by_residents_of_its_subdivisions() {
        // Arrange
        ResidentOfCondition condition = new ResidentOfCondition(new JurisdictionCode("FR"));

        // Act / Assert
        Assert.True(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("FR")).Generate(), AnyYear));
        Assert.True(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("FR-75")).Generate(), AnyYear));
        Assert.False(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("DE")).Generate(), AnyYear));
    }

    [Fact]
    public void Residency_refuses_a_null_jurisdiction() {
        Assert.Throws<ArgumentNullException>(() => new ResidentOfCondition(null!));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(65)]
    [InlineData(120)]
    public void An_age_condition_accepts_one_to_one_hundred_and_twenty_years(int years) {
        Assert.Equal(years, new AgeAtLeastCondition(years).Years);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void An_age_condition_refuses_the_rest(int years) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AgeAtLeastCondition(years));
    }

    [Theory]
    [InlineData(2000, 12, 31, 2030, 30, true)]
    [InlineData(2000, 1, 1, 2030, 30, true)]
    [InlineData(2000, 1, 1, 2030, 31, false)]
    [InlineData(2000, 1, 1, 2029, 30, false)]
    [InlineData(2000, 6, 1, 2000, 1, false)]
    public void Age_is_measured_on_31_December_of_the_tax_year(int year, int month, int day, int taxYear, int years, bool expected) {
        // Arrange
        Taxpayer taxpayer = new AnyTaxpayer().WithDateOfBirth(new DateOnly(year, month, day)).Generate();

        // Act / Assert
        Assert.Equal(expected, new AgeAtLeastCondition(years).IsSatisfiedBy(taxpayer, taxYear));
    }

    [Fact]
    public void An_age_condition_cannot_be_evaluated_for_a_year_before_the_birth() {
        Taxpayer anyTaxpayer = new AnyTaxpayer().WithDateOfBirth(new DateOnly(2000, 1, 1)).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new AgeAtLeastCondition(18).IsSatisfiedBy(anyTaxpayer, 1999));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(20)]
    public void A_household_condition_accepts_two_to_twenty_members(int members) {
        Assert.Equal(members, new HouseholdSizeAtLeastCondition(members).Members);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(21)]
    public void A_household_condition_refuses_the_rest(int members) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HouseholdSizeAtLeastCondition(members));
    }

    [Theory]
    [InlineData(1, 3, false)]
    [InlineData(2, 3, false)]
    [InlineData(3, 3, true)]
    [InlineData(4, 3, true)]
    [InlineData(20, 20, true)]
    public void A_household_condition_is_satisfied_from_its_member_count_up(int household, int members, bool expected) {
        Taxpayer taxpayer = new AnyTaxpayer().WithHouseholdSize(household).Generate();

        Assert.Equal(expected, new HouseholdSizeAtLeastCondition(members).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Every_leaf_refuses_a_null_taxpayer() {
        ICondition anyLeaf = Dummies.LeafCondition().Generate();

        Assert.Throws<ArgumentNullException>(() => anyLeaf.IsSatisfiedBy(null!, AnyYear));
    }

    [Fact]
    public void Leaf_equality_is_by_value() {
        Assert.Equal(new AgeAtLeastCondition(65), new AgeAtLeastCondition(65));
        Assert.NotEqual(new AgeAtLeastCondition(65), new AgeAtLeastCondition(66));
        Assert.Equal(new HouseholdSizeAtLeastCondition(3), new HouseholdSizeAtLeastCondition(3));
        Assert.NotEqual(new HouseholdSizeAtLeastCondition(3), new HouseholdSizeAtLeastCondition(4));
        Assert.Equal(new ResidentOfCondition(new JurisdictionCode("FR")), new ResidentOfCondition(new JurisdictionCode("FR")));
        Assert.NotEqual(new ResidentOfCondition(new JurisdictionCode("FR")), new ResidentOfCondition(new JurisdictionCode("DE")));
        Assert.Equal(new IncomeAboveCondition(IncomeKind.All, Lit.M("10")), new IncomeAboveCondition(IncomeKind.All, Lit.M("10.00")));
        Assert.NotEqual(new IncomeAboveCondition(IncomeKind.All, Lit.M("10")), new IncomeAboveCondition(IncomeKind.Employment, Lit.M("10")));
        Assert.NotEqual(new IncomeAboveCondition(IncomeKind.All, Lit.M("10")), new IncomeAboveCondition(IncomeKind.All, Lit.M("11")));
    }

    [Fact]
    public void Leaves_of_different_kinds_are_never_equal_even_with_the_same_number() {
        ICondition age       = new AgeAtLeastCondition(3);
        ICondition household = new HouseholdSizeAtLeastCondition(3);

        Assert.False(age.Equals(household));
        Assert.False(household.Equals(age));
    }

    [Fact]
    public void Equal_leaves_have_equal_hash_codes() {
        Assert.Equal(new AgeAtLeastCondition(65).GetHashCode(), new AgeAtLeastCondition(65).GetHashCode());
        Assert.Equal(new HouseholdSizeAtLeastCondition(4).GetHashCode(), new HouseholdSizeAtLeastCondition(4).GetHashCode());
        Assert.Equal(new IncomeAboveCondition(IncomeKind.All, Lit.M("10")).GetHashCode(), new IncomeAboveCondition(IncomeKind.All, Lit.M("10")).GetHashCode());
        Assert.Equal(new ResidentOfCondition(new JurisdictionCode("FR")).GetHashCode(), new ResidentOfCondition(new JurisdictionCode("FR")).GetHashCode());
    }

    [Fact]
    public void Leaves_describe_themselves() {
        Assert.Equal("aged 65 or more", new AgeAtLeastCondition(65).ToString());
        Assert.Equal("household of 3 or more", new HouseholdSizeAtLeastCondition(3).ToString());
        Assert.Equal("resident of FR", new ResidentOfCondition(new JurisdictionCode("FR")).ToString());
        Assert.Equal("Employment income above 10.00", new IncomeAboveCondition(IncomeKind.Employment, Lit.M("10")).ToString());
    }

}

public sealed class CombinedConditionTests {

    private const int AnyYear = 2024;

    private static readonly ICondition Yes = new ResidentOfCondition(new JurisdictionCode("FR"));
    private static readonly ICondition No  = new ResidentOfCondition(new JurisdictionCode("DE"));

    private static Taxpayer Frenchman() {
        return new AnyTaxpayer().WithResidence(new JurisdictionCode("FR")).Generate();
    }

    private static ICondition NestedAnd(int levelsAboveTheLeaf) {
        ICondition condition = Dummies.LeafCondition().Generate();
        for (int i = 0; i < levelsAboveTheLeaf; i++) {
            condition = new AndCondition([condition, Dummies.LeafCondition().Generate()]);
        }

        return condition;
    }

    [Fact]
    public void And_is_satisfied_when_every_operand_is() {
        Taxpayer taxpayer = Frenchman();

        Assert.True(new AndCondition([Yes, Yes]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new AndCondition([Yes, Yes, Yes]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new AndCondition([Yes, No]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new AndCondition([No, Yes]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new AndCondition([Yes, Yes, No]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new AndCondition([No, No]).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Or_is_satisfied_when_one_operand_is() {
        Taxpayer taxpayer = Frenchman();

        Assert.True(new OrCondition([Yes, No]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new OrCondition([No, Yes]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new OrCondition([No, No, Yes]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new OrCondition([Yes, Yes]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new OrCondition([No, No]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new OrCondition([No, No, No]).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Not_inverts_its_operand() {
        Taxpayer taxpayer = Frenchman();

        Assert.False(new NotCondition(Yes).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new NotCondition(No).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Conditions_nest() {
        // Arrange: (FR and not (DE or household of 5+))
        ICondition condition = new AndCondition([
            Yes,
            new NotCondition(new OrCondition([No, new HouseholdSizeAtLeastCondition(5)]))
        ]);

        // Act / Assert
        Assert.True(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("FR")).WithHouseholdSize(4).Generate(), AnyYear));
        Assert.False(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("FR")).WithHouseholdSize(5).Generate(), AnyYear));
        Assert.False(condition.IsSatisfiedBy(new AnyTaxpayer().WithResidence(new JurisdictionCode("DE")).WithHouseholdSize(1).Generate(), AnyYear));
    }

    [Fact]
    public void A_combination_needs_at_least_two_operands() {
        ICondition anyLeaf = Dummies.LeafCondition().Generate();

        Assert.Throws<ArgumentException>(() => new AndCondition([]));
        Assert.Throws<ArgumentException>(() => new AndCondition([anyLeaf]));
        Assert.Throws<ArgumentException>(() => new OrCondition([]));
        Assert.Throws<ArgumentException>(() => new OrCondition([anyLeaf]));
    }

    [Fact]
    public void A_combination_refuses_a_null_operand_or_a_null_list() {
        ICondition anyLeaf = Dummies.LeafCondition().Generate();

        Assert.Throws<ArgumentException>(() => new AndCondition([anyLeaf, null!]));
        Assert.Throws<ArgumentException>(() => new OrCondition([null!, anyLeaf]));
        Assert.Throws<ArgumentNullException>(() => new AndCondition(null!));
        Assert.Throws<ArgumentNullException>(() => new OrCondition(null!));
        Assert.Throws<ArgumentNullException>(() => new NotCondition(null!));
    }

    [Fact]
    public void A_negation_of_a_negation_is_refused() {
        ICondition anyLeaf = Dummies.LeafCondition().Generate();

        Assert.Throws<ArgumentException>(() => new NotCondition(new NotCondition(anyLeaf)));
    }

    [Fact]
    public void A_negation_of_a_combination_of_negations_is_accepted() {
        ICondition anyLeaf = Dummies.LeafCondition().Generate();

        NotCondition negation = new NotCondition(new AndCondition([new NotCondition(anyLeaf), anyLeaf]));

        Assert.Equal(4, negation.Depth);
    }

    [Fact]
    public void The_depth_is_one_more_than_the_deepest_operand() {
        // Arrange
        ICondition anyLeaf = Dummies.LeafCondition().Generate();
        ICondition deep    = NestedAnd(3);

        // Act
        AndCondition and = new AndCondition([anyLeaf, deep]);
        OrCondition  or  = new OrCondition([deep, anyLeaf]);
        NotCondition not = new NotCondition(deep);

        // Assert
        Assert.Equal(4, deep.Depth);
        Assert.Equal(5, and.Depth);
        Assert.Equal(5, or.Depth);
        Assert.Equal(5, not.Depth);
    }

    [Fact]
    public void A_condition_eight_levels_deep_is_accepted() {
        ICondition deepest = NestedAnd(ICondition.MaxDepth - 1);

        Assert.Equal(ICondition.MaxDepth, deepest.Depth);
    }

    [Fact]
    public void A_condition_nine_levels_deep_is_refused_whatever_the_combination() {
        // Arrange
        ICondition eight = NestedAnd(ICondition.MaxDepth - 1);
        ICondition leaf  = Dummies.LeafCondition().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new AndCondition([eight, leaf]));
        Assert.Throws<ArgumentException>(() => new OrCondition([leaf, eight]));
        Assert.Throws<ArgumentException>(() => new NotCondition(eight));
    }

    [Fact]
    public void Operands_are_listed_in_the_order_given() {
        ICondition first  = new AgeAtLeastCondition(10);
        ICondition second = new AgeAtLeastCondition(20);
        ICondition third  = new AgeAtLeastCondition(30);

        Assert.Equal([first, second, third], new AndCondition([first, second, third]).Operands);
        Assert.Equal([first, second], new OrCondition([first, second]).Operands);
        Assert.Equal([first], new NotCondition(first).Operands);
    }

    [Fact]
    public void A_combination_does_not_follow_later_changes_to_the_list_it_was_given() {
        List<ICondition> operands = [Yes, Yes];
        AndCondition     and      = new AndCondition(operands);

        operands.Add(No);

        Assert.Equal(2, and.Operands.Count);
    }

    [Fact]
    public void Combinations_refuse_a_null_taxpayer() {
        Assert.Throws<ArgumentNullException>(() => new AndCondition([Yes, Yes]).IsSatisfiedBy(null!, AnyYear));
        Assert.Throws<ArgumentNullException>(() => new OrCondition([Yes, Yes]).IsSatisfiedBy(null!, AnyYear));
        Assert.Throws<ArgumentNullException>(() => new NotCondition(Yes).IsSatisfiedBy(null!, AnyYear));
    }

    [Fact]
    public void And_and_Or_are_equal_by_their_operand_sequence() {
        ICondition first  = new AgeAtLeastCondition(10);
        ICondition second = new AgeAtLeastCondition(20);

        Assert.Equal(new AndCondition([first, second]), new AndCondition([new AgeAtLeastCondition(10), new AgeAtLeastCondition(20)]));
        Assert.NotEqual(new AndCondition([first, second]), new AndCondition([second, first]));
        Assert.NotEqual(new AndCondition([first, second]), new AndCondition([first, second, second]));
        Assert.Equal(new OrCondition([first, second]), new OrCondition([new AgeAtLeastCondition(10), new AgeAtLeastCondition(20)]));
        Assert.NotEqual(new OrCondition([first, second]), new OrCondition([second, first]));
        Assert.Equal(new AndCondition([first, second]).GetHashCode(), new AndCondition([new AgeAtLeastCondition(10), new AgeAtLeastCondition(20)]).GetHashCode());
        Assert.Equal(new OrCondition([first, second]).GetHashCode(), new OrCondition([new AgeAtLeastCondition(10), new AgeAtLeastCondition(20)]).GetHashCode());
    }

    [Fact]
    public void An_And_is_never_equal_to_an_Or_over_the_same_operands() {
        ICondition first  = new AgeAtLeastCondition(10);
        ICondition second = new AgeAtLeastCondition(20);

        Assert.False(new AndCondition([first, second]).Equals(new OrCondition([first, second])));
        Assert.False(new OrCondition([first, second]).Equals(new AndCondition([first, second])));
    }

    [Fact]
    public void Not_is_equal_by_its_operand() {
        Assert.Equal(new NotCondition(new AgeAtLeastCondition(10)), new NotCondition(new AgeAtLeastCondition(10)));
        Assert.NotEqual(new NotCondition(new AgeAtLeastCondition(10)), new NotCondition(new AgeAtLeastCondition(11)));
        Assert.Equal(new NotCondition(new AgeAtLeastCondition(10)).GetHashCode(), new NotCondition(new AgeAtLeastCondition(10)).GetHashCode());
    }

    [Fact]
    public void Combinations_describe_themselves() {
        Assert.Equal("(aged 10 or more and aged 20 or more)", new AndCondition([new AgeAtLeastCondition(10), new AgeAtLeastCondition(20)]).ToString());
        Assert.Equal("(aged 10 or more or aged 20 or more)", new OrCondition([new AgeAtLeastCondition(10), new AgeAtLeastCondition(20)]).ToString());
        Assert.Equal("not aged 10 or more", new NotCondition(new AgeAtLeastCondition(10)).ToString());
    }

}
