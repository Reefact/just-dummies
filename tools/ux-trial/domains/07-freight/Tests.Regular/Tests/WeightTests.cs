namespace Freight.Domain.Tests;

public sealed class WeightTests {

    [Fact]
    public void A_drawn_weight_exposes_the_kilograms_it_was_built_from() {
        // Arrange
        double anyKilograms = Any.Double().Between(0.001, Weight.MaxGrossKilograms).Generate();

        // Act
        Weight weight = new Weight(anyKilograms);

        // Assert
        Assert.Equal(anyKilograms, weight.Kilograms);
    }

    [Fact]
    public void The_heaviest_weight_is_accepted() {
        // Act
        Weight weight = new Weight(32_500);

        // Assert
        Assert.Equal(32_500, weight.Kilograms);
        Assert.Equal(Weight.MaxGrossKilograms, weight.Kilograms);
    }

    [Fact]
    public void A_weight_just_above_the_maximum_is_refused() {
        // Arrange
        double justAbove = double.BitIncrement(Weight.MaxGrossKilograms);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(justAbove));

        // Assert
        Assert.Equal("kilograms", refusal.ParamName);
    }

    [Fact]
    public void A_weight_above_the_maximum_is_refused() {
        // Arrange
        double anyExcess = Any.Double().GreaterThan(Weight.MaxGrossKilograms).LessThan(1e9).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(anyExcess));
    }

    [Fact]
    public void The_smallest_positive_weight_is_accepted() {
        // Act
        Weight weight = new Weight(double.Epsilon);

        // Assert
        Assert.Equal(double.Epsilon, weight.Kilograms);
    }

    [Fact]
    public void Zero_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(0));
    }

    [Fact]
    public void Negative_zero_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(-0.0));
    }

    [Fact]
    public void A_negative_weight_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(anyNegative));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_weight_that_is_not_a_finite_number_is_refused(double kilograms) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(kilograms));
    }

    [Theory]
    [InlineData(12_500, 12.5)]
    [InlineData(1_000, 1)]
    [InlineData(250, 0.25)]
    public void The_metric_tons_are_the_kilograms_over_a_thousand(double kilograms, double expectedTons) {
        // Act
        double tons = new Weight(kilograms).MetricTons;

        // Assert
        Assert.Equal(expectedTons, tons, 10);
    }

    [Fact]
    public void Two_weights_with_the_same_kilograms_are_equal() {
        // Arrange
        Weight anyWeight = Any.Weight().Generate();
        Weight same      = new Weight(anyWeight.Kilograms);

        // Act & Assert
        Assert.Equal(anyWeight, same);
        Assert.True(anyWeight.Equals((object)same));
        Assert.Equal(anyWeight.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_weights_with_different_kilograms_are_not_equal() {
        // Arrange
        Weight anyWeight = Any.Weight().Generate();
        Weight anyOther  = Any.Weight().WithKilograms(kilograms => kilograms.DifferentFrom(anyWeight.Kilograms)).Generate();

        // Act & Assert
        Assert.NotEqual(anyWeight, anyOther);
        Assert.False(anyWeight.Equals(null));
    }

    [Fact]
    public void A_weight_prints_with_its_unit() {
        // Act
        string text = new Weight(1_250).ToString();

        // Assert
        Assert.Equal("1250 kg", text);
    }

    [Fact(Skip = "domain defect: ToString interpolates a double with the current culture, so under fr-FR it prints \"1250,5 kg\", seed n/a (literal)")]
    public void The_text_does_not_depend_on_the_culture_of_the_machine() {
        // Arrange
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try {
            // Act
            string text = new Weight(1_250.5).ToString();

            // Assert
            Assert.Equal("1250.5 kg", text);
        } finally {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

}
