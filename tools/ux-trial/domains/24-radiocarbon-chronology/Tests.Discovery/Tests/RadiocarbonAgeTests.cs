namespace RadiocarbonChronology.Tests.Discovery;

public sealed class RadiocarbonAgeTests {

    [Theory]
    [InlineData(0, 1)]
    [InlineData(55_000, 5_000)]
    public void An_age_on_the_edge_of_the_ranges_is_accepted(int yearsBp, int uncertainty) {
        // Act
        RadiocarbonAge age = new RadiocarbonAge(yearsBp, uncertainty);

        // Assert
        Assert.Equal(yearsBp, age.YearsBp);
        Assert.Equal(uncertainty, age.Uncertainty);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(55_001)]
    public void An_age_outside_0_to_55000_BP_is_refused(int yearsBp) {
        // Arrange
        int anyUncertainty = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new RadiocarbonAge(yearsBp, anyUncertainty));

        // Assert
        Assert.Equal("yearsBp", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(5_001)]
    public void An_uncertainty_outside_1_to_5000_years_is_refused(int uncertainty) {
        // Arrange
        int anyYearsBp = Any.Int32().Between(0, RadiocarbonAge.MaximumYearsBp).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new RadiocarbonAge(anyYearsBp, uncertainty));

        // Assert
        Assert.Equal("uncertainty", refusal.ParamName);
    }

    [Fact]
    public void A_positive_offset_makes_the_age_younger_and_keeps_the_uncertainty() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(3_000, 40);

        // Act
        RadiocarbonAge corrected = measured.CorrectedFor(500);

        // Assert
        Assert.Equal(new RadiocarbonAge(2_500, 40), corrected);
    }

    [Fact]
    public void A_negative_offset_makes_the_age_older() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(3_000, 40);

        // Act
        RadiocarbonAge corrected = measured.CorrectedFor(-200);

        // Assert
        Assert.Equal(3_200, corrected.YearsBp);
    }

    [Fact]
    public void An_offset_equal_to_the_age_leaves_zero_years() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(450, 25);

        // Act
        RadiocarbonAge corrected = measured.CorrectedFor(450);

        // Assert
        Assert.Equal(0, corrected.YearsBp);
    }

    [Fact]
    public void An_offset_larger_than_the_age_is_refused() {
        // Arrange
        RadiocarbonAge measured = new RadiocarbonAge(450, 25);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => measured.CorrectedFor(451));

        // Assert
        Assert.Contains("451", refusal.Message);
    }

    [Fact]
    public void Two_ages_with_the_same_years_and_uncertainty_are_equal() {
        // Arrange
        RadiocarbonAge age      = new RadiocarbonAge(2_000, 30);
        RadiocarbonAge sameAge  = new RadiocarbonAge(2_000, 30);
        RadiocarbonAge otherAge = new RadiocarbonAge(2_000, 31);

        // Assert
        Assert.Equal(age, sameAge);
        Assert.Equal(age.GetHashCode(), sameAge.GetHashCode());
        Assert.NotEqual(age, otherAge);
        Assert.False(age.Equals(null));
    }

    [Fact]
    public void An_age_reads_as_years_BP_plus_or_minus_its_uncertainty() {
        // Arrange
        RadiocarbonAge age = new RadiocarbonAge(2_000, 30);

        // Assert
        Assert.Equal("2000 ± 30 BP", age.ToString());
    }

}
