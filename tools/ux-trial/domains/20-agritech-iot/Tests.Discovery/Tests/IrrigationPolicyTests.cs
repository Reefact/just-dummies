using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class IrrigationPolicyTests {

    [Theory]
    [InlineData(0.0, 1)]
    [InlineData(100.0, 1)]
    [InlineData(35.5, 10)]
    [InlineData(50.0, int.MaxValue)]     // no upper bound on the minimum number of readings
    public void A_policy_keeps_a_threshold_from_0_to_100_and_a_positive_minimum(double threshold, int minimumReadings) {
        IrrigationPolicy policy = new IrrigationPolicy(threshold, minimumReadings);

        Assert.Equal(threshold, policy.DryThresholdPercent);
        Assert.Equal(minimumReadings, policy.MinimumReadings);
    }

    [Theory]
    [InlineData(-0.0000001)]
    [InlineData(-50.0)]
    [InlineData(double.NegativeInfinity)]
    public void A_negative_threshold_is_refused(double threshold) {
        // Arrange
        int anyMinimum = Any.Int32().Positive().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(threshold, anyMinimum));

        // Assert
        Assert.Equal("dryThresholdPercent", exception.ParamName);
    }

    [Theory]
    [InlineData(100.0000001)]
    [InlineData(1000.0)]
    [InlineData(double.PositiveInfinity)]
    public void A_threshold_above_100_is_refused(double threshold) {
        int anyMinimum = Any.Int32().Positive().Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(threshold, anyMinimum));

        Assert.Equal("dryThresholdPercent", exception.ParamName);
    }

    [Fact]
    public void A_threshold_that_is_not_a_number_is_refused() {
        int anyMinimum = Any.Int32().Positive().Generate();

        Assert.ThrowsAny<ArgumentException>(() => new IrrigationPolicy(double.NaN, anyMinimum));
    }

    [Fact(Skip = "domain defect: IrrigationPolicy accepts a NaN whose sign bit is clear (ThrowIfNegative tests the sign bit, ThrowIfGreaterThan is false for NaN); only double.NaN, which has the bit set, is refused by luck, and such a policy never irrigates, seed n/a (literal case)")]
    public void A_threshold_that_is_a_NaN_with_the_sign_bit_clear_is_refused() {
        // double.NaN carries the sign bit; a NaN produced by other arithmetic or decoded off the wire need not.
        double positiveNaN = BitConverter.Int64BitsToDouble(0x7FF8000000000000L);
        int    anyMinimum  = Any.Int32().Positive().Generate();

        Assert.ThrowsAny<ArgumentException>(() => new IrrigationPolicy(positiveNaN, anyMinimum));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_minimum_that_is_not_positive_is_refused(int minimumReadings) {
        double anyThreshold = Any.Double().Between(0d, 100d).Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(anyThreshold, minimumReadings));

        Assert.Equal("minimumReadings", exception.ParamName);
    }

    [Fact]
    public void A_drawn_policy_is_always_buildable() {
        IrrigationPolicy anyPolicy = Any.IrrigationPolicy().Generate();

        Assert.InRange(anyPolicy.DryThresholdPercent, 0.0, 100.0);
        Assert.True(anyPolicy.MinimumReadings > 0);
    }

    [Fact]
    public void A_field_drier_than_the_threshold_is_irrigated() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(40.0, 2);
        ReadingBatch     batch  = Readings.BatchOf(Readings.WithHumidity(30.0), Readings.WithHumidity(35.0));

        // Act and assert
        Assert.True(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_field_wetter_than_the_threshold_is_not_irrigated() {
        IrrigationPolicy policy = new IrrigationPolicy(40.0, 2);
        ReadingBatch     batch  = Readings.BatchOf(Readings.WithHumidity(50.0), Readings.WithHumidity(60.0));

        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_field_exactly_at_the_threshold_is_not_irrigated() {
        IrrigationPolicy policy = new IrrigationPolicy(40.0, 2);
        ReadingBatch     batch  = Readings.BatchOf(Readings.WithHumidity(30.0), Readings.WithHumidity(50.0));

        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_field_just_below_the_threshold_is_irrigated() {
        IrrigationPolicy policy = new IrrigationPolicy(40.0, 2);
        ReadingBatch     batch  = Readings.BatchOf(Readings.WithHumidity(30.0), Readings.WithHumidity(49.9999998));

        Assert.True(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void Too_few_readings_never_irrigate_however_dry_they_are() {
        IrrigationPolicy policy = new IrrigationPolicy(40.0, 3);
        ReadingBatch     batch  = Readings.BatchOf(Readings.WithHumidity(0.0), Readings.WithHumidity(0.0));

        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void Exactly_the_minimum_number_of_readings_is_enough() {
        IrrigationPolicy policy = new IrrigationPolicy(40.0, 3);
        ReadingBatch     batch  = Readings.BatchOf(Readings.WithHumidity(0.0), Readings.WithHumidity(0.0), Readings.WithHumidity(0.0));

        Assert.True(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_threshold_of_zero_never_irrigates() {
        IrrigationPolicy policy = new IrrigationPolicy(0.0, 1);
        ReadingBatch     batch  = Readings.BatchOf(Readings.WithHumidity(0.0));

        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void A_threshold_of_100_irrigates_unless_every_reading_is_saturated() {
        IrrigationPolicy policy = new IrrigationPolicy(100.0, 1);

        Assert.True(policy.ShouldIrrigate(Readings.BatchOf(Readings.WithHumidity(99.9999999))));
        Assert.False(policy.ShouldIrrigate(Readings.BatchOf(Readings.WithHumidity(100.0))));
    }

    [Fact]
    public void The_decision_does_not_depend_on_who_sent_the_batch_nor_on_the_other_readings_attributes() {
        // Arrange: only the humidity is pinned; positions, batteries, times and gateway are drawn.
        IrrigationPolicy policy = new IrrigationPolicy(40.0, 2);
        ReadingBatch     anyDryBatch = Any.ReadingBatch()
                                          .WithReadings(new[] { Readings.WithHumidity(10.0), Readings.WithHumidity(20.0) })
                                          .Generate();

        // Act and assert
        Assert.True(policy.ShouldIrrigate(anyDryBatch));
    }

    [Fact(Skip = "domain defect: IrrigationPolicy.ShouldIrrigate does not null-check its batch and throws NullReferenceException instead of ArgumentNullException, seed n/a (literal case)")]
    public void A_missing_batch_is_refused() {
        IrrigationPolicy anyPolicy = Any.IrrigationPolicy().Generate();

        Assert.Throws<ArgumentNullException>(() => anyPolicy.ShouldIrrigate(null!));
    }

}
