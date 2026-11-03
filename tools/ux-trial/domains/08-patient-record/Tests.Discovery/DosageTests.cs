using Clinical.Domain;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Discovery;

public sealed class DosageTests {

    [Fact]
    public void A_dosage_keeps_what_it_was_given() {
        // Arrange
        DoseUnit anyUnit = Any.Enum<DoseUnit>().Generate();

        // Act
        Dosage dosage = new Dosage(2.5m, anyUnit, 3);

        // Assert
        Assert.Equal(2.5m, dosage.Amount);
        Assert.Equal(anyUnit, dosage.Unit);
        Assert.Equal(3, dosage.TimesPerDay);
    }

    [Fact]
    public void The_daily_amount_is_the_amount_times_the_intakes_per_day() {
        // Arrange
        DoseUnit anyUnit = Any.Enum<DoseUnit>().Generate();

        Dosage dosage = new Dosage(2.5m, anyUnit, 3);

        // Assert
        Assert.Equal(7.5m, dosage.DailyAmount);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("0.5")]
    [InlineData("9999.999")]
    [InlineData("10000")]
    [InlineData("1.230")]
    public void An_amount_in_the_open_closed_interval_with_at_most_three_decimals_is_accepted(string amount) {
        // Act
        Dosage dosage = new Dosage(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), DoseUnit.Milligram, 1);

        // Assert
        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), dosage.Amount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.001")]
    [InlineData("-5")]
    [InlineData("10000.001")]
    [InlineData("20000")]
    public void An_amount_outside_zero_exclusive_to_ten_thousand_inclusive_is_out_of_range(string amount) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Dosage(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), DoseUnit.Milligram, 1));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [InlineData("0.0001")]
    [InlineData("1.2345")]
    [InlineData("9999.9999")]
    public void An_amount_with_more_than_three_decimals_is_refused(string amount) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Dosage(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), DoseUnit.Milligram, 1));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_undeclared_unit_is_out_of_range() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(1m, (DoseUnit)99, 1));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void One_to_six_intakes_a_day_are_accepted(int timesPerDay) {
        // Act
        Dosage dosage = new Dosage(1m, DoseUnit.Milligram, timesPerDay);

        // Assert
        Assert.Equal(timesPerDay, dosage.TimesPerDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(7)]
    public void Fewer_than_one_or_more_than_six_intakes_a_day_are_out_of_range(int timesPerDay) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(1m, DoseUnit.Milligram, timesPerDay));

        // Assert
        Assert.Equal("timesPerDay", refusal.ParamName);
    }

}
