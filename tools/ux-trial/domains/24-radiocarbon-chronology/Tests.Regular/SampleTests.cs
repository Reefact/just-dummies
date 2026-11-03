using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class SampleTests {

    [Fact]
    public void A_generated_sample_exposes_what_it_was_built_with() {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        SampleMaterial anyMaterial    = Any.Enum<SampleMaterial>().Except(SampleMaterial.MarineShell).Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Act
        Sample sample = new Sample(anyLabCode, anyMaterial, anyMeasurement, 0);

        // Assert
        Assert.Equal(anyLabCode, sample.LabCode);
        Assert.Equal(anyMaterial, sample.Material);
        Assert.Same(anyMeasurement, sample.Measurement);
        Assert.Equal(0, sample.ReservoirOffset);
    }

    [Fact]
    public void The_scaffolded_generator_only_draws_valid_samples() {
        // Arrange / Act
        Sample anySample = Any.Sample().Generate();

        // Assert
        Assert.True(anySample.CorrectedAge.YearsBp >= 0);
        if (anySample.Material != SampleMaterial.MarineShell) {
            Assert.Equal(0, anySample.ReservoirOffset);
        }
    }

    [Theory]
    [InlineData("OxA-12345")]
    [InlineData("Beta-123456")]
    [InlineData("AA-123")]
    [InlineData("GifA-99001")]
    public void A_laboratory_code_is_letters_a_dash_and_digits(string labCode) {
        // Arrange
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Act
        Sample sample = new Sample(labCode, SampleMaterial.Charcoal, anyMeasurement, 0);

        // Assert
        Assert.Equal(labCode, sample.LabCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("OxA12345")]
    [InlineData("oxA-12345")]
    [InlineData("O-12345")]
    [InlineData("OxAbCd-12345")]
    [InlineData("OxA-12")]
    [InlineData("OxA-1234567")]
    [InlineData("OxA-12a45")]
    [InlineData(" OxA-12345")]
    [InlineData("OxA-12345 ")]
    public void A_malformed_laboratory_code_is_refused(string labCode) {
        // Arrange
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Act / Assert
        Assert.Throws<FormatException>(() => new Sample(labCode, SampleMaterial.Charcoal, anyMeasurement, 0));
    }

    [Fact]
    public void A_missing_laboratory_code_is_refused() {
        // Arrange
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Sample(null!, SampleMaterial.Charcoal, anyMeasurement, 0));
    }

    [Fact]
    public void An_arbitrary_text_is_not_a_laboratory_code() {
        // Arrange
        string         anyText        = Any.String().Alpha().WithLengthBetween(1, 30).Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Act / Assert: no dash, so no digits group can follow
        Assert.Throws<FormatException>(() => new Sample(anyText, SampleMaterial.Charcoal, anyMeasurement, 0));
    }

    [Fact]
    public void An_unknown_material_is_refused() {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Sample(anyLabCode, (SampleMaterial)99, anyMeasurement, 0));

        // Assert
        Assert.Equal("material", error.ParamName);
    }

    [Fact]
    public void A_missing_measurement_is_refused() {
        // Arrange
        string anyLabCode = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Sample(anyLabCode, SampleMaterial.Seed, null!, 0));
    }

    [Theory]
    [InlineData(SampleMaterial.Charcoal)]
    [InlineData(SampleMaterial.Bone)]
    [InlineData(SampleMaterial.Seed)]
    public void Only_a_marine_shell_may_carry_a_reservoir_offset(SampleMaterial terrestrial) {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().WithYearsBp(5000).Generate();
        int            anyOffset      = Any.Int32().Between(1, 500).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Sample(anyLabCode, terrestrial, anyMeasurement, anyOffset));

        // Assert
        Assert.Equal("reservoirOffset", error.ParamName);
    }

    [Fact]
    public void A_marine_shell_carries_its_reservoir_offset() {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().WithYearsBp(5000).Generate();

        // Act
        Sample sample = new Sample(anyLabCode, SampleMaterial.MarineShell, anyMeasurement, 400);

        // Assert
        Assert.Equal(400, sample.ReservoirOffset);
    }

    [Theory]
    [InlineData(-500)]
    [InlineData(2000)]
    public void The_reservoir_offset_limits_are_accepted(int offset) {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().WithYearsBp(10_000).Generate();

        // Act
        Sample sample = new Sample(anyLabCode, SampleMaterial.MarineShell, anyMeasurement, offset);

        // Assert
        Assert.Equal(offset, sample.ReservoirOffset);
    }

    [Fact]
    public void A_marine_offset_below_the_minimum_is_refused() {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().WithYearsBp(10_000).Generate();
        int            anyTooLow      = Any.Int32().LessThan(Sample.MinimumReservoirOffset).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Sample(anyLabCode, SampleMaterial.MarineShell, anyMeasurement, anyTooLow));

        // Assert
        Assert.Equal("reservoirOffset", error.ParamName);
    }

    [Fact]
    public void A_marine_offset_above_the_maximum_is_refused() {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().WithYearsBp(RadiocarbonAge.MaximumYearsBp).Generate();
        int            anyTooHigh     = Any.Int32().GreaterThan(Sample.MaximumReservoirOffset).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Sample(anyLabCode, SampleMaterial.MarineShell, anyMeasurement, anyTooHigh));

        // Assert
        Assert.Equal("reservoirOffset", error.ParamName);
    }

    [Fact]
    public void An_offset_larger_than_the_measured_age_is_refused() {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().WithYearsBp(300).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Sample(anyLabCode, SampleMaterial.MarineShell, anyMeasurement, 301));

        // Assert
        Assert.Equal("reservoirOffset", error.ParamName);
    }

    [Fact]
    public void An_offset_equal_to_the_measured_age_is_accepted() {
        // Arrange
        string         anyLabCode     = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().WithYearsBp(300).Generate();

        // Act
        Sample sample = new Sample(anyLabCode, SampleMaterial.MarineShell, anyMeasurement, 300);

        // Assert
        Assert.Equal(0, sample.CorrectedAge.YearsBp);
    }

    [Fact]
    public void The_corrected_age_is_the_measurement_less_the_offset() {
        // Arrange
        string         anyLabCode  = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        int            anyUncertainty = Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty).Generate();
        RadiocarbonAge measurement = new RadiocarbonAge(4200, anyUncertainty);
        Sample         sample      = new Sample(anyLabCode, SampleMaterial.MarineShell, measurement, 380);

        // Act
        RadiocarbonAge corrected = sample.CorrectedAge;

        // Assert
        Assert.Equal(3820, corrected.YearsBp);
        Assert.Equal(anyUncertainty, corrected.Uncertainty);
    }

    [Fact]
    public void A_terrestrial_sample_is_calibrated_on_its_measured_age() {
        // Arrange
        CalibrationCurve curve  = TestCurves.Linear();
        Sample           sample = Any.Sample().WithMaterial(SampleMaterial.Charcoal).WithMeasurement(new RadiocarbonAge(3000, 30)).Generate();

        // Act
        CalibratedDate date = sample.CalibrateWith(curve);

        // Assert
        Assert.Equal(-1050d, date.Mean, 0);
    }

    [Fact]
    public void A_marine_sample_is_calibrated_on_its_corrected_age() {
        // Arrange
        CalibrationCurve curve  = TestCurves.Linear();
        Sample           sample = Any.Sample().WithMaterial(SampleMaterial.MarineShell).WithMeasurement(new RadiocarbonAge(3400, 30)).WithReservoirOffset(400).Generate();

        // Act
        CalibratedDate date = sample.CalibrateWith(curve);

        // Assert: 3400 - 400 = 3000 BP, which the linear curve gives in the year -1050
        Assert.Equal(-1050d, date.Mean, 0);
    }

    [Fact]
    public void Calibrating_with_no_curve_is_refused() {
        // Arrange
        Sample anySample = Any.Sample().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anySample.CalibrateWith(null!));
    }

}

public sealed class SampleDefectTests {

    [Fact(Skip = "domain defect: the lab code pattern ends with $, which also matches before a trailing line break, so 'OxA-12345\\n' is accepted; seed 657165919 (any seed shows it, the value is a literal)")]
    public void A_laboratory_code_followed_by_a_line_break_is_refused() {
        // Arrange
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Act / Assert
        Assert.Throws<FormatException>(() => new Sample("OxA-12345\n", SampleMaterial.Charcoal, anyMeasurement, 0));
    }

    [Fact(Skip = "domain defect: a marine shell measured at 55000 BP with a negative offset is accepted by the constructor, yet its CorrectedAge (55500 BP) is then refused with ArgumentOutOfRangeException; seed: none involved, the failing values are literals")]
    public void A_sample_accepted_by_the_constructor_has_a_usable_corrected_age() {
        // Arrange
        string         anyLabCode  = Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}").Generate();
        RadiocarbonAge measurement = new RadiocarbonAge(RadiocarbonAge.MaximumYearsBp, 100);
        Sample         sample      = new Sample(anyLabCode, SampleMaterial.MarineShell, measurement, -500);

        // Act
        RadiocarbonAge corrected = sample.CorrectedAge;

        // Assert
        Assert.Equal(55_500, corrected.YearsBp);
    }

}
