using AgriTech.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

public sealed class IrrigationPolicyTests {

    private static SensorReading ReadingWithHumidity(double percent) {
        return Any.SensorReading().WithHumidity(humidity => humidity.WithPercent(percent)).Generate();
    }

    private static ReadingBatch BatchWithHumidities(params double[] percents) {
        SensorReading[] readings = percents.Select(ReadingWithHumidity).ToArray();

        return Any.ReadingBatch().WithReadings(readings).Generate();
    }

    // ------------------------------------------------------------------ construction

    [Fact]
    public void A_policy_keeps_its_threshold_and_its_minimum_number_of_readings() {
        // Act
        IrrigationPolicy policy = new IrrigationPolicy(35.5, 4);

        // Assert
        Assert.Equal(35.5, policy.DryThresholdPercent);
        Assert.Equal(4, policy.MinimumReadings);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    public void Both_ends_of_the_zero_to_one_hundred_range_are_valid_thresholds(double threshold) {
        // Arrange
        int anyMinimum = Any.Int32().Positive().Generate();

        // Act
        IrrigationPolicy policy = new IrrigationPolicy(threshold, anyMinimum);

        // Assert
        Assert.Equal(threshold, policy.DryThresholdPercent);
    }

    [Theory]
    [InlineData(-0.000000000000001)]
    [InlineData(-1.0)]
    [InlineData(100.00000000000001)]
    [InlineData(101.0)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void A_threshold_outside_zero_to_one_hundred_is_refused(double threshold) {
        // Arrange
        int anyMinimum = Any.Int32().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(threshold, anyMinimum));

        // Assert
        Assert.NotNull(refusal.ParamName);
    }

    [Fact]
    public void A_threshold_that_is_not_a_number_is_refused() {
        // Arrange
        int anyMinimum = Any.Int32().Positive().Generate();

        // Assert
        Assert.ThrowsAny<ArgumentException>(() => new IrrigationPolicy(double.NaN, anyMinimum));
    }

    [Fact(Skip = "domain defect: a NaN with its sign bit clear (BitConverter.Int64BitsToDouble(0x7FF8000000000000)) is accepted as a threshold, while double.NaN, whose sign bit is set, is refused by ThrowIfNegative; the refusal of NaN depends on the sign bit; no drawn value involved")]
    public void A_threshold_that_is_a_positive_signed_NaN_is_refused_like_any_other_NaN() {
        // Arrange
        double positiveNaN = BitConverter.Int64BitsToDouble(0x7FF8000000000000);
        int    anyMinimum  = Any.Int32().Positive().Generate();

        // Assert
        Assert.ThrowsAny<ArgumentException>(() => new IrrigationPolicy(positiveNaN, anyMinimum));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void Any_positive_minimum_number_of_readings_is_valid(int minimum) {
        // Arrange
        double anyThreshold = Any.Double().Between(0.0, 100.0).Generate();

        // Act
        IrrigationPolicy policy = new IrrigationPolicy(anyThreshold, minimum);

        // Assert
        Assert.Equal(minimum, policy.MinimumReadings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_zero_or_negative_minimum_number_of_readings_is_refused(int minimum) {
        // Arrange
        double anyThreshold = Any.Double().Between(0.0, 100.0).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(anyThreshold, minimum));

        // Assert
        Assert.Equal("minimumReadings", refusal.ParamName);
    }

    // ------------------------------------------------------------------ ShouldIrrigate

    [Fact]
    public void A_dry_enough_batch_with_enough_readings_calls_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 3);
        ReadingBatch     batch  = BatchWithHumidities(10.0, 20.0, 30.0); // average 20

        // Assert
        Assert.True(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void An_average_exactly_at_the_threshold_does_not_call_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 2);
        ReadingBatch     batch  = BatchWithHumidities(20.0, 40.0); // average 30

        // Assert
        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void An_average_just_below_the_threshold_calls_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 2);
        ReadingBatch     batch  = BatchWithHumidities(20.0, 39.0); // average 29.5

        // Assert
        Assert.True(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_wet_batch_does_not_call_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 1);
        ReadingBatch     batch  = BatchWithHumidities(80.0, 90.0);

        // Assert
        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_batch_with_fewer_readings_than_the_minimum_does_not_call_for_irrigation_however_dry_it_is() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 3);
        ReadingBatch     batch  = BatchWithHumidities(0.0, 0.0); // bone dry, but only two readings

        // Assert
        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_batch_with_exactly_the_minimum_number_of_readings_is_enough() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 3);
        ReadingBatch     batch  = BatchWithHumidities(0.0, 0.0, 0.0);

        // Assert
        Assert.True(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_threshold_of_zero_never_calls_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(0.0, 1);
        ReadingBatch     batch  = BatchWithHumidities(0.0, 0.0);

        // Assert: nothing is strictly below zero
        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_threshold_of_one_hundred_calls_for_irrigation_unless_the_air_is_saturated_everywhere() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(100.0, 1);

        // Assert
        Assert.True(policy.ShouldIrrigate(BatchWithHumidities(100.0, 99.0)));
        Assert.False(policy.ShouldIrrigate(BatchWithHumidities(100.0, 100.0)));
    }

    [Fact]
    public void A_policy_requiring_more_readings_than_a_batch_can_hold_never_calls_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(100.0, int.MaxValue);
        ReadingBatch     batch  = BatchWithHumidities(0.0, 0.0);

        // Assert
        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact(Skip = "domain defect: ShouldIrrigate does not null-check its batch and fails with a NullReferenceException instead of an ArgumentNullException naming 'batch'; no drawn value involved")]
    public void A_missing_batch_is_refused_as_a_missing_argument() {
        // Arrange
        IrrigationPolicy anyPolicy = Any.IrrigationPolicy().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPolicy.ShouldIrrigate(null!));

        // Assert
        Assert.Equal("batch", refusal.ParamName);
    }

}
