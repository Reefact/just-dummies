using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class RadiocarbonAgeTests {

    [Fact]
    public void A_reservoir_offset_is_subtracted_from_the_measured_age() {
        // Arrange
        RadiocarbonAge anyAgeOf3400Bp = Arbitrary.RadiocarbonAge().WithYearsBp(3400).Generate();

        // Act
        RadiocarbonAge corrected = anyAgeOf3400Bp.CorrectedFor(400);

        // Assert
        Assert.Equal(3000, corrected.YearsBp);
    }

    [Fact]
    public void A_negative_offset_makes_the_age_older() {
        // Arrange
        RadiocarbonAge anyAgeOf3000Bp = Arbitrary.RadiocarbonAge().WithYearsBp(3000).Generate();

        // Act
        RadiocarbonAge corrected = anyAgeOf3000Bp.CorrectedFor(-150);

        // Assert
        Assert.Equal(3150, corrected.YearsBp);
    }

    [Fact]
    public void Correcting_keeps_the_laboratory_uncertainty() {
        // Arrange
        RadiocarbonAge age = new RadiocarbonAge(3400, 35);

        // Act
        RadiocarbonAge corrected = age.CorrectedFor(400);

        // Assert
        Assert.Equal(new RadiocarbonAge(3000, 35), corrected);
    }

    [Fact]
    public void An_offset_larger_than_the_age_is_refused() {
        // Arrange
        RadiocarbonAge anyAgeOf300Bp = Arbitrary.RadiocarbonAge().WithYearsBp(300).Generate();

        // Act
        Action correcting = () => anyAgeOf300Bp.CorrectedFor(400);

        // Assert
        Assert.Throws<InvalidOperationException>(correcting);
    }

}
