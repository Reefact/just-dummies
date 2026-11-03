using Clinical.Domain;
using Clinical.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Expert;

public sealed class DosageTests {

    [Fact]
    public void A_dosage_keeps_what_it_was_given() {
        // Act
        Dosage dosage = new Dosage(2.5m, DoseUnit.Millilitre, 3);

        // Assert
        Assert.Equal(2.5m, dosage.Amount);
        Assert.Equal(DoseUnit.Millilitre, dosage.Unit);
        Assert.Equal(3, dosage.TimesPerDay);
    }

    public static TheoryData<decimal, int, decimal> DailyAmounts => new TheoryData<decimal, int, decimal> {
        { 2.5m, 3, 7.5m },
        { 0.001m, 6, 0.006m },
        { 10_000m, 6, 60_000m },
        { 1m, 1, 1m }
    };

    public static TheoryData<decimal> Extremes => new TheoryData<decimal> { 0.001m, 10_000m };

    public static TheoryData<decimal> OutOfRangeAmounts => new TheoryData<decimal> { 0m, 10_000.001m };

    public static TheoryData<decimal> OverPreciseAmounts => new TheoryData<decimal> { 0.0001m, 1.2345m, 9999.9999m };

    [Theory]
    [MemberData(nameof(DailyAmounts))]
    public void The_daily_amount_is_the_amount_times_the_intakes_per_day(decimal amount, int timesPerDay, decimal expectedDailyAmount) {
        // Arrange
        DoseUnit anyUnit = Any.Enum<DoseUnit>().Generate();

        // Act
        Dosage dosage = new Dosage(amount, anyUnit, timesPerDay);

        // Assert
        Assert.Equal(expectedDailyAmount, dosage.DailyAmount);
    }

    [Theory]
    [MemberData(nameof(Extremes))]
    public void The_smallest_and_the_largest_amounts_are_accepted(decimal amount) {
        // Arrange
        DoseUnit anyUnit        = Any.Enum<DoseUnit>().Generate();
        int      anyTimesPerDay = Any.Int32().Between(1, 6).Generate();

        // Act
        Dosage dosage = new Dosage(amount, anyUnit, anyTimesPerDay);

        // Assert
        Assert.Equal(amount, dosage.Amount);
    }

    [Theory]
    [MemberData(nameof(OutOfRangeAmounts))]
    public void An_amount_outside_zero_exclusive_to_ten_thousand_inclusive_is_refused(decimal amount) {
        // Arrange
        DoseUnit anyUnit        = Any.Enum<DoseUnit>().Generate();
        int      anyTimesPerDay = Any.Int32().Between(1, 6).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(amount, anyUnit, anyTimesPerDay));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_negative_amount_is_refused() {
        // Arrange
        decimal  anyNegativeAmount = Any.Decimal().Negative().GreaterThanOrEqualTo(-1_000_000m).WithScale(3).Generate();
        DoseUnit anyUnit           = Any.Enum<DoseUnit>().Generate();
        int      anyTimesPerDay    = Any.Int32().Between(1, 6).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(anyNegativeAmount, anyUnit, anyTimesPerDay));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_amount_above_ten_thousand_is_refused() {
        // Arrange
        decimal  anyHugeAmount  = Any.Decimal().GreaterThan(10_000m).LessThanOrEqualTo(1_000_000m).WithScale(3).Generate();
        DoseUnit anyUnit        = Any.Enum<DoseUnit>().Generate();
        int      anyTimesPerDay = Any.Int32().Between(1, 6).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(anyHugeAmount, anyUnit, anyTimesPerDay));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(OverPreciseAmounts))]
    public void An_amount_with_more_than_three_decimals_is_refused_as_an_invalid_argument_not_as_out_of_range(decimal amount) {
        // Arrange
        DoseUnit anyUnit        = Any.Enum<DoseUnit>().Generate();
        int      anyTimesPerDay = Any.Int32().Between(1, 6).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Dosage(amount, anyUnit, anyTimesPerDay));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_amount_written_with_trailing_zeros_beyond_three_decimals_is_accepted() {
        // Arrange
        DoseUnit anyUnit        = Any.Enum<DoseUnit>().Generate();
        int      anyTimesPerDay = Any.Int32().Between(1, 6).Generate();

        // Act
        Dosage dosage = new Dosage(1.500000m, anyUnit, anyTimesPerDay);

        // Assert
        Assert.Equal(1.5m, dosage.Amount);
    }

    [Fact]
    public void A_unit_that_is_not_a_declared_member_is_refused() {
        // Arrange
        decimal anyAmount      = Any.Decimal().Positive().LessThanOrEqualTo(10_000m).WithScale(3).Generate();
        int     anyTimesPerDay = Any.Int32().Between(1, 6).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(anyAmount, (DoseUnit)99, anyTimesPerDay));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void From_one_to_six_intakes_a_day_are_accepted(int timesPerDay) {
        // Arrange
        decimal  anyAmount = Any.Decimal().Positive().LessThanOrEqualTo(10_000m).WithScale(3).Generate();
        DoseUnit anyUnit   = Any.Enum<DoseUnit>().Generate();

        // Act
        Dosage dosage = new Dosage(anyAmount, anyUnit, timesPerDay);

        // Assert
        Assert.Equal(timesPerDay, dosage.TimesPerDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void Fewer_than_one_or_more_than_six_intakes_a_day_are_refused(int timesPerDay) {
        // Arrange
        decimal  anyAmount = Any.Decimal().Positive().LessThanOrEqualTo(10_000m).WithScale(3).Generate();
        DoseUnit anyUnit   = Any.Enum<DoseUnit>().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(anyAmount, anyUnit, timesPerDay));

        // Assert
        Assert.Equal("timesPerDay", refusal.ParamName);
    }

    [Fact]
    public void An_amount_that_is_zero_is_reported_before_an_undefined_unit() {
        // Arrange
        int anyTimesPerDay = Any.Int32().Between(1, 6).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Dosage(0m, (DoseUnit)99, anyTimesPerDay));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

}
