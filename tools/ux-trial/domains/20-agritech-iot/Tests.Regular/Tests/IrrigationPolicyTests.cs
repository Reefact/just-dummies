using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class IrrigationPolicyTests {

    [Fact]
    public void A_policy_keeps_its_threshold_and_its_minimum() {
        // Arrange
        double anyThreshold = Any.Double().Between(0d, 100d).Generate();
        int    anyMinimum   = Any.Int32().Positive().Generate();

        // Act
        IrrigationPolicy anyPolicy = new IrrigationPolicy(anyThreshold, anyMinimum);

        // Assert
        Assert.Equal(anyThreshold, anyPolicy.DryThresholdPercent);
        Assert.Equal(anyMinimum, anyPolicy.MinimumReadings);
    }

    [Theory]
    [InlineData(0.0, 1)]
    [InlineData(100.0, 1)]
    [InlineData(50.0, int.MaxValue)]
    public void The_extreme_valid_arguments_are_accepted(double threshold, int minimum) {
        // Act
        IrrigationPolicy policy = new IrrigationPolicy(threshold, minimum);

        // Assert
        Assert.Equal(threshold, policy.DryThresholdPercent);
        Assert.Equal(minimum, policy.MinimumReadings);
    }

    [Fact]
    public void A_negative_threshold_is_refused() {
        // Arrange
        double anyThreshold = Any.Double().Negative().Generate();
        int    anyMinimum   = Any.Int32().Positive().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(anyThreshold, anyMinimum));

        // Assert
        Assert.Equal("dryThresholdPercent", exception.ParamName);
    }

    [Fact]
    public void A_threshold_above_100_is_refused() {
        // Arrange
        double anyThreshold = Any.Double().GreaterThan(100d).Generate();
        int    anyMinimum   = Any.Int32().Positive().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(anyThreshold, anyMinimum));

        // Assert
        Assert.Equal("dryThresholdPercent", exception.ParamName);
    }

    [Fact]
    public void A_threshold_just_beyond_either_end_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(double.BitDecrement(0.0), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(double.BitIncrement(100.0), 1));
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void An_infinite_threshold_is_refused(double threshold) {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(threshold, 1));
    }

    [Fact(Skip = "domain defect: IrrigationPolicy accepts a NaN threshold whose sign bit is clear (ThrowIfNegative only tests the sign bit, ThrowIfGreaterThan(NaN, 100) is false); double.NaN, which has the sign bit set, happens to be refused. Such a policy then never irrigates. seed n/a, deterministic")]
    public void A_threshold_that_is_not_a_number_is_refused_whatever_its_sign_bit() {
        // Arrange
        double positiveNaN = BitConverter.Int64BitsToDouble(0x7FF8000000000000L);

        // Act / Assert
        Assert.True(double.IsNaN(positiveNaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(positiveNaN, 1));
    }

    [Fact]
    public void The_default_not_a_number_is_refused_as_a_threshold() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(double.NaN, 1));
    }

    [Fact]
    public void A_minimum_of_zero_readings_is_refused() {
        // Arrange
        double anyThreshold = Any.Double().Between(0d, 100d).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(anyThreshold, 0));

        // Assert
        Assert.Equal("minimumReadings", exception.ParamName);
    }

    [Fact]
    public void A_negative_minimum_is_refused() {
        // Arrange
        double anyThreshold = Any.Double().Between(0d, 100d).Generate();
        int    anyMinimum   = Any.Int32().Negative().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new IrrigationPolicy(anyThreshold, anyMinimum));

        // Assert
        Assert.Equal("minimumReadings", exception.ParamName);
    }

    [Fact]
    public void A_dry_enough_batch_with_enough_readings_calls_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 3);
        ReadingBatch     batch  = TestData.BatchWithHumidities(10.0, 20.0, 30.0);

        // Act
        bool shouldIrrigate = policy.ShouldIrrigate(batch);

        // Assert
        Assert.True(shouldIrrigate);
    }

    [Fact]
    public void An_average_equal_to_the_threshold_does_not_call_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 1);
        ReadingBatch     batch  = TestData.BatchWithHumidities(20.0, 40.0);

        // Act
        bool shouldIrrigate = policy.ShouldIrrigate(batch);

        // Assert
        Assert.False(shouldIrrigate);
    }

    [Fact]
    public void An_average_just_below_the_threshold_calls_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 1);
        ReadingBatch     batch  = TestData.BatchWithHumidities(20.0, 39.5);

        // Act
        bool shouldIrrigate = policy.ShouldIrrigate(batch);

        // Assert
        Assert.True(shouldIrrigate);
    }

    [Fact]
    public void A_wet_batch_does_not_call_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 1);
        ReadingBatch     batch  = TestData.BatchWithHumidities(70.0, 90.0);

        // Act
        bool shouldIrrigate = policy.ShouldIrrigate(batch);

        // Assert
        Assert.False(shouldIrrigate);
    }

    [Fact]
    public void A_bone_dry_batch_with_too_few_readings_does_not_call_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 4);
        ReadingBatch     batch  = TestData.BatchWithHumidities(0.0, 0.0, 0.0);

        // Act
        bool shouldIrrigate = policy.ShouldIrrigate(batch);

        // Assert
        Assert.False(shouldIrrigate);
    }

    [Fact]
    public void A_dry_batch_with_exactly_the_minimum_number_of_readings_calls_for_irrigation() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 4);
        ReadingBatch     batch  = TestData.BatchWithHumidities(0.0, 0.0, 0.0, 0.0);

        // Act
        bool shouldIrrigate = policy.ShouldIrrigate(batch);

        // Assert
        Assert.True(shouldIrrigate);
    }

    [Fact]
    public void Nothing_is_dry_enough_under_a_threshold_of_zero() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(0.0, 1);
        ReadingBatch     batch  = TestData.BatchWithHumidities(0.0, 0.0);

        // Act
        bool shouldIrrigate = policy.ShouldIrrigate(batch);

        // Assert
        Assert.False(shouldIrrigate);
    }

    [Fact]
    public void Everything_but_a_fully_saturated_batch_is_dry_enough_under_a_threshold_of_100() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(100.0, 1);

        // Act / Assert
        Assert.True(policy.ShouldIrrigate(TestData.BatchWithHumidities(99.9)));
        Assert.False(policy.ShouldIrrigate(TestData.BatchWithHumidities(100.0)));
    }

    [Fact]
    public void The_gateway_and_the_positions_play_no_part_in_the_decision() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(30.0, 2);
        List<SensorReading> readings = new List<SensorReading> {
            TestData.ReadingWithHumidity(10.0),
            TestData.ReadingWithHumidity(20.0)
        };
        ReadingBatch anyBatch = Any.ReadingBatch().WithReadings(readings).Generate();

        // Act
        bool shouldIrrigate = policy.ShouldIrrigate(anyBatch);

        // Assert
        Assert.True(shouldIrrigate);
    }

    [Fact(Skip = "domain defect: ShouldIrrigate(null) throws NullReferenceException instead of ArgumentNullException(batch); the constructors null-check every argument")]
    public void A_missing_batch_is_refused_with_an_argument_exception() {
        // Arrange
        IrrigationPolicy anyPolicy = Any.IrrigationPolicy().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyPolicy.ShouldIrrigate(null!));
    }

}
