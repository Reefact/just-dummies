using JustDummies;
using JustDummies.Xunit;

using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Discovery;

[Reproducible]
public sealed class CalibrationCurveTests {

    [Fact]
    public void Calibration_centres_a_measurement_on_the_year_the_curve_reads_for_it() {
        // Arrange
        CalibrationCurve linearCurve = Any.CalibrationCurve().WithPoints(Curves.Linear()).Generate();
        RadiocarbonAge   measurement = Any.RadiocarbonAge().WithYearsBp(2000).WithUncertainty(30).Generate();

        // Act
        CalibratedDate date = linearCurve.Calibrate(measurement);

        // Assert
        Assert.Equal(-50, date.Mean, 1);
    }

    [Fact]
    public void An_age_the_curve_cannot_reach_is_refused() {
        // Arrange
        CalibrationCurve recentCurve = Any.CalibrationCurve()
                                          .WithPoints([new CalibrationPoint(1000, 950, 10), new CalibrationPoint(1950, 0, 10)])
                                          .Generate();
        RadiocarbonAge   anyGlacialAge = Any.RadiocarbonAge().WithYearsBp(40_000).WithUncertainty(u => u.Between(10, 500)).Generate();

        // Act
        Action calibrating = () => recentCurve.Calibrate(anyGlacialAge);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(calibrating);
    }

    [Fact]
    public void Curve_points_out_of_calendar_order_are_refused() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();
        CalibrationPoint later   = Any.CalibrationPoint().WithCalendarYear(500).Generate();
        CalibrationPoint earlier = Any.CalibrationPoint().WithCalendarYear(400).Generate();

        // Act
        Action creating = () => new CalibrationCurve(anyName, [later, earlier]);

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

}

[Reproducible]
public sealed class SampleTests {

    [Fact]
    public void A_lab_code_without_its_laboratory_prefix_is_refused() {
        // Arrange
        AnySample anySample = Any.Sample().WithLabCode("12345");

        // Act
        Action creating = () => anySample.Generate();

        // Assert
        Assert.Throws<FormatException>(creating);
    }

    [Fact]
    public void A_terrestrial_sample_cannot_carry_a_reservoir_offset() {
        // Arrange
        // AnySample.Generate() gives a non-marine sample no offset, so the constructor is called directly.
        string         anyLabCode     = Any.Sample().Generate().LabCode;
        SampleMaterial anyTerrestrial = Any.Enum<SampleMaterial>().DifferentFrom(SampleMaterial.MarineShell).Generate();
        RadiocarbonAge anyMeasurement = Any.RadiocarbonAge().WithYearsBp(y => y.GreaterThanOrEqualTo(Sample.MaximumReservoirOffset)).Generate();

        // Act
        Action creating = () => new Sample(anyLabCode, anyTerrestrial, anyMeasurement, 400);

        // Assert
        Assert.Throws<ArgumentException>(creating);
    }

    [Fact]
    public void A_marine_shell_is_read_at_its_measured_age_less_its_reservoir_offset() {
        // Arrange
        Sample shell = Any.Sample()
                          .WithMaterial(SampleMaterial.MarineShell)
                          .WithMeasurement(m => m.WithYearsBp(5000))
                          .WithReservoirOffset(400)
                          .Generate();

        // Act
        RadiocarbonAge corrected = shell.CorrectedAge;

        // Assert
        Assert.Equal(4600, corrected.YearsBp);
    }

    [Fact]
    public void A_marine_shell_calibrates_like_a_terrestrial_sample_of_its_corrected_age() {
        // Arrange
        CalibrationCurve linearCurve = Any.CalibrationCurve().WithPoints(Curves.Linear()).Generate();
        Sample shell = Any.Sample()
                          .WithMaterial(SampleMaterial.MarineShell)
                          .WithMeasurement(new RadiocarbonAge(2400, 40))
                          .WithReservoirOffset(400)
                          .Generate();
        Sample charcoal = Any.Sample()
                             .WithMaterial(SampleMaterial.Charcoal)
                             .WithMeasurement(new RadiocarbonAge(2000, 40))
                             .Generate();

        // Act
        CalibratedDate shellDate    = shell.CalibrateWith(linearCurve);
        CalibratedDate charcoalDate = charcoal.CalibrateWith(linearCurve);

        // Assert
        Assert.Equal(charcoalDate.Probabilities, shellDate.Probabilities);
    }

}
