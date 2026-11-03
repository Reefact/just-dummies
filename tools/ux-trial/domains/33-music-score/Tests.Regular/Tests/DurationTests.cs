using System;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class DurationTests {

    [Theory]
    [InlineData(2, 4, 1, 2)]
    [InlineData(6, 8, 3, 4)]
    [InlineData(4, 4, 1, 1)]
    [InlineData(12, 48, 1, 4)]
    [InlineData(3, 8, 3, 8)]
    public void A_duration_is_normalised_by_the_greatest_common_divisor(long numerator, long denominator, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration duration = new Duration(numerator, denominator);

        // Assert
        Assert.Equal(expectedNumerator, duration.Numerator);
        Assert.Equal(expectedDenominator, duration.Denominator);
    }

    [Fact]
    public void A_zero_numerator_gives_the_zero_duration_whatever_the_denominator() {
        // Arrange
        long anyDenominator = Any.Int64().Between(1, Duration.MaxDenominator).Generate();

        // Act
        Duration duration = new Duration(0, anyDenominator);

        // Assert
        Assert.True(duration.IsZero);
        Assert.Equal("0/1", duration.ToString());
        Assert.Equal(Duration.Zero, duration);
    }

    [Fact]
    public void A_non_positive_denominator_is_refused() {
        // Arrange
        long anyNumerator           = Any.Int64().Between(0, 100).Generate();
        long anyNonPositiveDenominator = Any.Int64().LessThanOrEqualTo(0).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Duration(anyNumerator, anyNonPositiveDenominator));

        // Assert
        Assert.Equal("denominator", error.ParamName);
    }

    [Fact]
    public void A_negative_numerator_is_refused() {
        // Arrange
        long anyNegativeNumerator = Any.Int64().Negative().Generate();
        long anyDenominator       = Any.Int64().Between(1, Duration.MaxDenominator).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Duration(anyNegativeNumerator, anyDenominator));

        // Assert
        Assert.Equal("numerator", error.ParamName);
    }

    [Fact]
    public void A_duration_finer_than_one_1024th_is_refused() {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Duration(1, 1025));

        // Assert
        Assert.Equal("denominator", error.ParamName);
    }

    [Fact]
    public void One_1024th_is_the_finest_duration_accepted() {
        // Act
        Duration finest = new Duration(1, 1024);

        // Assert
        Assert.Equal(1024, finest.Denominator);
    }

    [Fact]
    public void The_limit_applies_after_normalisation_not_before() {
        // Act: 2/2048 is 1/1024 once reduced
        Duration reduced = new Duration(2, 2048);

        // Assert
        Assert.Equal(D(1, 1024), reduced);
    }

    [Fact]
    public void A_fraction_whose_reduced_denominator_exceeds_the_limit_is_refused_even_when_the_numerator_is_large() {
        // 3/2048 cannot be reduced
        Assert.Throws<ArgumentOutOfRangeException>(() => new Duration(3, 2048));
    }

    [Theory]
    [InlineData(1, 4, 1, 8, 3, 8)]
    [InlineData(1, 3, 1, 6, 1, 2)]
    [InlineData(3, 4, 1, 4, 1, 1)]
    [InlineData(0, 1, 5, 16, 5, 16)]
    [InlineData(1, 12, 1, 12, 1, 6)]
    public void Durations_add_exactly(long leftNumerator, long leftDenominator, long rightNumerator, long rightDenominator, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration sum = D(leftNumerator, leftDenominator) + D(rightNumerator, rightDenominator);

        // Assert
        Assert.Equal(D(expectedNumerator, expectedDenominator), sum);
    }

    [Fact]
    public void Three_triplet_eighths_make_a_quarter_exactly() {
        // Act
        Duration triplets = D(1, 12) + D(1, 12) + D(1, 12);

        // Assert
        Assert.Equal(D(1, 4), triplets);
    }

    [Fact]
    public void A_sum_whose_reduced_denominator_is_too_fine_is_refused() {
        // 1/1024 + 1/1000 has a denominator far beyond 1024
        Assert.Throws<ArgumentOutOfRangeException>(() => D(1, 1024) + D(1, 1000));
    }

    [Fact]
    public void Adding_a_null_duration_is_refused() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyDuration + null!);
        Assert.Throws<ArgumentNullException>(() => null! + anyDuration);
    }

    [Theory]
    [InlineData(3, 8, 4, 3, 2)]
    [InlineData(1, 4, 0, 0, 1)]
    [InlineData(1, 6, 3, 1, 2)]
    [InlineData(5, 16, 1, 5, 16)]
    public void A_duration_multiplies_by_a_whole_number(long numerator, long denominator, int factor, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration product = D(numerator, denominator) * factor;

        // Assert
        Assert.Equal(D(expectedNumerator, expectedDenominator), product);
    }

    [Fact]
    public void A_negative_factor_is_refused() {
        // Arrange
        Duration anyDuration     = Arbitrary.Duration().Generate();
        int      anyNegativeFactor = Any.Int32().Negative().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyDuration * anyNegativeFactor);
    }

    [Theory]
    [InlineData(1, 3, 1, 2, -1)]
    [InlineData(1, 2, 2, 4, 0)]
    [InlineData(3, 4, 5, 8, 1)]
    [InlineData(0, 1, 1, 1024, -1)]
    public void Durations_compare_by_value_not_by_representation(long leftNumerator, long leftDenominator, long rightNumerator, long rightDenominator, int expectedSign) {
        // Arrange
        Duration left  = D(leftNumerator, leftDenominator);
        Duration right = D(rightNumerator, rightDenominator);

        // Act
        int sign = Math.Sign(left.CompareTo(right));

        // Assert
        Assert.Equal(expectedSign, sign);
        Assert.Equal(expectedSign < 0, left < right);
        Assert.Equal(expectedSign <= 0, left <= right);
        Assert.Equal(expectedSign > 0, left > right);
        Assert.Equal(expectedSign >= 0, left >= right);
        Assert.Equal(expectedSign == 0, left == right);
        Assert.Equal(expectedSign != 0, left != right);
    }

    [Fact]
    public void Equal_durations_have_equal_hash_codes_even_when_built_from_different_fractions() {
        // Act
        Duration half      = D(1, 2);
        Duration fourOfEight = D(4, 8);

        // Assert
        Assert.Equal(half.GetHashCode(), fourOfEight.GetHashCode());
        Assert.True(half.Equals((object)fourOfEight));
    }

    [Fact]
    public void Any_duration_is_greater_than_nothing() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().Generate();

        // Assert
        Assert.Equal(1, anyDuration.CompareTo(null));
        Assert.False(anyDuration.Equals(null));
        Assert.False(anyDuration.Equals("1/2"));
    }

    [Theory]
    [InlineData(3, 8, 1, 8, 3.0)]
    [InlineData(1, 2, 1, 4, 2.0)]
    [InlineData(1, 4, 3, 8, 2.0 / 3.0)]
    [InlineData(0, 1, 1, 4, 0.0)]
    public void Dividing_by_a_unit_counts_how_many_units_fit(long numerator, long denominator, long unitNumerator, long unitDenominator, double expected) {
        // Act
        double count = D(numerator, denominator).DividedBy(D(unitNumerator, unitDenominator));

        // Assert
        Assert.Equal(expected, count, precision: 12);
    }

    [Fact]
    public void Dividing_by_the_zero_duration_is_refused() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => anyDuration.DividedBy(Duration.Zero));
    }

    [Fact]
    public void A_duration_reads_as_numerator_over_denominator() {
        // Assert
        Assert.Equal("3/8", D(6, 16).ToString());
        Assert.Equal(0.375, D(3, 8).InWholeNotes);
    }

}
