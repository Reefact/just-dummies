using Insurance.Domain;

using JustDummies;

using Xunit;

namespace Insurance.Tests.Regular;

public sealed class ClaimTests {

    [Fact]
    public void A_claim_keeps_what_it_was_built_from() {
        // Arrange
        Guid           anyId             = Any.Guid().NonEmpty().Generate();
        DateOnly       anyOccurredOn     = Any.DateOnly().Generate();
        decimal        anyDamage         = Any.Decimal().Positive().Generate();
        Responsibility anyResponsibility = Any.Enum<Responsibility>().Generate();

        // Act
        Claim claim = new Claim(anyId, anyOccurredOn, anyDamage, anyResponsibility);

        // Assert
        Assert.Equal(anyId, claim.Id);
        Assert.Equal(anyOccurredOn, claim.OccurredOn);
        Assert.Equal(anyDamage, claim.Damage);
        Assert.Equal(anyResponsibility, claim.Responsibility);
    }

    [Fact]
    public void An_empty_identifier_is_refused() {
        Assert.Throws<ArgumentException>(() => new Claim(Guid.Empty, Any.DateOnly().Generate(), 100m, Responsibility.None));
    }

    [Fact]
    public void A_zero_damage_is_out_of_range() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Claim(Any.Guid().NonEmpty().Generate(), Any.DateOnly().Generate(), 0m, Responsibility.None));
    }

    [Fact]
    public void A_negative_damage_is_out_of_range() {
        decimal anyNegative = Any.Decimal().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Claim(Any.Guid().NonEmpty().Generate(), Any.DateOnly().Generate(), anyNegative, Responsibility.None));
    }

    [Fact]
    public void The_smallest_positive_damage_is_accepted() {
        Assert.Equal(0.0000000000000000000000000001m, Any.Claim().WithDamage(0.0000000000000000000000000001m).Generate().Damage);
    }

    [Fact]
    public void There_is_no_upper_bound_on_the_damage() {
        Assert.Equal(decimal.MaxValue, Any.Claim().WithDamage(decimal.MaxValue).Generate().Damage);
    }

    [Theory]
    [InlineData((Responsibility)3)]
    [InlineData((Responsibility)(-1))]
    [InlineData((Responsibility)42)]
    public void An_undefined_responsibility_is_out_of_range(Responsibility responsibility) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Claim(Any.Guid().NonEmpty().Generate(), Any.DateOnly().Generate(), 100m, responsibility));
    }

    [Fact]
    public void The_identifier_is_checked_before_the_damage_and_the_damage_before_the_responsibility() {
        ArgumentException idFirst = Assert.Throws<ArgumentException>(() => new Claim(Guid.Empty, Any.DateOnly().Generate(), 0m, (Responsibility)42));
        ArgumentOutOfRangeException damageNext = Assert.Throws<ArgumentOutOfRangeException>(() => new Claim(Any.Guid().NonEmpty().Generate(), Any.DateOnly().Generate(), 0m, (Responsibility)42));

        Assert.Equal("id", idFirst.ParamName);
        Assert.Equal("damage", damageNext.ParamName);
    }

    [Theory]
    [InlineData(Responsibility.None, 1.00)]
    [InlineData(Responsibility.Partial, 0.50)]
    [InlineData(Responsibility.Full, 0.00)]
    public void The_insured_share_follows_the_responsibility(Responsibility responsibility, double expected) {
        Claim claim = Any.Claim().WithResponsibility(responsibility).Generate();

        Assert.Equal((decimal)expected, claim.InsuredShare);
    }

}
