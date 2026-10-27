using Insurance.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Insurance.Tests;

[Reproducible]
public sealed class ValueObjectTests {

    [Fact]
    public void A_coverage_period_must_end_after_it_starts() {
        // Arrange
        DateOnly anyStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2040, 12, 31)).Generate();

        // Act
        Func<CoveragePeriod> sameDay = () => new CoveragePeriod(anyStart, anyStart);

        // Assert
        Assert.Throws<ArgumentException>("end", sameDay);
    }

    [Fact]
    public void A_coverage_period_of_one_year_and_a_day_is_refused() {
        // Act
        Func<CoveragePeriod> tooLong = () => new CoveragePeriod(new DateOnly(2026, 3, 1), new DateOnly(2027, 3, 2));

        // Assert
        Assert.Throws<ArgumentException>("end", tooLong);
    }

    [Fact]
    public void The_next_period_starts_the_day_after_and_lasts_as_long() {
        // Arrange
        CoveragePeriod spring = new(new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 31));

        // Act
        CoveragePeriod next = spring.Next();

        // Assert
        Assert.Equal(new DateOnly(2026, 6, 1), next.Start);
        Assert.Equal(spring.Days, next.Days);
    }

    [Fact]
    public void A_claim_free_year_earns_a_five_percent_bonus() {
        Assert.Equal(0.95, BonusMalus.NewDriver.AfterYear(responsibleClaims: 0, partialClaims: 0).Coefficient);
    }

    [Fact]
    public void The_bonus_never_goes_below_its_floor() {
        Assert.Equal(0.50, new BonusMalus(0.51).AfterYear(responsibleClaims: 0, partialClaims: 0).Coefficient);
    }

    [Fact]
    public void The_malus_never_goes_above_its_ceiling() {
        Assert.Equal(3.50, new BonusMalus(3.00).AfterYear(responsibleClaims: 1, partialClaims: 0).Coefficient);
    }

    [Fact]
    public void A_policy_number_carries_an_upper_case_country_code() {
        // Act
        Func<PolicyNumber> lowerCase = () => new PolicyNumber("POL-2026-fr123456");

        // Assert
        Assert.Throws<ArgumentException>("value", lowerCase);
    }

    [Fact]
    public void A_driver_name_is_trimmed() {
        // Arrange
        Driver anyDriver = Any.Driver().WithFullName("  Ada Lovelace ").Generate();

        // Assert
        Assert.Equal("Ada Lovelace", anyDriver.FullName);
    }

}
