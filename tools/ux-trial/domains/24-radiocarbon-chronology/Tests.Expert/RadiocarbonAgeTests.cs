using JustDummies;

using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class RadiocarbonAgeTests {

    [Fact]
    public void Correcting_an_age_shifts_it_by_the_offset_and_keeps_its_uncertainty() {
        // Arrange
        RadiocarbonAge measured = new(2_400, 35);

        // Act
        RadiocarbonAge corrected = measured.CorrectedFor(400);

        // Assert
        Assert.Equal(new RadiocarbonAge(2_000, 35), corrected);
    }

    [Fact]
    public void A_correction_larger_than_the_age_is_refused() {
        // Arrange
        RadiocarbonAge anyRecentAge = Any.RadiocarbonAge().WithYearsBp(300).Generate();

        // Act
        Action correcting = () => anyRecentAge.CorrectedFor(400);

        // Assert
        Assert.Throws<InvalidOperationException>(correcting);
    }

}
