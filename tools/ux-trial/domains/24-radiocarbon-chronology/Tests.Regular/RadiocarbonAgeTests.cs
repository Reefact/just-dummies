using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class RadiocarbonAgeTests {

    [Fact]
    public void A_generated_age_exposes_what_it_was_built_with() {
        // Arrange
        int anyYearsBp    = Any.Int32().Between(0, RadiocarbonAge.MaximumYearsBp).Generate();
        int anyUncertainty = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        RadiocarbonAge age = new RadiocarbonAge(anyYearsBp, anyUncertainty);

        // Assert
        Assert.Equal(anyYearsBp, age.YearsBp);
        Assert.Equal(anyUncertainty, age.Uncertainty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(55000)]
    public void The_extreme_ages_are_accepted(int yearsBp) {
        // Arrange
        int anyUncertainty = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        RadiocarbonAge age = new RadiocarbonAge(yearsBp, anyUncertainty);

        // Assert
        Assert.Equal(yearsBp, age.YearsBp);
    }

    [Fact]
    public void A_negative_age_is_refused() {
        // Arrange
        int anyNegativeYears = Any.Int32().Negative().Generate();
        int anyUncertainty   = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new RadiocarbonAge(anyNegativeYears, anyUncertainty));

        // Assert
        Assert.Equal("yearsBp", error.ParamName);
    }

    [Fact]
    public void An_age_beyond_the_radiocarbon_horizon_is_refused() {
        // Arrange
        int anyTooOldYears = Any.Int32().GreaterThan(RadiocarbonAge.MaximumYearsBp).Generate();
        int anyUncertainty = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new RadiocarbonAge(anyTooOldYears, anyUncertainty));

        // Assert
        Assert.Equal("yearsBp", error.ParamName);
    }

    [Fact]
    public void A_zero_or_negative_uncertainty_is_refused() {
        // Arrange
        int anyYearsBp         = Any.Int32().Between(0, RadiocarbonAge.MaximumYearsBp).Generate();
        int anyInvalidSigma    = Any.Int32().LessThan(1).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new RadiocarbonAge(anyYearsBp, anyInvalidSigma));

        // Assert
        Assert.Equal("uncertainty", error.ParamName);
    }

    [Fact]
    public void An_uncertainty_above_the_laboratory_ceiling_is_refused() {
        // Arrange
        int anyYearsBp      = Any.Int32().Between(0, RadiocarbonAge.MaximumYearsBp).Generate();
        int anyHugeSigma    = Any.Int32().GreaterThan(RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new RadiocarbonAge(anyYearsBp, anyHugeSigma));

        // Assert
        Assert.Equal("uncertainty", error.ParamName);
    }

    [Fact]
    public void Correcting_an_age_subtracts_the_reservoir_offset_and_keeps_the_uncertainty() {
        // Arrange
        int            anyUncertainty = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();
        RadiocarbonAge age            = new RadiocarbonAge(3000, anyUncertainty);

        // Act
        RadiocarbonAge corrected = age.CorrectedFor(400);

        // Assert
        Assert.Equal(2600, corrected.YearsBp);
        Assert.Equal(anyUncertainty, corrected.Uncertainty);
    }

    [Fact]
    public void A_negative_offset_makes_the_age_older() {
        // Arrange
        RadiocarbonAge anyAge = Any.RadiocarbonAge().WithYearsBp(1000).Generate();

        // Act
        RadiocarbonAge corrected = anyAge.CorrectedFor(-250);

        // Assert
        Assert.Equal(1250, corrected.YearsBp);
    }

    [Fact]
    public void A_zero_offset_leaves_the_age_equal() {
        // Arrange
        RadiocarbonAge anyAge = Any.RadiocarbonAge().Generate();

        // Act
        RadiocarbonAge corrected = anyAge.CorrectedFor(0);

        // Assert
        Assert.Equal(anyAge, corrected);
    }

    [Fact]
    public void An_offset_larger_than_the_age_is_refused() {
        // Arrange
        RadiocarbonAge anyAge = Any.RadiocarbonAge().WithYearsBp(300).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyAge.CorrectedFor(301));
    }

    [Fact]
    public void An_offset_equal_to_the_age_gives_zero() {
        // Arrange
        RadiocarbonAge anyAge = Any.RadiocarbonAge().WithYearsBp(300).Generate();

        // Act
        RadiocarbonAge corrected = anyAge.CorrectedFor(300);

        // Assert
        Assert.Equal(0, corrected.YearsBp);
    }

    [Fact]
    public void Two_ages_with_the_same_figures_are_equal() {
        // Arrange
        int anyYearsBp     = Any.Int32().Between(0, RadiocarbonAge.MaximumYearsBp).Generate();
        int anyUncertainty = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        RadiocarbonAge first  = new RadiocarbonAge(anyYearsBp, anyUncertainty);
        RadiocarbonAge second = new RadiocarbonAge(anyYearsBp, anyUncertainty);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ages_differing_by_a_year_are_not_equal() {
        // Arrange
        int anyUncertainty = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();

        // Act
        RadiocarbonAge first  = new RadiocarbonAge(2000, anyUncertainty);
        RadiocarbonAge second = new RadiocarbonAge(2001, anyUncertainty);

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void An_age_is_written_with_its_uncertainty() {
        // Arrange
        RadiocarbonAge age = new RadiocarbonAge(3450, 35);

        // Act
        string text = age.ToString();

        // Assert
        Assert.Equal("3450 ± 35 BP", text);
    }

}
