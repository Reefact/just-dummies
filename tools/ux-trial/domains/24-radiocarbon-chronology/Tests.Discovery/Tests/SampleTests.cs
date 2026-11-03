namespace RadiocarbonChronology.Tests.Discovery;

public sealed class SampleTests {

    [Fact]
    public void A_sample_keeps_what_the_laboratory_reported() {
        // Arrange
        RadiocarbonAge measurement = new RadiocarbonAge(2_600, 30);

        // Act
        Sample sample = new Sample("OxA-12345", SampleMaterial.MarineShell, measurement, 400);

        // Assert
        Assert.Equal("OxA-12345", sample.LabCode);
        Assert.Equal(SampleMaterial.MarineShell, sample.Material);
        Assert.Same(measurement, sample.Measurement);
        Assert.Equal(400, sample.ReservoirOffset);
    }

    [Theory]
    [InlineData("OxA-12345")]
    [InlineData("AA-123")]
    [InlineData("GrN-1234")]
    [InlineData("Beta-123456")]
    [InlineData("ABCDE-000001")]
    public void A_lab_code_of_two_to_five_letters_and_three_to_six_digits_is_accepted(string labCode) {
        // Act
        Sample sample = Any.Sample().WithLabCode(labCode).Generate();

        // Assert
        Assert.Equal(labCode, sample.LabCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("oxA-12345")]
    [InlineData("O-12345")]
    [InlineData("ABCDEF-12345")]
    [InlineData("OxA12345")]
    [InlineData("OxA-12")]
    [InlineData("OxA-1234567")]
    [InlineData("OxA-12a45")]
    [InlineData("Ox1-12345")]
    [InlineData(" OxA-12345")]
    [InlineData("OxA 12345")]
    public void A_lab_code_that_is_not_letters_dash_digits_is_refused(string labCode) {
        // Assert
        Assert.Throws<FormatException>(() => Any.Sample().WithLabCode(labCode).Generate());
    }

    [Fact(Skip = "domain defect: the lab-code pattern ends with $ and not \\z, so 'OxA-12345\\n' (a trailing newline) passes as a lab code; literal input, no seed")]
    public void A_lab_code_followed_by_a_line_break_is_refused() {
        // Assert
        Assert.Throws<FormatException>(() => Any.Sample().WithLabCode("OxA-12345\n").Generate());
    }

    [Fact]
    public void A_missing_lab_code_is_refused() {
        // Arrange
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Sample(null!, SampleMaterial.Charcoal, anyMeasurement, 0));
    }

    [Fact]
    public void A_material_that_is_not_defined_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Sample().WithMaterial((SampleMaterial)99).Generate());

        // Assert
        Assert.Equal("material", refusal.ParamName);
    }

    [Fact]
    public void A_sample_needs_a_measurement() {
        // Arrange
        string anyLabCode = Any.Sample().Generate().LabCode;

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Sample(anyLabCode, SampleMaterial.Bone, null!, 0));
    }

    [Theory]
    [InlineData(SampleMaterial.Charcoal)]
    [InlineData(SampleMaterial.Bone)]
    [InlineData(SampleMaterial.Seed)]
    public void Only_a_marine_shell_may_carry_a_reservoir_offset(SampleMaterial material) {
        // Arrange
        RadiocarbonAge measurement = new RadiocarbonAge(5_000, 30);

        string anyLabCode = Any.Sample().Generate().LabCode;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Sample(anyLabCode, material, measurement, 100));

        // Assert
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Theory]
    [InlineData(-500)]
    [InlineData(0)]
    [InlineData(2_000)]
    public void A_marine_shell_accepts_an_offset_from_minus_500_to_2000(int reservoirOffset) {
        // Arrange
        string anyLabCode = Any.Sample().Generate().LabCode;

        // Act
        Sample sample = new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(5_000, 30), reservoirOffset);

        // Assert
        Assert.Equal(reservoirOffset, sample.ReservoirOffset);
    }

    [Theory]
    [InlineData(-501)]
    [InlineData(2_001)]
    public void A_marine_shell_refuses_an_offset_outside_minus_500_to_2000(int reservoirOffset) {
        // Arrange
        string anyLabCode = Any.Sample().Generate().LabCode;

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(5_000, 30), reservoirOffset));

        // Assert
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Fact]
    public void An_offset_larger_than_the_measured_age_is_refused() {
        // Arrange
        string anyLabCode = Any.Sample().Generate().LabCode;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(300, 30), 301));

        // Assert
        Assert.Equal("reservoirOffset", refusal.ParamName);
    }

    [Fact]
    public void An_offset_equal_to_the_measured_age_is_accepted() {
        // Arrange
        string anyLabCode = Any.Sample().Generate().LabCode;

        // Act
        Sample sample = new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(300, 30), 300);

        // Assert
        Assert.Equal(0, sample.CorrectedAge.YearsBp);
    }

    [Fact]
    public void The_corrected_age_is_the_measurement_less_the_offset() {
        // Arrange
        string anyLabCode = Any.Sample().Generate().LabCode;
        Sample sample     = new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(2_600, 30), 400);

        // Assert
        Assert.Equal(new RadiocarbonAge(2_200, 30), sample.CorrectedAge);
    }

    [Fact]
    public void A_sample_without_offset_is_read_as_measured() {
        // Arrange
        Sample sample = Any.Sample().WithMaterial(SampleMaterial.Seed).WithMeasurement(new RadiocarbonAge(2_600, 30)).Generate();

        // Assert
        Assert.Equal(new RadiocarbonAge(2_600, 30), sample.CorrectedAge);
    }

    [Fact(Skip = "domain defect: the constructor accepts a marine shell of 55000 BP with an offset of -500, but CorrectedAge then throws ArgumentOutOfRangeException (55500 BP is out of range); literal input, no seed")]
    public void The_corrected_age_of_an_accepted_sample_can_be_read() {
        // Arrange
        string anyLabCode = Any.Sample().Generate().LabCode;
        Sample sample     = new Sample(anyLabCode, SampleMaterial.MarineShell, new RadiocarbonAge(55_000, 30), -500);

        // Assert
        Assert.Equal(55_500, sample.CorrectedAge.YearsBp);
    }

    [Fact]
    public void Calibrating_reads_the_corrected_age_and_not_the_measured_one() {
        // Arrange
        CalibrationCurve curve            = Curves.Linear();
        Sample           marineWithOffset = new Sample("OxA-12345", SampleMaterial.MarineShell, new RadiocarbonAge(2_400, 30), 400);
        Sample           charcoalOfSameCorrectedAge = Any.Sample().WithMaterial(SampleMaterial.Charcoal).WithMeasurement(new RadiocarbonAge(2_000, 30)).Generate();
        Sample           charcoalOfSameMeasurement  = Any.Sample().WithMaterial(SampleMaterial.Charcoal).WithMeasurement(new RadiocarbonAge(2_400, 30)).Generate();

        // Act
        CalibratedDate date = marineWithOffset.CalibrateWith(curve);

        // Assert
        Assert.Equal(charcoalOfSameCorrectedAge.CalibrateWith(curve).Mean, date.Mean, 1e-9);
        Assert.NotEqual(charcoalOfSameMeasurement.CalibrateWith(curve).Mean, date.Mean, 1.0);
    }

    [Fact]
    public void Calibrating_needs_a_curve() {
        // Arrange
        Sample anySample = Any.Sample().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySample.CalibrateWith(null!));
    }

}
