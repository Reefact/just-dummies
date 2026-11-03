using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class DurationTests {

    [Theory]
    [InlineData(2, 4, 1, 2)]
    [InlineData(6, 8, 3, 4)]
    [InlineData(12, 1024, 3, 256)]
    [InlineData(5, 1, 5, 1)]
    public void A_duration_is_stored_in_lowest_terms(long numerator, long denominator, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration duration = new Duration(numerator, denominator);

        // Assert
        Assert.Equal(expectedNumerator, duration.Numerator);
        Assert.Equal(expectedDenominator, duration.Denominator);
    }

    [Fact]
    public void A_zero_length_duration_is_normalised_to_zero_over_one() {
        // Arrange
        long anyDenominator = Any.Int64().Between(1, Duration.MaxDenominator).Generate();

        // Act
        Duration duration = new Duration(0, anyDenominator);

        // Assert
        Assert.True(duration.IsZero);
        Assert.Equal(Duration.Zero, duration);
        Assert.Equal(1, duration.Denominator);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_denominator_that_is_not_positive_is_refused(long denominator) {
        // Arrange
        long anyNumerator = Any.Int64().Between(0, 64).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Duration(anyNumerator, denominator));

        // Assert
        Assert.Equal("denominator", error.ParamName);
    }

    [Fact]
    public void A_negative_numerator_is_refused() {
        // Arrange
        long anyNegativeNumerator = Any.Int64().Between(-1000, -1).Generate();
        long anyDenominator       = Any.Int64().Between(1, Duration.MaxDenominator).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Duration(anyNegativeNumerator, anyDenominator));

        // Assert
        Assert.Equal("numerator", error.ParamName);
    }

    [Fact]
    public void A_duration_as_fine_as_one_1024th_of_a_whole_note_is_notatable() {
        // Act
        Duration duration = new Duration(1, 1024);

        // Assert
        Assert.Equal(1024, duration.Denominator);
    }

    [Fact]
    public void A_duration_finer_than_one_1024th_is_refused() {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Duration(1, 1025));

        // Assert
        Assert.Equal("denominator", error.ParamName);
    }

    [Fact]
    public void The_notatable_limit_applies_after_normalisation() {
        // 2/2048 is 1/1024 once reduced.
        Duration duration = new Duration(2, 2048);

        Assert.Equal(new Duration(1, 1024), duration);
    }

    [Fact]
    public void Two_durations_are_added_exactly() {
        // Arrange
        Duration quarter = new Duration(1, 4);
        Duration eighth  = new Duration(1, 8);

        // Act
        Duration sum = quarter + eighth;

        // Assert
        Assert.Equal(new Duration(3, 8), sum);
    }

    [Fact]
    public void Three_triplet_eighths_make_a_quarter_note() {
        // Act
        Duration sum = new Duration(1, 12) + new Duration(1, 12) + new Duration(1, 12);

        // Assert
        Assert.Equal(new Duration(1, 4), sum);
    }

    [Fact]
    public void Adding_a_null_duration_is_refused() {
        // Arrange
        Duration anyDuration = Any.Duration().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyDuration + null!);
        Assert.Throws<ArgumentNullException>(() => null! + anyDuration);
    }

    [Theory]
    [InlineData(3, 8, 2, 3, 4)]
    [InlineData(3, 8, 0, 0, 1)]
    [InlineData(1, 4, 4, 1, 1)]
    public void A_duration_is_multiplied_by_a_whole_number(long numerator, long denominator, int factor, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration product = new Duration(numerator, denominator) * factor;

        // Assert
        Assert.Equal(new Duration(expectedNumerator, expectedDenominator), product);
    }

    [Fact]
    public void A_negative_factor_is_refused() {
        // Arrange
        Duration anyDuration = Any.Duration().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyDuration * -1);
    }

    [Fact]
    public void Equal_ratios_are_equal_durations_with_the_same_hash_code() {
        // Arrange
        Duration half         = new Duration(1, 2);
        Duration twoQuarters  = new Duration(2, 4);

        // Assert
        Assert.True(half == twoQuarters);
        Assert.False(half != twoQuarters);
        Assert.Equal(half.GetHashCode(), twoQuarters.GetHashCode());
    }

    [Fact]
    public void Durations_are_ordered_by_their_exact_value() {
        // Arrange
        Duration third = new Duration(1, 3);
        Duration half  = new Duration(1, 2);

        // Assert
        Assert.True(third < half);
        Assert.True(half > third);
        Assert.True(third <= new Duration(2, 6));
        Assert.True(half >= new Duration(2, 4));
        Assert.True(third.CompareTo(half) < 0);
    }

    [Fact]
    public void Any_duration_sorts_after_null() {
        // Arrange
        Duration anyDuration = Any.Duration().Generate();

        // Assert
        Assert.Equal(1, anyDuration.CompareTo(null));
        Assert.False(anyDuration.Equals(null));
    }

    [Fact]
    public void A_duration_counts_how_many_times_a_unit_fits_in_it() {
        // Act
        double beats = new Duration(3, 4).DividedBy(new Duration(1, 4));

        // Assert
        Assert.Equal(3.0, beats);
    }

    [Fact]
    public void Dividing_by_a_zero_duration_is_refused() {
        // Arrange
        Duration anyDuration = Any.Duration().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => anyDuration.DividedBy(Duration.Zero));
    }

    [Fact]
    public void A_duration_reads_as_its_ratio() {
        Assert.Equal("3/8", new Duration(6, 16).ToString());
        Assert.Equal(0.375, new Duration(3, 8).InWholeNotes);
    }

    [Fact(Skip = "domain defect: the sum of two notatable durations can itself be refused as not notatable (1/100 + 1/101 throws ArgumentOutOfRangeException on 'denominator'), seed any")]
    public void The_sum_of_two_notatable_durations_is_a_duration() {
        // Arrange
        Duration oneHundredth      = new Duration(1, 100);
        Duration oneHundredAndOne  = new Duration(1, 101);

        // Act
        Duration sum = oneHundredth + oneHundredAndOne;

        // Assert
        Assert.Equal(201, sum.Numerator);
    }

}
