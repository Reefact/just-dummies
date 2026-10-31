using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class SampleTests {

    [Theory]
    [InlineData("12345")]
    [InlineData("OxA12345")]
    [InlineData("oxa-12345")]
    [InlineData("OxA-12")]
    public void A_code_that_is_not_a_laboratory_code_is_refused(string labCode) {
        // Arrange
        AnySample anySampleCoded = Arbitrary.Sample().WithLabCode(labCode);

        // Act
        Action creating = () => anySampleCoded.Generate();

        // Assert
        Assert.Throws<FormatException>(creating);
    }

    [Fact]
    public void Only_a_marine_shell_may_carry_a_reservoir_offset() {
        // Arrange
        AnySample anyTerrestrialSampleWithOffset = Arbitrary.Sample()
                                                            .WithMaterial(m => m.DifferentFrom(SampleMaterial.MarineShell))
                                                            .WithReservoirOffset(250);

        // Act
        Action creating = () => anyTerrestrialSampleWithOffset.Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(creating);
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Fact]
    public void A_shell_offset_exceeding_the_measurement_is_refused() {
        // Arrange
        AnySample anyShellOverCorrected = Arbitrary.Sample()
                                                   .WithMaterial(SampleMaterial.MarineShell)
                                                   .WithMeasurement(m => m.WithYearsBp(300))
                                                   .WithReservoirOffset(400);

        // Act
        Action creating = () => anyShellOverCorrected.Generate();

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

    [Fact]
    public void A_marine_shell_is_calibrated_on_its_corrected_age() {
        // Arrange
        Sample anyShell = Arbitrary.Sample()
                                   .WithMaterial(SampleMaterial.MarineShell)
                                   .WithMeasurement(new RadiocarbonAge(3400, 20))
                                   .WithReservoirOffset(400)
                                   .Generate();

        // Act
        CalibratedDate date = anyShell.CalibrateWith(Curves.Linear());

        // Assert
        Assert.Equal(-1050, date.Mean, 1.0);
    }

    [Fact]
    public void A_terrestrial_sample_is_calibrated_on_its_measurement() {
        // Arrange
        Sample anyCharcoal = Arbitrary.Sample()
                                      .WithMaterial(SampleMaterial.Charcoal)
                                      .WithMeasurement(new RadiocarbonAge(3400, 20))
                                      .Generate();

        // Act
        CalibratedDate date = anyCharcoal.CalibrateWith(Curves.Linear());

        // Assert
        Assert.Equal(-1450, date.Mean, 1.0);
    }

}
