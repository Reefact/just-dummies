using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class CollisionProbabilityTests {

    [Fact]
    public void A_probability_keeps_its_value() {
        // Arrange
        double anyValue = Any.Double().Between(0, 1).Generate();

        // Act
        CollisionProbability probability = new(anyValue);

        // Assert
        Assert.Equal(anyValue, probability.Value);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    public void The_two_ends_of_the_unit_interval_are_accepted(double end) {
        // Arrange / Act
        CollisionProbability probability = new(end);

        // Assert
        Assert.Equal(end, probability.Value);
    }

    [Fact]
    public void A_negative_value_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new CollisionProbability(anyNegative));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_value_above_one_is_refused() {
        // Arrange
        double anyExcess = Any.Double().GreaterThan(1).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new CollisionProbability(anyExcess));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void The_smallest_step_above_one_is_refused() {
        // Arrange
        double justAboveOne = Math.BitIncrement(1d);

        // Act
        Exception? refusal = Record.Exception(() => new CollisionProbability(justAboveOne));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void Not_a_number_is_refused() {
        // Arrange / Act
        Exception? refusal = Record.Exception(() => new CollisionProbability(double.NaN));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_probability_of_at_least_one_in_ten_thousand_is_high() {
        // Arrange
        double anyValue = Any.Double().Between(CollisionProbability.HighThreshold, 1).Generate();

        // Act
        RiskLevel level = new CollisionProbability(anyValue).Level;

        // Assert
        Assert.Equal(RiskLevel.High, level);
    }

    [Fact]
    public void A_probability_between_one_in_a_hundred_thousand_and_one_in_ten_thousand_is_elevated() {
        // Arrange
        double anyValue = Any.Double().Between(CollisionProbability.ElevatedThreshold, Math.BitDecrement(CollisionProbability.HighThreshold)).Generate();

        // Act
        RiskLevel level = new CollisionProbability(anyValue).Level;

        // Assert
        Assert.Equal(RiskLevel.Elevated, level);
    }

    [Fact]
    public void A_probability_under_one_in_a_hundred_thousand_is_low() {
        // Arrange
        double anyValue = Any.Double().Between(0, Math.BitDecrement(CollisionProbability.ElevatedThreshold)).Generate();

        // Act
        RiskLevel level = new CollisionProbability(anyValue).Level;

        // Assert
        Assert.Equal(RiskLevel.Low, level);
    }

    [Theory]
    [InlineData(1e-4, RiskLevel.High)]
    [InlineData(1e-5, RiskLevel.Elevated)]
    [InlineData(0d, RiskLevel.Low)]
    [InlineData(1d, RiskLevel.High)]
    public void The_thresholds_belong_to_the_higher_level(double value, RiskLevel expected) {
        // Arrange / Act
        RiskLevel level = new CollisionProbability(value).Level;

        // Assert
        Assert.Equal(expected, level);
    }

    [Fact]
    public void The_value_just_under_each_threshold_belongs_to_the_lower_level() {
        // Arrange
        CollisionProbability underHigh     = new(Math.BitDecrement(1e-4));
        CollisionProbability underElevated = new(Math.BitDecrement(1e-5));

        // Act / Assert
        Assert.Equal(RiskLevel.Elevated, underHigh.Level);
        Assert.Equal(RiskLevel.Low, underElevated.Level);
    }

    [Fact]
    public void Probabilities_compare_by_value() {
        // Arrange
        double               anySmall = Any.Double().Between(0, 0.5).Generate();
        CollisionProbability small    = new(anySmall);
        CollisionProbability large    = new(Any.Double().Between(0.5, 1).DifferentFrom(anySmall).Generate());

        // Act
        int ascending  = small.CompareTo(large);
        int descending = large.CompareTo(small);
        int same       = small.CompareTo(new CollisionProbability(small.Value));
        int againstNull = small.CompareTo(null);

        // Assert
        Assert.True(ascending < 0);
        Assert.True(descending > 0);
        Assert.Equal(0, same);
        Assert.True(againstNull > 0);
    }

    [Fact]
    public void A_probability_is_written_in_scientific_notation() {
        // Arrange
        CollisionProbability probability = new(0.000123);

        // Act
        string text = probability.ToString();

        // Assert
        Assert.Equal("1.23E-004", text);
    }

}
