using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class HoldingPeriodTests {

    private static readonly DateTimeOffset From = new DateTimeOffset(2024, 3, 15, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void An_open_period_is_current() {
        // Arrange
        CreditSerial anySerial = Any.CreditSerial().Generate();

        // Act
        HoldingPeriod period = new HoldingPeriod(anySerial, "FR-000123", From, null);

        // Assert
        Assert.True(period.IsCurrent);
        Assert.Null(period.Until);
    }

    [Fact]
    public void A_closed_period_is_not_current() {
        CreditSerial anySerial = Any.CreditSerial().Generate();

        HoldingPeriod period = new HoldingPeriod(anySerial, "FR-000123", From, From.AddDays(1));

        Assert.False(period.IsCurrent);
        Assert.Equal(From.AddDays(1), period.Until);
    }

    [Fact]
    public void A_period_exposes_the_four_parts_it_was_built_from() {
        // Arrange
        CreditSerial serial = new CreditSerial("VCS", 1234, 2019, 42);

        // Act
        HoldingPeriod period = new HoldingPeriod(serial, "FR-000123", From, From.AddHours(5));

        // Assert
        Assert.Same(serial, period.Serial);
        Assert.Equal("FR-000123", period.AccountId);
        Assert.Equal(From, period.From);
        Assert.Equal(From.AddHours(5), period.Until);
    }

    [Fact]
    public void A_period_may_end_at_the_very_instant_it_starts() {
        CreditSerial anySerial = Any.CreditSerial().Generate();

        HoldingPeriod period = new HoldingPeriod(anySerial, "FR-000123", From, From);

        Assert.Equal(period.From, period.Until);
    }

    [Fact]
    public void A_period_cannot_end_a_tick_before_it_starts() {
        CreditSerial anySerial = Any.CreditSerial().Generate();

        Assert.Throws<ArgumentException>(() => new HoldingPeriod(anySerial, "FR-000123", From, From.AddTicks(-1)));
    }

    [Fact]
    public void Ends_are_compared_as_instants_not_as_local_clock_readings() {
        // Arrange: 10:00 at +02:00 is 08:00 UTC, so it ends before 09:00 UTC even though 10 > 09 on the wall clock
        DateTimeOffset startsAtNineUtc    = new DateTimeOffset(2024, 3, 15, 9, 0, 0, TimeSpan.Zero);
        DateTimeOffset endsAtEightUtc     = new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.FromHours(2));
        CreditSerial   anySerial          = Any.CreditSerial().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new HoldingPeriod(anySerial, "FR-000123", startsAtNineUtc, endsAtEightUtc));
    }

    [Fact]
    public void A_null_serial_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new HoldingPeriod(null!, "FR-000123", From, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    public void A_missing_or_blank_account_id_is_refused(string? accountId) {
        CreditSerial anySerial = Any.CreditSerial().Generate();

        Assert.ThrowsAny<ArgumentException>(() => new HoldingPeriod(anySerial, accountId!, From, null));
    }

    [Fact]
    public void The_account_id_of_a_period_is_not_checked_against_the_id_format() {
        // A period only names an account; whatever non-blank text it carries is kept as given.
        CreditSerial anySerial    = Any.CreditSerial().Generate();
        string       anyAccountId = Any.String().NotBlank().WithMaxLength(50).Generate();

        HoldingPeriod period = new HoldingPeriod(anySerial, anyAccountId, From, null);

        Assert.Equal(anyAccountId, period.AccountId);
    }

}
