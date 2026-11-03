using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class HoldingPeriodTests {

    [Fact]
    public void A_period_without_an_end_is_current() {
        // Arrange
        CreditSerial   anySerial    = new AnyCreditSerial().Generate();
        string         anyAccountId = Any.String().NotBlank().Generate();
        DateTimeOffset anyFrom      = Any.DateTimeOffset().Generate();

        // Act
        HoldingPeriod period = new HoldingPeriod(anySerial, anyAccountId, anyFrom, null);

        // Assert
        Assert.True(period.IsCurrent);
        Assert.Null(period.Until);
    }

    [Fact]
    public void A_period_with_an_end_is_not_current() {
        // Arrange
        CreditSerial   anySerial    = new AnyCreditSerial().Generate();
        string         anyAccountId = Any.String().NotBlank().Generate();
        DateTimeOffset anyFrom      = Any.DateTimeOffset().Between(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddYears(100)).Generate();

        // Act
        HoldingPeriod period = new HoldingPeriod(anySerial, anyAccountId, anyFrom, anyFrom.AddDays(1));

        // Assert
        Assert.False(period.IsCurrent);
        Assert.Equal(anyFrom.AddDays(1), period.Until);
    }

    [Fact]
    public void A_period_may_end_at_the_very_instant_it_starts() {
        // Arrange
        CreditSerial   anySerial    = new AnyCreditSerial().Generate();
        string         anyAccountId = Any.String().NotBlank().Generate();
        DateTimeOffset anyInstant   = Any.DateTimeOffset().Generate();

        // Act
        HoldingPeriod period = new HoldingPeriod(anySerial, anyAccountId, anyInstant, anyInstant);

        // Assert
        Assert.Equal(anyInstant, period.From);
        Assert.Equal(anyInstant, period.Until);
    }

    [Fact]
    public void A_period_cannot_end_before_it_starts() {
        // Arrange
        CreditSerial   anySerial    = new AnyCreditSerial().Generate();
        string         anyAccountId = Any.String().NotBlank().Generate();
        DateTimeOffset anyFrom      = Any.DateTimeOffset().Between(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddYears(100)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new HoldingPeriod(anySerial, anyAccountId, anyFrom, anyFrom.AddTicks(-1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_account_id_is_refused(string accountId) {
        // Arrange
        CreditSerial   anySerial = new AnyCreditSerial().Generate();
        DateTimeOffset anyFrom   = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new HoldingPeriod(anySerial, accountId, anyFrom, null));
    }

    [Fact]
    public void A_null_serial_or_account_id_is_refused() {
        // Arrange
        CreditSerial   anySerial    = new AnyCreditSerial().Generate();
        string         anyAccountId = Any.String().NotBlank().Generate();
        DateTimeOffset anyFrom      = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HoldingPeriod(null!, anyAccountId, anyFrom, null));
        Assert.Throws<ArgumentNullException>(() => new HoldingPeriod(anySerial, null!, anyFrom, null));
    }

}
