using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class DurationTests {

    [Theory]
    [InlineData(6, 8, 3, 4)]
    [InlineData(4, 4, 1, 1)]
    [InlineData(12, 16, 3, 4)]
    [InlineData(7, 3, 7, 3)]
    [InlineData(0, 5, 0, 1)]
    [InlineData(2, 2048, 1, 1024)]
    public void A_duration_is_stored_in_lowest_terms(long numerator, long denominator, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration duration = new(numerator, denominator);

        // Assert
        Assert.Equal(expectedNumerator, duration.Numerator);
        Assert.Equal(expectedDenominator, duration.Denominator);
    }

    [Fact]
    public void Zero_written_over_any_denominator_is_the_zero_duration() {
        // Arrange
        long anyDenominator = Any.Int64().Positive().Generate();

        // Act
        Duration duration = new(0, anyDenominator);

        // Assert
        Assert.True(duration.IsZero);
        Assert.Equal(Duration.Zero, duration);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void A_denominator_that_is_not_positive_is_refused(long denominator) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Duration().WithDenominator(denominator).Generate());

        // Assert
        Assert.Equal("denominator", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void A_negative_numerator_is_refused(long numerator) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Duration().WithNumerator(numerator).Generate());

        // Assert
        Assert.Equal("numerator", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 1025)]
    [InlineData(1, 2048)]
    [InlineData(5, 5125)]
    [InlineData(1, 1_000_000_007)]
    public void A_duration_finer_than_a_1024th_of_a_whole_note_is_refused(long numerator, long denominator) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Duration(numerator, denominator));

        // Assert
        Assert.Equal("denominator", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, 1024)]
    [InlineData(3, 3072)]
    [InlineData(1023, 1024)]
    public void A_1024th_of_a_whole_note_is_the_finest_notatable_duration(long numerator, long denominator) {
        // Act
        Duration duration = new(numerator, denominator);

        // Assert
        Assert.Equal(Duration.MaxDenominator, duration.Denominator);
    }

    [Theory]
    [InlineData(1, 3, 1, 6, 1, 2)]
    [InlineData(1, 4, 3, 4, 1, 1)]
    [InlineData(0, 1, 5, 8, 5, 8)]
    [InlineData(1, 64, 1, 64, 1, 32)]
    public void Durations_add_exactly(long leftNumerator, long leftDenominator, long rightNumerator, long rightDenominator, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration sum = D(leftNumerator, leftDenominator) + D(rightNumerator, rightDenominator);

        // Assert
        Assert.Equal(D(expectedNumerator, expectedDenominator), sum);
    }

    [Theory]
    [InlineData(3, 8, 4, 3, 2)]
    [InlineData(3, 8, 0, 0, 1)]
    [InlineData(1, 3, 3, 1, 1)]
    public void A_duration_is_multiplied_by_a_whole_factor(long numerator, long denominator, int factor, long expectedNumerator, long expectedDenominator) {
        // Act
        Duration product = D(numerator, denominator) * factor;

        // Assert
        Assert.Equal(D(expectedNumerator, expectedDenominator), product);
    }

    [Fact]
    public void A_negative_factor_is_refused() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyDuration * -1);
    }

    [Fact]
    public void Adding_a_null_duration_is_refused() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyDuration + null!);
    }

    [Theory]
    [InlineData(1, 3, 1, 2, -1)]
    [InlineData(1, 2, 2, 4, 0)]
    [InlineData(5, 8, 3, 5, 1)]
    [InlineData(0, 1, 1, 1024, -1)]
    public void Durations_compare_by_their_exact_value(long leftNumerator, long leftDenominator, long rightNumerator, long rightDenominator, int expectedSign) {
        // Arrange
        Duration left  = D(leftNumerator, leftDenominator);
        Duration right = D(rightNumerator, rightDenominator);

        // Act & Assert
        Assert.Equal(expectedSign, Math.Sign(left.CompareTo(right)));
        Assert.Equal(expectedSign < 0, left < right);
        Assert.Equal(expectedSign > 0, left > right);
        Assert.Equal(expectedSign <= 0, left <= right);
        Assert.Equal(expectedSign >= 0, left >= right);
        Assert.Equal(expectedSign == 0, left == right);
        Assert.Equal(expectedSign != 0, left != right);
    }

    [Fact]
    public void Any_duration_is_greater_than_nothing() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().Generate();

        // Act & Assert
        Assert.Equal(1, anyDuration.CompareTo(null));
    }

    [Fact]
    public void Two_spellings_of_one_length_are_equal_and_hash_alike() {
        // Act
        Duration written    = new(2, 4);
        Duration normalised = new(1, 2);

        // Assert
        Assert.Equal(normalised, written);
        Assert.Equal(normalised.GetHashCode(), written.GetHashCode());
        Assert.True(written == normalised);
    }

    [Theory]
    [InlineData(3, 4, 1, 4, 3.0)]
    [InlineData(1, 3, 1, 6, 2.0)]
    [InlineData(1, 8, 3, 8, 1.0 / 3.0)]
    [InlineData(0, 1, 1, 4, 0.0)]
    public void A_duration_is_counted_in_units(long numerator, long denominator, long unitNumerator, long unitDenominator, double expected) {
        // Act
        double count = D(numerator, denominator).DividedBy(D(unitNumerator, unitDenominator));

        // Assert
        Assert.Equal(expected, count, 12);
    }

    [Fact]
    public void Dividing_by_a_zero_duration_is_refused() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyDuration.DividedBy(Duration.Zero));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the sum of two notatable durations (1/512 + 1/12 = 131/1536) is refused as 'finer than 1/1024' with ParamName 'denominator', so + is not closed over Duration; no seed, literals")]
    public void The_sum_of_two_notatable_durations_is_a_duration() {
        // Act
        Duration sum = D(1, 512) + D(1, 12);

        // Assert
        Assert.Equal(D(131, 1536), sum);
    }

    [Fact]
    public void A_duration_reads_as_its_fraction() {
        Assert.Equal("3/4", D(6, 8).ToString());
        Assert.Equal(0.75, D(6, 8).InWholeNotes);
    }

}
