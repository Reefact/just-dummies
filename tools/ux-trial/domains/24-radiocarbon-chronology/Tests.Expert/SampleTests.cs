using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

using Xunit;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class SampleTests {

    [Fact]
    public void A_marine_shell_is_read_after_its_reservoir_offset() {
        // Arrange
        Sample anyShell = Any.Sample()
                             .WithMaterial(SampleMaterial.MarineShell)
                             .WithMeasurement(new RadiocarbonAge(2_400, 35))
                             .WithReservoirOffset(400)
                             .Generate();

        // Act
        RadiocarbonAge corrected = anyShell.CorrectedAge;

        // Assert
        Assert.Equal(new RadiocarbonAge(2_000, 35), corrected);
    }

    [Fact]
    public void Only_a_marine_shell_may_carry_a_reservoir_offset() {
        // Arrange
        string         anyLabCode          = AnySample.LabCodes().Generate();
        SampleMaterial anyTerrestrial      = Any.Enum<SampleMaterial>().DifferentFrom(SampleMaterial.MarineShell).Generate();
        RadiocarbonAge anyMeasurement      = Any.RadiocarbonAge().Generate();

        // Act
        Action recording = () => new Sample(anyLabCode, anyTerrestrial, anyMeasurement, reservoirOffset: 400);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(recording);
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Fact]
    public void A_reservoir_offset_cannot_exceed_the_measured_age() {
        // Arrange
        string         anyLabCode    = AnySample.LabCodes().Generate();
        RadiocarbonAge anyRecentAge  = Any.RadiocarbonAge().WithYearsBp(300).Generate();

        // Act
        Action recording = () => new Sample(anyLabCode, SampleMaterial.MarineShell, anyRecentAge, reservoirOffset: 400);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(recording);
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Theory]
    [InlineData("OxA12345")]   // no dash
    [InlineData("O-12345")]    // a single letter
    [InlineData("OxA-12")]     // too few digits
    [InlineData("oxa-12345")]  // starts in lower case
    public void A_lab_code_outside_the_laboratory_format_is_refused(string labCode) {
        // Arrange
        SampleMaterial anyMaterial    = Any.Enum<SampleMaterial>().Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Act
        Action recording = () => new Sample(labCode, anyMaterial, anyMeasurement, reservoirOffset: 0);

        // Assert
        Assert.Throws<FormatException>(recording);
    }

    [Fact]
    public void A_marine_shell_calibrates_at_the_calendar_year_of_its_corrected_age() {
        // Arrange
        CalibrationCurve straight = Curves.Straight();
        Sample anyShell = Any.Sample()
                             .WithMaterial(SampleMaterial.MarineShell)
                             .WithMeasurement(new RadiocarbonAge(2_400, 30))
                             .WithReservoirOffset(400)
                             .Generate();

        // Act
        CalibratedDate date = anyShell.CalibrateWith(straight);

        // Assert: 2000 BP once corrected, which the straight curve reads as astronomical year -50.
        Assert.InRange(date.Mean, -51.0, -49.0);
    }

}
