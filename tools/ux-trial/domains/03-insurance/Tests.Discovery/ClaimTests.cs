using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests.Discovery;

public sealed class ClaimTests {

    [Fact]
    public void A_claim_keeps_what_it_was_declared_with() {
        // Arrange
        Guid id = new Guid("11111111-2222-3333-4444-555555555555");

        // Act
        Claim claim = new Claim(id, new DateOnly(2026, 4, 2), 1234.56m, Responsibility.Partial);

        // Assert
        Assert.Equal(id, claim.Id);
        Assert.Equal(new DateOnly(2026, 4, 2), claim.OccurredOn);
        Assert.Equal(1234.56m, claim.Damage);
        Assert.Equal(Responsibility.Partial, claim.Responsibility);
    }

    [Fact]
    public void An_empty_identifier_is_refused() {
        // Arrange
        DateOnly anyDate   = Any.DateOnly().Generate();
        decimal  anyDamage = Any.Decimal().Positive().Generate();

        // Assert
        Assert.Throws<ArgumentException>("id", () => new Claim(Guid.Empty, anyDate, anyDamage, Responsibility.None));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1000")]
    public void A_damage_that_is_not_positive_is_refused(string text) {
        // Arrange
        decimal  damage  = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        Guid     anyId   = Any.Guid().NonEmpty().Generate();
        DateOnly anyDate = Any.DateOnly().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("damage", () => new Claim(anyId, anyDate, damage, Responsibility.Full));
    }

    [Fact]
    public void The_smallest_possible_damage_is_accepted_and_there_is_no_ceiling() {
        // Arrange
        Guid     anyId   = Any.Guid().NonEmpty().Generate();
        DateOnly anyDate = Any.DateOnly().Generate();

        // Assert
        Assert.Equal(0.01m, new Claim(anyId, anyDate, 0.01m, Responsibility.None).Damage);
        Assert.Equal(1_000_000_000m, new Claim(anyId, anyDate, 1_000_000_000m, Responsibility.None).Damage);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void An_undefined_responsibility_is_refused(int undefined) {
        // Arrange
        Guid     anyId     = Any.Guid().NonEmpty().Generate();
        DateOnly anyDate   = Any.DateOnly().Generate();
        decimal  anyDamage = Any.Decimal().Positive().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("responsibility", () => new Claim(anyId, anyDate, anyDamage, (Responsibility)undefined));
    }

    [Fact]
    public void The_responsibility_is_checked_after_the_damage() {
        // Arrange
        Guid     anyId   = Any.Guid().NonEmpty().Generate();
        DateOnly anyDate = Any.DateOnly().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Claim(anyId, anyDate, 0m, (Responsibility)99));

        // Assert
        Assert.Equal("damage", error.ParamName);
    }

    [Theory]
    [InlineData(Responsibility.None, 1.00)]
    [InlineData(Responsibility.Partial, 0.50)]
    [InlineData(Responsibility.Full, 0.00)]
    public void The_insured_share_follows_the_responsibility(Responsibility responsibility, double expectedShare) {
        // Arrange
        Claim claim = new AnyClaim().WithResponsibility(responsibility).Generate();

        // Assert
        Assert.Equal((decimal)expectedShare, claim.InsuredShare);
    }

}
