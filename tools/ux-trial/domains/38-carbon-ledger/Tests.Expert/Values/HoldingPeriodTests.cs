using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Values;

public sealed class HoldingPeriodTests {

    private static readonly DateTimeOffset Start = new DateTimeOffset(2024, 3, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_period_without_an_end_is_still_current() {
        HoldingPeriod period = Any.HoldingPeriod().WithFrom(Start).WithoutUntil().Generate();

        Assert.True(period.IsCurrent);
        Assert.Null(period.Until);
    }

    [Fact]
    public void A_period_with_an_end_is_not_current() {
        HoldingPeriod period = Any.HoldingPeriod().WithFrom(Start).WithUntil(Start.AddDays(1)).Generate();

        Assert.False(period.IsCurrent);
        Assert.Equal(Start.AddDays(1), period.Until);
    }

    [Fact]
    public void A_period_may_end_the_very_instant_it_starts() {
        HoldingPeriod period = Any.HoldingPeriod().WithFrom(Start).WithUntil(Start).Generate();

        Assert.Equal(period.From, period.Until);
    }

    [Fact]
    public void A_period_cannot_end_before_it_starts() {
        CreditSerial anySerial = Any.CreditSerial().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new HoldingPeriod(anySerial, "FR-000123", Start, Start.AddTicks(-1)));

        Assert.Equal("until", refusal.ParamName);
    }

    [Fact]
    public void A_period_keeps_the_unit_the_account_and_the_start_it_was_given() {
        // Arrange
        CreditSerial anySerial = Any.CreditSerial().Generate();

        // Act
        HoldingPeriod period = new HoldingPeriod(anySerial, "FR-000123", Start, null);

        // Assert
        Assert.Same(anySerial, period.Serial);
        Assert.Equal("FR-000123", period.AccountId);
        Assert.Equal(Start, period.From);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_period_needs_an_account(string accountId) {
        Assert.Throws<ArgumentException>(() => Any.HoldingPeriod().WithAccountId(accountId).Generate());
    }

    [Fact]
    public void A_missing_account_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new HoldingPeriod(Any.CreditSerial().Generate(), null!, Start, null));
    }

    [Fact]
    public void A_missing_unit_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new HoldingPeriod(null!, "FR-000123", Start, null));
    }

}
