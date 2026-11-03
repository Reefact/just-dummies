using JustDummies;

using Xunit;

namespace Clinical.Domain;

public sealed class DosageTests {

    [Fact]
    public void The_daily_amount_is_the_amount_times_the_intakes_per_day() {
        // Arrange
        DoseUnit anyUnit = Any.Enum<DoseUnit>().Generate();

        // Act
        Dosage dosage = new Dosage(2.5m, anyUnit, 3);

        // Assert
        Assert.Equal(7.5m, dosage.DailyAmount);
    }

    [Fact]
    public void The_dosage_keeps_what_it_was_given() {
        // Act
        Dosage dosage = new Dosage(500m, DoseUnit.Microgram, 2);

        // Assert
        Assert.Equal(500m, dosage.Amount);
        Assert.Equal(DoseUnit.Microgram, dosage.Unit);
        Assert.Equal(2, dosage.TimesPerDay);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("10000")]
    [InlineData("1.23")]
    [InlineData("1.2300")]   // a trailing zero is not a fourth decimal
    public void An_amount_in_the_open_closed_range_with_three_decimals_at_most_is_accepted(string amount) {
        // Arrange
        DoseUnit anyUnit  = Any.Enum<DoseUnit>().Generate();
        int      anyTimes = Any.Int32().Between(1, 6).Generate();

        // Act
        Dosage dosage = new Dosage(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), anyUnit, anyTimes);

        // Assert
        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), dosage.Amount);
    }

    [Fact]
    public void A_zero_amount_is_out_of_range() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(0m, DoseUnit.Milligram, 1));
    }

    [Fact]
    public void A_negative_amount_is_out_of_range() {
        // Arrange
        decimal anyNegativeAmount = Any.Decimal().Negative().WithScale(3).Generate();

        // Act
        Action construct = () => new Dosage(anyNegativeAmount, DoseUnit.Milligram, 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void An_amount_above_ten_thousand_is_out_of_range() {
        // Arrange
        decimal anyExcessiveAmount = Any.Decimal().GreaterThan(10_000m).LessThan(1_000_000m).WithScale(3).Generate();

        // Act
        Action construct = () => new Dosage(anyExcessiveAmount, DoseUnit.Milligram, 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void The_smallest_excess_over_ten_thousand_is_out_of_range() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(10_000.001m, DoseUnit.Milligram, 1));
    }

    [Theory]
    [InlineData("1.0001")]
    [InlineData("0.0005")]
    [InlineData("9999.9999")]
    public void An_amount_with_four_decimals_is_an_argument_exception(string amount) {
        // Arrange
        decimal fourDecimals = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        Action construct = () => new Dosage(fourDecimals, DoseUnit.Milligram, 1);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void The_range_is_checked_before_the_decimals() {
        // 10 000.0001 is both too large and too precise: the range error wins.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(10_000.0001m, DoseUnit.Milligram, 1));
    }

    [Fact]
    public void An_undefined_unit_is_out_of_range() {
        // Arrange
        decimal anyAmount = Any.Decimal().Positive().LessThanOrEqualTo(10_000m).WithScale(3).Generate();

        // Act
        Action construct = () => new Dosage(anyAmount, (DoseUnit)99, 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void One_to_six_intakes_a_day_are_accepted(int timesPerDay) {
        Dosage dosage = new Dosage(1m, DoseUnit.Milligram, timesPerDay);

        Assert.Equal(timesPerDay, dosage.TimesPerDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void Fewer_than_one_or_more_than_six_intakes_a_day_are_out_of_range(int timesPerDay) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(1m, DoseUnit.Milligram, timesPerDay));
    }

}
