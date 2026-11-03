using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests.Discovery;

public sealed class LeafConditionTests {

    private const int AnyYear = 2024;

    [Fact]
    public void Income_must_be_strictly_above_the_threshold() {
        // Arrange
        IncomeAboveCondition condition = new IncomeAboveCondition(IncomeKind.Employment, new Money(30_000m));

        // Act and assert
        Assert.False(condition.IsSatisfiedBy(Fixtures.TaxpayerEarning(29_999.99m), AnyYear));
        Assert.False(condition.IsSatisfiedBy(Fixtures.TaxpayerEarning(30_000m), AnyYear));
        Assert.True(condition.IsSatisfiedBy(Fixtures.TaxpayerEarning(30_000.01m), AnyYear));
    }

    [Fact]
    public void Income_above_a_threshold_counts_only_the_kinds_it_names() {
        // Arrange
        Taxpayer             taxpayer = Fixtures.EarningTaxpayer(Fixtures.Salary(20_000m), Fixtures.Interest(15_000m));
        IncomeAboveCondition employmentOnly = new IncomeAboveCondition(IncomeKind.Employment, new Money(30_000m));
        IncomeAboveCondition both           = new IncomeAboveCondition(IncomeKind.Employment | IncomeKind.Investment, new Money(30_000m));
        IncomeAboveCondition everything     = new IncomeAboveCondition(IncomeKind.All, new Money(34_999.99m));

        // Act and assert
        Assert.False(employmentOnly.IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(both.IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(everything.IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Income_above_zero_is_not_satisfied_by_a_taxpayer_without_income() {
        IncomeAboveCondition condition = new IncomeAboveCondition(IncomeKind.All, Money.Zero);

        Assert.False(condition.IsSatisfiedBy(Fixtures.EarningTaxpayer(), AnyYear));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    public void A_tax_base_naming_no_kind_or_an_unknown_one_is_refused(int kinds) {
        Money anyThreshold = Any.Money().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new IncomeAboveCondition((IncomeKind)kinds, anyThreshold));
    }

    [Fact]
    public void A_missing_threshold_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new IncomeAboveCondition(IncomeKind.All, null!));
    }

    [Fact]
    public void Residency_in_a_country_covers_its_subdivisions() {
        // Arrange
        ResidentOfCondition condition = new ResidentOfCondition(new JurisdictionCode("US"));

        // Act and assert
        Assert.True(condition.IsSatisfiedBy(Any.Taxpayer().WithResidence(new JurisdictionCode("US")).Generate(), AnyYear));
        Assert.True(condition.IsSatisfiedBy(Any.Taxpayer().WithResidence(new JurisdictionCode("US-CA")).Generate(), AnyYear));
        Assert.False(condition.IsSatisfiedBy(Any.Taxpayer().WithResidence(new JurisdictionCode("CA")).Generate(), AnyYear));
    }

    [Fact]
    public void Residency_in_a_subdivision_is_not_satisfied_by_the_country_or_a_sibling() {
        ResidentOfCondition condition = new ResidentOfCondition(new JurisdictionCode("US-CA"));

        Assert.True(condition.IsSatisfiedBy(Any.Taxpayer().WithResidence(new JurisdictionCode("US-CA")).Generate(), AnyYear));
        Assert.False(condition.IsSatisfiedBy(Any.Taxpayer().WithResidence(new JurisdictionCode("US")).Generate(), AnyYear));
        Assert.False(condition.IsSatisfiedBy(Any.Taxpayer().WithResidence(new JurisdictionCode("US-NY")).Generate(), AnyYear));
    }

    [Fact]
    public void A_missing_jurisdiction_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ResidentOfCondition(null!));
    }

    [Theory]
    [InlineData(1950, 2024, 74, true)]
    [InlineData(1950, 2024, 75, false)]
    [InlineData(1950, 2024, 1, true)]
    [InlineData(2024, 2024, 1, false)]
    public void Age_is_counted_in_years_at_the_end_of_the_tax_year(int birthYear, int taxYear, int years, bool expected) {
        // Arrange
        Taxpayer taxpayer = Any.Taxpayer().WithDateOfBirth(new DateOnly(birthYear, 6, 15)).Generate();

        // Act
        bool satisfied = new AgeAtLeastCondition(years).IsSatisfiedBy(taxpayer, taxYear);

        // Assert
        Assert.Equal(expected, satisfied);
    }

    [Fact]
    public void The_exact_birth_date_within_the_year_does_not_matter_for_age() {
        Taxpayer bornInJanuary  = Any.Taxpayer().WithDateOfBirth(new DateOnly(1980, 1, 1)).Generate();
        Taxpayer bornInDecember = Any.Taxpayer().WithDateOfBirth(new DateOnly(1980, 12, 31)).Generate();
        AgeAtLeastCondition condition = new AgeAtLeastCondition(44);

        Assert.True(condition.IsSatisfiedBy(bornInJanuary, 2024));
        Assert.True(condition.IsSatisfiedBy(bornInDecember, 2024));
    }

    [Fact]
    public void Age_in_a_year_before_the_birth_is_refused() {
        Taxpayer taxpayer = Any.Taxpayer().WithDateOfBirth(new DateOnly(1990, 5, 5)).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new AgeAtLeastCondition(1).IsSatisfiedBy(taxpayer, 1989));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(120, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    [InlineData(121, false)]
    public void An_age_threshold_lies_between_one_and_one_hundred_twenty(int years, bool accepted) {
        if (accepted) {
            Assert.Equal(years, new AgeAtLeastCondition(years).Years);
        } else {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AgeAtLeastCondition(years));
        }
    }

    [Theory]
    [InlineData(3, 2, true)]
    [InlineData(3, 3, true)]
    [InlineData(3, 4, false)]
    [InlineData(1, 2, false)]
    [InlineData(20, 20, true)]
    [InlineData(19, 20, false)]
    public void A_household_size_condition_is_met_at_or_above_the_given_size(int householdSize, int members, bool expected) {
        // Arrange
        Taxpayer taxpayer = Any.Taxpayer().WithHouseholdSize(householdSize).Generate();

        // Act
        bool satisfied = new HouseholdSizeAtLeastCondition(members).IsSatisfiedBy(taxpayer, AnyYear);

        // Assert
        Assert.Equal(expected, satisfied);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(20, true)]
    [InlineData(1, false)]
    [InlineData(0, false)]
    [InlineData(21, false)]
    public void A_household_size_condition_asks_for_two_to_twenty_members(int members, bool accepted) {
        if (accepted) {
            Assert.Equal(members, new HouseholdSizeAtLeastCondition(members).Members);
        } else {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HouseholdSizeAtLeastCondition(members));
        }
    }

    [Fact]
    public void Every_leaf_has_depth_one_and_no_operand() {
        ICondition[] leaves = [
            new IncomeAboveCondition(IncomeKind.All, Money.Zero),
            new ResidentOfCondition(new JurisdictionCode("FR")),
            new AgeAtLeastCondition(18),
            new HouseholdSizeAtLeastCondition(2)
        ];

        Assert.All(leaves, leaf => {
            Assert.Equal(1, leaf.Depth);
            Assert.Empty(leaf.Operands);
        });
    }

    [Fact]
    public void Leaves_equal_by_value() {
        Assert.Equal(new AgeAtLeastCondition(18), new AgeAtLeastCondition(18));
        Assert.NotEqual(new AgeAtLeastCondition(18), new AgeAtLeastCondition(19));
        Assert.Equal(new HouseholdSizeAtLeastCondition(3), new HouseholdSizeAtLeastCondition(3));
        Assert.NotEqual(new HouseholdSizeAtLeastCondition(3), new HouseholdSizeAtLeastCondition(4));
        Assert.Equal(new ResidentOfCondition(new JurisdictionCode("FR")), new ResidentOfCondition(new JurisdictionCode("FR")));
        Assert.NotEqual(new ResidentOfCondition(new JurisdictionCode("FR")), new ResidentOfCondition(new JurisdictionCode("DE")));
        Assert.Equal(new IncomeAboveCondition(IncomeKind.Employment, new Money(5m)), new IncomeAboveCondition(IncomeKind.Employment, new Money(5m)));
        Assert.NotEqual(new IncomeAboveCondition(IncomeKind.Employment, new Money(5m)), new IncomeAboveCondition(IncomeKind.Investment, new Money(5m)));
        Assert.NotEqual(new IncomeAboveCondition(IncomeKind.Employment, new Money(5m)), new IncomeAboveCondition(IncomeKind.Employment, new Money(6m)));
    }

    [Fact]
    public void A_leaf_never_equals_a_leaf_of_another_kind_even_with_matching_numbers() {
        object age       = new AgeAtLeastCondition(2);
        object household = new HouseholdSizeAtLeastCondition(2);

        Assert.False(age.Equals(household));
        Assert.NotEqual(age.GetHashCode(), household.GetHashCode());
    }

    [Fact]
    public void Every_leaf_refuses_a_missing_taxpayer() {
        ICondition[] leaves = [
            Any.LeafCondition().Generate(), Any.LeafCondition().Generate(), Any.LeafCondition().Generate(),
            new IncomeAboveCondition(IncomeKind.All, Money.Zero),
            new ResidentOfCondition(new JurisdictionCode("FR")),
            new AgeAtLeastCondition(18),
            new HouseholdSizeAtLeastCondition(2)
        ];

        Assert.All(leaves, leaf => Assert.Throws<ArgumentNullException>(() => leaf.IsSatisfiedBy(null!, AnyYear)));
    }

}

public sealed class CombinedConditionTests {

    private const int AnyYear = 2024;

    // A taxpayer of a household of 3, born in 1980: "household of 3 or more" and "aged 44 or more" hold, the others do not.
    private static Taxpayer Taxpayer3In1980() {
        return Any.Taxpayer().WithHouseholdSize(3).WithDateOfBirth(new DateOnly(1980, 3, 3)).Generate();
    }

    private static readonly ICondition Holds1 = new HouseholdSizeAtLeastCondition(3);
    private static readonly ICondition Holds2 = new AgeAtLeastCondition(44);
    private static readonly ICondition Fails1 = new HouseholdSizeAtLeastCondition(4);
    private static readonly ICondition Fails2 = new AgeAtLeastCondition(45);

    [Fact]
    public void And_requires_every_operand() {
        Taxpayer taxpayer = Taxpayer3In1980();

        Assert.True(new AndCondition([Holds1, Holds2]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new AndCondition([Holds1, Fails2]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new AndCondition([Fails1, Holds2]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new AndCondition([Fails1, Fails2]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new AndCondition([Holds1, Holds2, Fails1]).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Or_requires_any_operand() {
        Taxpayer taxpayer = Taxpayer3In1980();

        Assert.True(new OrCondition([Holds1, Holds2]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new OrCondition([Holds1, Fails2]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new OrCondition([Fails1, Holds2]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.False(new OrCondition([Fails1, Fails2]).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new OrCondition([Fails1, Fails2, Holds1]).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Not_inverts_its_operand() {
        Taxpayer taxpayer = Taxpayer3In1980();

        Assert.False(new NotCondition(Holds1).IsSatisfiedBy(taxpayer, AnyYear));
        Assert.True(new NotCondition(Fails1).IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void Combinations_nest() {
        Taxpayer taxpayer = Taxpayer3In1980();

        // not (fails1 or fails2) and holds1  ->  true and true
        ICondition condition = new AndCondition([new NotCondition(new OrCondition([Fails1, Fails2])), Holds1]);

        Assert.True(condition.IsSatisfiedBy(taxpayer, AnyYear));
    }

    [Fact]
    public void The_tax_year_reaches_the_leaves() {
        Taxpayer taxpayer = Taxpayer3In1980();
        ICondition condition = new AndCondition([Holds1, new AgeAtLeastCondition(50)]);

        Assert.False(condition.IsSatisfiedBy(taxpayer, 2024));
        Assert.True(condition.IsSatisfiedBy(taxpayer, 2030));
    }

    [Fact]
    public void Depth_is_one_more_than_the_deepest_operand() {
        Assert.Equal(2, new AndCondition([Holds1, Holds2]).Depth);
        Assert.Equal(3, new OrCondition([new AndCondition([Holds1, Holds2]), Holds1]).Depth);
        Assert.Equal(2, new NotCondition(Holds1).Depth);
        Assert.Equal(3, new NotCondition(new AndCondition([Holds1, Holds2])).Depth);
    }

    [Fact]
    public void A_combination_exposes_its_operands_in_order() {
        Assert.Equal([Holds1, Fails2], new AndCondition([Holds1, Fails2]).Operands);
        Assert.Equal([Holds1, Fails2], new OrCondition([Holds1, Fails2]).Operands);
        Assert.Equal([Holds1], new NotCondition(Holds1).Operands);
    }

    [Fact]
    public void A_combination_depth_of_eight_is_the_deepest_allowed() {
        ICondition deepest = Fixtures.ChainOfDepth(ICondition.MaxDepth);

        Assert.Equal(8, deepest.Depth);
    }

    [Fact]
    public void An_and_one_level_below_the_deepest_is_refused() {
        ICondition deepest = Fixtures.ChainOfDepth(ICondition.MaxDepth);
        ICondition anyLeaf = Any.LeafCondition().Generate();

        Assert.Throws<ArgumentException>(() => new AndCondition([deepest, anyLeaf]));
        Assert.Throws<ArgumentException>(() => new OrCondition([anyLeaf, deepest]));
    }

    [Fact]
    public void A_negation_follows_the_same_depth_limit() {
        ICondition seven   = Fixtures.ChainOfDepth(ICondition.MaxDepth - 1);
        ICondition deepest = Fixtures.ChainOfDepth(ICondition.MaxDepth);

        Assert.Equal(8, new NotCondition(seven).Depth);
        Assert.Throws<ArgumentException>(() => new NotCondition(deepest));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void A_combination_joins_at_least_two_operands(int operandCount) {
        List<ICondition> operands = Enumerable.Range(0, operandCount).Select(_ => Any.LeafCondition().Generate()).ToList();

        Assert.Throws<ArgumentException>(() => new AndCondition(operands));
        Assert.Throws<ArgumentException>(() => new OrCondition(operands));
    }

    [Fact]
    public void A_combination_with_a_null_operand_is_refused() {
        ICondition anyLeaf = Any.LeafCondition().Generate();

        Assert.Throws<ArgumentException>(() => new AndCondition([anyLeaf, null!]));
        Assert.Throws<ArgumentException>(() => new OrCondition([null!, anyLeaf]));
    }

    [Fact]
    public void A_missing_operand_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AndCondition(null!));
        Assert.Throws<ArgumentNullException>(() => new OrCondition(null!));
        Assert.Throws<ArgumentNullException>(() => new NotCondition(null!));
    }

    [Fact]
    public void A_double_negation_is_refused() {
        NotCondition negation = new NotCondition(Any.LeafCondition().Generate());

        Assert.Throws<ArgumentException>(() => new NotCondition(negation));
    }

    [Fact]
    public void A_negation_of_a_combination_containing_a_negation_is_allowed() {
        ICondition inner = new AndCondition([new NotCondition(Holds1), Holds2]);

        Assert.Equal(4, new NotCondition(inner).Depth);
    }

    [Fact]
    public void The_operands_cannot_be_changed_through_the_list_they_were_built_from() {
        List<ICondition> operands = [Holds1, Holds2];
        AndCondition     and      = new AndCondition(operands);

        operands.Add(Fails1);

        Assert.Equal(2, and.Operands.Count);
    }

    [Fact]
    public void Combinations_equal_by_their_operand_sequence() {
        Assert.Equal(new AndCondition([Holds1, Holds2]), new AndCondition([Holds1, Holds2]));
        Assert.Equal(new AndCondition([Holds1, Holds2]).GetHashCode(), new AndCondition([Holds1, Holds2]).GetHashCode());
        Assert.NotEqual(new AndCondition([Holds1, Holds2]), new AndCondition([Holds2, Holds1]));
        Assert.NotEqual(new AndCondition([Holds1, Holds2]), new AndCondition([Holds1, Holds2, Fails1]));
        Assert.Equal(new OrCondition([Holds1, Holds2]), new OrCondition([Holds1, Holds2]));
        Assert.NotEqual(new OrCondition([Holds1, Holds2]), new OrCondition([Holds2, Holds1]));
        Assert.Equal(new NotCondition(Holds1), new NotCondition(new HouseholdSizeAtLeastCondition(3)));
        Assert.NotEqual(new NotCondition(Holds1), new NotCondition(Holds2));
    }

    [Fact]
    public void An_and_never_equals_an_or_over_the_same_operands_nor_a_not() {
        object and = new AndCondition([Holds1, Holds2]);
        object or  = new OrCondition([Holds1, Holds2]);

        Assert.False(and.Equals(or));
        Assert.False(or.Equals(and));
        Assert.False(new NotCondition(Holds1).Equals(Holds1));
    }

    [Fact]
    public void Combinations_describe_themselves() {
        Assert.Equal("(household of 3 or more and aged 44 or more)", new AndCondition([Holds1, Holds2]).ToString());
        Assert.Equal("(household of 3 or more or aged 44 or more)", new OrCondition([Holds1, Holds2]).ToString());
        Assert.Equal("not household of 3 or more", new NotCondition(Holds1).ToString());
    }

    [Fact]
    public void A_combination_refuses_a_missing_taxpayer() {
        Assert.Throws<ArgumentNullException>(() => new AndCondition([Holds1, Holds2]).IsSatisfiedBy(null!, AnyYear));
        Assert.Throws<ArgumentNullException>(() => new OrCondition([Holds1, Holds2]).IsSatisfiedBy(null!, AnyYear));
        Assert.Throws<ArgumentNullException>(() => new NotCondition(Holds1).IsSatisfiedBy(null!, AnyYear));
    }

    [Fact]
    public void A_generated_condition_is_always_constructible_and_within_the_depth_limit() {
        for (int i = 0; i < 100; i++) {
            ICondition anyCondition = Any.Condition().Generate();

            Assert.InRange(anyCondition.Depth, 1, ICondition.MaxDepth);
        }
    }

}
