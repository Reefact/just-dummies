using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests;

public sealed class ClaimTests {

    [Fact]
    public void A_claim_keeps_what_it_was_given() {
        // Arrange
        Guid id = new Guid("5d0f3f4e-6a5a-4a63-9a43-0c1b5e7f2a10");

        // Act
        Claim claim = new Claim(id, new DateOnly(2026, 7, 14), 1_250.40m, Responsibility.Partial);

        // Assert
        Assert.Equal(id, claim.Id);
        Assert.Equal(new DateOnly(2026, 7, 14), claim.OccurredOn);
        Assert.Equal(1_250.40m, claim.Damage);
        Assert.Equal(Responsibility.Partial, claim.Responsibility);
    }

    [Theory]
    [InlineData(Responsibility.None,    "1.00")]
    [InlineData(Responsibility.Partial, "0.50")]
    [InlineData(Responsibility.Full,    "0.00")]
    public void The_share_the_third_partys_insurer_reimburses_follows_the_responsibility(Responsibility responsibility, string expectedShare) {
        // Arrange
        Claim claim = Any.Claim().WithResponsibility(responsibility).Generate();

        // Act
        decimal share = claim.InsuredShare;

        // Assert
        Assert.Equal(decimal.Parse(expectedShare, System.Globalization.CultureInfo.InvariantCulture), share);
    }

    [Fact]
    public void An_empty_identifier_is_refused_as_a_plain_argument_error() {
        // Act + Assert (the exact type: the damage and the responsibility are fine)
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Claim().WithId(Guid.Empty).Generate());
        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-5000")]
    public void A_damage_that_is_not_strictly_positive_is_out_of_range(string text) {
        // Arrange
        decimal damage = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act + Assert
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Claim().WithDamage(damage).Generate());
        Assert.Equal("damage", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.0001")]   // no decimal-place rule
    [InlineData("79228162514264337593543950335")]   // nor any upper bound: decimal.MaxValue
    public void Any_strictly_positive_damage_is_accepted(string text) {
        // Arrange
        decimal damage = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        Claim claim = Any.Claim().WithDamage(damage).Generate();

        // Assert
        Assert.Equal(damage, claim.Damage);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void A_responsibility_that_is_not_a_declared_member_is_out_of_range(int undefined) {
        // Act + Assert
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Claim().WithResponsibility((Responsibility)undefined).Generate());
        Assert.Equal("responsibility", refusal.ParamName);
    }

    [Fact]
    public void The_identifier_is_checked_before_the_damage() {
        // Act + Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Claim().WithId(Guid.Empty).WithDamage(0m).Generate());
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void The_damage_is_checked_before_the_responsibility() {
        // Act + Assert
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Claim().WithDamage(0m).WithResponsibility((Responsibility)42).Generate());
        Assert.Equal("damage", refusal.ParamName);
    }

}
