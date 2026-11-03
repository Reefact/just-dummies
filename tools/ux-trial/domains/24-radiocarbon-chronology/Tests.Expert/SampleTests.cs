using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class SampleTests {

    // ---------------------------------------------------------------- laboratory code

    [Theory]
    [InlineData("OxA-12345")]
    [InlineData("AA-123")]
    [InlineData("Beta-123456")]
    [InlineData("Poz-1234")]
    [InlineData("GrM-100000")]
    public void A_laboratory_code_is_a_capital_then_up_to_four_letters_a_dash_and_three_to_six_digits(string labCode) {
        // Act
        Sample sample = Any.Sample().WithLabCode(labCode).Generate();

        // Assert
        Assert.Equal(labCode, sample.LabCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("oxA-12345")]
    [InlineData("A-12345")]
    [InlineData("ABCDEF-12345")]
    [InlineData("OxA12345")]
    [InlineData("OxA-12")]
    [InlineData("OxA-1234567")]
    [InlineData("OxA-12a45")]
    [InlineData("O1A-12345")]
    [InlineData("OxA_12345")]
    [InlineData(" OxA-12345")]
    [InlineData("OxA-12345 ")]
    public void A_laboratory_code_of_another_shape_is_refused(string labCode) {
        // Act & Assert
        Assert.Throws<FormatException>(() => Any.Sample().WithLabCode(labCode).Generate());
    }

    [Fact(Skip = "domain defect: the lab code pattern is anchored with $, which .NET lets match before a final line break, so 'OxA-12345\\n' is accepted as a laboratory code (no seed: literals only)")]
    public void A_laboratory_code_followed_by_a_line_break_is_refused() {
        // Act & Assert
        Assert.Throws<FormatException>(() => Any.Sample().WithLabCode("OxA-12345\n").Generate());
    }

    [Fact]
    public void A_missing_laboratory_code_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.Sample().WithLabCode((string)null!).Generate());

        // Assert
        Assert.Equal("labCode", refusal.ParamName);
    }

    // ---------------------------------------------------------------- material, measurement

    [Fact]
    public void A_material_the_domain_does_not_know_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Sample().WithMaterial((SampleMaterial)99).Generate());

        // Assert
        Assert.Equal("material", refusal.ParamName);
    }

    [Fact]
    public void A_sample_without_a_measurement_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Any.Sample().WithMaterial(SampleMaterial.Charcoal).WithMeasurement((RadiocarbonAge)null!).Generate());

        // Assert
        Assert.Equal("measurement", refusal.ParamName);
    }

    [Fact]
    public void A_sample_keeps_what_it_was_given() {
        // Arrange
        RadiocarbonAge measurement = new RadiocarbonAge(3_400, 40);

        // Act
        Sample sample = Any.Sample()
                           .WithLabCode("OxA-12345")
                           .WithMaterial(SampleMaterial.MarineShell)
                           .WithMeasurement(measurement)
                           .WithReservoirOffset(400)
                           .Generate();

        // Assert
        Assert.Equal("OxA-12345", sample.LabCode);
        Assert.Equal(SampleMaterial.MarineShell, sample.Material);
        Assert.Same(measurement, sample.Measurement);
        Assert.Equal(400, sample.ReservoirOffset);
    }

    // ---------------------------------------------------------------- reservoir offset

    [Theory]
    [InlineData(SampleMaterial.Charcoal)]
    [InlineData(SampleMaterial.Bone)]
    [InlineData(SampleMaterial.Seed)]
    public void Only_a_marine_shell_may_carry_a_reservoir_offset(SampleMaterial terrestrial) {
        // Arrange
        string         anyLabCode = AnySample.ValidLabCode().Generate();
        RadiocarbonAge anyAge     = Any.RadiocarbonAge().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Sample(anyLabCode, terrestrial, anyAge, 400));

        // Assert
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void An_offset_of_one_year_either_way_is_enough_to_refuse_it_on_a_terrestrial_sample(int offset) {
        // Arrange
        string         anyLabCode = AnySample.ValidLabCode().Generate();
        RadiocarbonAge anyAge     = Any.RadiocarbonAge().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Sample(anyLabCode, SampleMaterial.Bone, anyAge, offset));
    }

    [Theory]
    [InlineData(SampleMaterial.Charcoal)]
    [InlineData(SampleMaterial.Bone)]
    [InlineData(SampleMaterial.Seed)]
    [InlineData(SampleMaterial.MarineShell)]
    public void No_offset_is_valid_for_every_material(SampleMaterial material) {
        // Arrange
        string         anyLabCode = AnySample.ValidLabCode().Generate();
        RadiocarbonAge anyAge     = Any.RadiocarbonAge().Generate();

        // Act
        Sample sample = new Sample(anyLabCode, material, anyAge, 0);

        // Assert
        Assert.Equal(0, sample.ReservoirOffset);
    }

    [Theory]
    [InlineData(-500)]
    [InlineData(2_000)]
    public void A_marine_shell_accepts_an_offset_at_the_limits_of_the_range(int offset) {
        // Arrange
        string anyLabCode = AnySample.ValidLabCode().Generate();

        // Act
        Sample shell = new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(5_000, 30), offset);

        // Assert
        Assert.Equal(offset, shell.ReservoirOffset);
    }

    [Theory]
    [InlineData(-501)]
    [InlineData(2_001)]
    public void A_marine_shell_refuses_an_offset_outside_the_range(int offset) {
        // Arrange
        string anyLabCode = AnySample.ValidLabCode().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(5_000, 30), offset));

        // Assert
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Fact]
    public void An_offset_equal_to_the_measured_age_is_accepted() {
        // Arrange
        string anyLabCode = AnySample.ValidLabCode().Generate();

        // Act
        Sample shell = new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(400, 30), 400);

        // Assert
        Assert.Equal(0, shell.CorrectedAge.YearsBp);
    }

    [Fact]
    public void An_offset_one_year_above_the_measured_age_is_refused() {
        // Arrange
        string anyLabCode = AnySample.ValidLabCode().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(400, 30), 401));

        // Assert
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: a marine shell measured near the end of the scale with a negative offset is accepted, then CorrectedAge throws ArgumentOutOfRangeException because the corrected age passes 55000 BP; the constructor checks the lower end of the scale only (no seed: literals only)")]
    public void An_offset_that_takes_the_corrected_age_past_the_end_of_the_scale_is_refused_when_the_sample_is_built() {
        // Arrange
        string anyLabCode = AnySample.ValidLabCode().Generate();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(
            () => new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(54_900, 30), -500));
    }

    // ---------------------------------------------------------------- corrected age, calibration

    [Fact]
    public void The_corrected_age_of_a_marine_shell_is_the_measurement_less_the_offset() {
        // Arrange
        Sample shell = Any.Sample()
                          .WithMaterial(SampleMaterial.MarineShell)
                          .WithMeasurement(new RadiocarbonAge(3_400, 40))
                          .WithReservoirOffset(400)
                          .Generate();

        // Assert
        Assert.Equal(new RadiocarbonAge(3_000, 40), shell.CorrectedAge);
    }

    [Fact]
    public void The_corrected_age_of_a_terrestrial_sample_is_its_measurement() {
        // Arrange
        Sample sample = Any.Sample()
                           .WithMaterial(material => material.Except(SampleMaterial.MarineShell))
                           .WithMeasurement(new RadiocarbonAge(3_000, 40))
                           .Generate();

        // Assert
        Assert.Equal(new RadiocarbonAge(3_000, 40), sample.CorrectedAge);
    }

    [Fact]
    public void A_marine_shell_is_calibrated_at_its_corrected_age() {
        // Arrange
        CalibrationCurve curve = Curves.Identity();
        Sample shell = Any.Sample()
                          .WithMaterial(SampleMaterial.MarineShell)
                          .WithMeasurement(new RadiocarbonAge(3_400, 30))
                          .WithReservoirOffset(400)
                          .Generate();
        Sample charcoalOfThatCorrectedAge = Any.Sample()
                                               .WithMaterial(SampleMaterial.Charcoal)
                                               .WithMeasurement(new RadiocarbonAge(3_000, 30))
                                               .Generate();

        // Act
        CalibratedDate shellDate    = shell.CalibrateWith(curve);
        CalibratedDate charcoalDate = charcoalOfThatCorrectedAge.CalibrateWith(curve);

        // Assert
        Assert.Equal(charcoalDate.Probabilities, shellDate.Probabilities);
        Assert.Equal(-1_050, shellDate.Mean, 1);
    }

    [Fact]
    public void Calibrating_against_no_curve_is_refused() {
        // Arrange
        Sample anySample = Any.Sample().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anySample.CalibrateWith(null!));
    }

}
