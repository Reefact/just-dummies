using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class LabResultTests {

    private static ReferenceRange RangeWithCriticalLimits(MeasurementUnit unit) {
        return new ReferenceRange(10d, 20d, unit, criticalLow: 5d, criticalHigh: 30d);
    }

    private static LabResult AnyResultMeasuring(double value) {
        ReferenceRange range = RangeWithCriticalLimits(Any.MeasurementUnit().Generate());

        return Any.LabResult().WithRange(range).WithMeasurement(m => m.WithValue(value)).Generate();
    }

    private static LabResult AnyFinalResult() {
        LabResult result = Any.LabResult().Generate();
        result.Release();

        return result;
    }

    // ---- construction -------------------------------------------------------------------------------------------

    [Fact]
    public void A_result_keeps_what_it_was_given_and_starts_preliminary() {
        // Arrange
        AnalyteCode     anyAnalyte = Any.AnalyteCode().Generate();
        Specimen        anySpecimen = Any.Specimen().Generate();
        MeasurementUnit anyUnit     = Any.MeasurementUnit().Generate();
        Measurement     anyMeasurement = Any.Measurement().WithUnit(anyUnit).Generate();
        ReferenceRange  anyRange       = Any.ReferenceRange().WithUnit(anyUnit).Generate();
        DateTimeOffset  anyReportedAt  = anySpecimen.ReceivedAt + Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(30)).Generate();

        // Act
        LabResult result = new LabResult(anyAnalyte, anySpecimen, anyMeasurement, anyRange, anyReportedAt);

        // Assert
        Assert.Same(anyAnalyte, result.Analyte);
        Assert.Same(anySpecimen, result.Specimen);
        Assert.Same(anyMeasurement, result.Measurement);
        Assert.Same(anyRange, result.Range);
        Assert.Equal(anyReportedAt, result.ReportedAt);
        Assert.Equal(ResultStatus.Preliminary, result.Status);
    }

    [Fact]
    public void The_generator_only_draws_results_the_constructor_accepts() {
        // Act
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Equal(anyResult.Range.Unit, anyResult.Measurement.Unit);
        Assert.True(anyResult.ReportedAt >= anyResult.Specimen.ReceivedAt);
    }

    [Fact]
    public void A_result_reported_the_instant_the_specimen_was_received_is_accepted() {
        // Arrange
        Specimen anySpecimen = Any.Specimen().Generate();

        // Act
        LabResult result = Any.LabResult().WithSpecimen(anySpecimen).WithReportedAt(anySpecimen.ReceivedAt).Generate();

        // Assert
        Assert.Equal(anySpecimen.ReceivedAt, result.ReportedAt);
    }

    [Fact]
    public void A_result_reported_one_tick_before_the_specimen_was_received_is_refused() {
        // Arrange
        Specimen        anySpecimen = Any.Specimen().Generate();
        MeasurementUnit anyUnit     = Any.MeasurementUnit().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(Any.AnalyteCode().Generate(),
                                                                                         anySpecimen,
                                                                                         Any.Measurement().WithUnit(anyUnit).Generate(),
                                                                                         Any.ReferenceRange().WithUnit(anyUnit).Generate(),
                                                                                         anySpecimen.ReceivedAt - TimeSpan.FromTicks(1)));

        // Assert
        Assert.Equal("reportedAt", refusal.ParamName);
    }

    [Fact]
    public void A_result_reported_long_before_the_specimen_was_received_is_refused() {
        // Arrange
        Specimen        anySpecimen = Any.Specimen().Generate();
        MeasurementUnit anyUnit     = Any.MeasurementUnit().Generate();
        TimeSpan        anyAdvance  = Any.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromDays(3_650)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LabResult(Any.AnalyteCode().Generate(),
                                                             anySpecimen,
                                                             Any.Measurement().WithUnit(anyUnit).Generate(),
                                                             Any.ReferenceRange().WithUnit(anyUnit).Generate(),
                                                             anySpecimen.ReceivedAt - anyAdvance));
    }

    [Fact]
    public void A_measurement_and_a_range_in_different_units_are_refused() {
        // Arrange
        MeasurementUnit anyUnit      = Any.MeasurementUnit().Generate();
        MeasurementUnit anyOtherUnit = Any.MeasurementUnit().WithSymbol(s => s.DifferentFrom(anyUnit.Symbol)).Generate();
        Specimen        anySpecimen  = Any.Specimen().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(Any.AnalyteCode().Generate(),
                                                                                         anySpecimen,
                                                                                         Any.Measurement().WithUnit(anyUnit).Generate(),
                                                                                         Any.ReferenceRange().WithUnit(anyOtherUnit).Generate(),
                                                                                         anySpecimen.ReceivedAt));

        // Assert
        Assert.Equal("range", refusal.ParamName);
    }

    [Fact]
    public void Units_that_differ_only_by_case_are_different_units() {
        // Arrange
        Specimen anySpecimen = Any.Specimen().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LabResult(Any.AnalyteCode().Generate(),
                                                             anySpecimen,
                                                             Any.Measurement().WithUnit(new MeasurementUnit("mmol/l")).Generate(),
                                                             Any.ReferenceRange().WithUnit(new MeasurementUnit("mmol/L")).Generate(),
                                                             anySpecimen.ReceivedAt));
    }

    [Fact]
    public void Two_units_with_the_same_symbol_are_the_same_unit_even_as_different_instances() {
        // Arrange
        string          anySymbol   = Any.String().NotBlank().WithMaxLength(MeasurementUnit.MaxLength).Generate();
        Specimen        anySpecimen = Any.Specimen().Generate();

        // Act
        LabResult result = new LabResult(Any.AnalyteCode().Generate(),
                                         anySpecimen,
                                         Any.Measurement().WithUnit(new MeasurementUnit(anySymbol)).Generate(),
                                         Any.ReferenceRange().WithUnit(new MeasurementUnit(anySymbol)).Generate(),
                                         anySpecimen.ReceivedAt);

        // Assert
        Assert.Equal(result.Measurement.Unit, result.Range.Unit);
    }

    [Fact]
    public void The_units_are_judged_before_the_report_time() {
        // Arrange: wrong unit AND reported before reception.
        Specimen anySpecimen = Any.Specimen().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(Any.AnalyteCode().Generate(),
                                                                                         anySpecimen,
                                                                                         Any.Measurement().WithUnit(new MeasurementUnit("g/dL")).Generate(),
                                                                                         Any.ReferenceRange().WithUnit(new MeasurementUnit("mmol/L")).Generate(),
                                                                                         anySpecimen.ReceivedAt - TimeSpan.FromDays(1)));

        // Assert
        Assert.Equal("range", refusal.ParamName);
    }

    [Fact]
    public void Every_collaborator_is_required() {
        // Arrange
        AnalyteCode    analyte     = Any.AnalyteCode().Generate();
        Specimen       specimen    = Any.Specimen().Generate();
        MeasurementUnit unit       = Any.MeasurementUnit().Generate();
        Measurement    measurement = Any.Measurement().WithUnit(unit).Generate();
        ReferenceRange range       = Any.ReferenceRange().WithUnit(unit).Generate();
        DateTimeOffset reportedAt  = specimen.ReceivedAt;

        // Act & Assert
        Assert.Equal("analyte", Assert.Throws<ArgumentNullException>(() => new LabResult(null!, specimen, measurement, range, reportedAt)).ParamName);
        Assert.Equal("specimen", Assert.Throws<ArgumentNullException>(() => new LabResult(analyte, null!, measurement, range, reportedAt)).ParamName);
        Assert.Equal("measurement", Assert.Throws<ArgumentNullException>(() => new LabResult(analyte, specimen, null!, range, reportedAt)).ParamName);
        Assert.Equal("range", Assert.Throws<ArgumentNullException>(() => new LabResult(analyte, specimen, measurement, null!, reportedAt)).ParamName);
    }

    // ---- flags --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(15d, ResultFlag.Normal, false, false)]
    [InlineData(10d, ResultFlag.Normal, false, false)]
    [InlineData(20d, ResultFlag.Normal, false, false)]
    [InlineData(7d, ResultFlag.Low, true, false)]
    [InlineData(25d, ResultFlag.High, true, false)]
    [InlineData(5d, ResultFlag.CriticalLow, true, true)]
    [InlineData(0d, ResultFlag.CriticalLow, true, true)]
    [InlineData(30d, ResultFlag.CriticalHigh, true, true)]
    [InlineData(1e6d, ResultFlag.CriticalHigh, true, true)]
    public void The_flag_follows_the_measurement_against_the_range(double value, ResultFlag flag, bool outOfRange, bool phoneCall) {
        // Arrange
        LabResult result = AnyResultMeasuring(value);

        // Act & Assert
        Assert.Equal(flag, result.Flag);
        Assert.Equal(outOfRange, result.IsOutOfRange);
        Assert.Equal(phoneCall, result.RequiresPhoneCall);
    }

    // ---- release ------------------------------------------------------------------------------------------------

    [Fact]
    public void Releasing_a_preliminary_result_makes_it_final() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        anyResult.Release();

        // Assert
        Assert.Equal(ResultStatus.Final, anyResult.Status);
    }

    [Fact]
    public void Releasing_leaves_the_measurement_and_the_report_time_alone() {
        // Arrange
        LabResult      anyResult   = Any.LabResult().Generate();
        Measurement    measurement = anyResult.Measurement;
        DateTimeOffset reportedAt  = anyResult.ReportedAt;

        // Act
        anyResult.Release();

        // Assert
        Assert.Same(measurement, anyResult.Measurement);
        Assert.Equal(reportedAt, anyResult.ReportedAt);
    }

    [Fact]
    public void A_final_result_cannot_be_released_again() {
        // Arrange
        LabResult anyResult = AnyFinalResult();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyResult.Release());
        Assert.Equal(ResultStatus.Final, anyResult.Status);
    }

    [Fact]
    public void An_amended_result_cannot_be_released() {
        // Arrange
        LabResult anyResult = AnyFinalResult();
        anyResult.Amend(Any.Measurement().WithUnit(anyResult.Range.Unit).Generate(), anyResult.ReportedAt + TimeSpan.FromHours(1));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyResult.Release());
        Assert.Equal(ResultStatus.Amended, anyResult.Status);
    }

    // ---- amendment ----------------------------------------------------------------------------------------------

    [Fact]
    public void Amending_a_final_result_replaces_the_measurement_and_the_report_time_and_marks_it_amended() {
        // Arrange
        LabResult      anyResult      = AnyFinalResult();
        Measurement    anyCorrected   = Any.Measurement().WithUnit(anyResult.Range.Unit).Generate();
        DateTimeOffset anyAmendedAt   = Any.DateTimeOffset().After(anyResult.ReportedAt).BeforeOrEqualTo(anyResult.ReportedAt + TimeSpan.FromDays(30)).Generate();

        // Act
        anyResult.Amend(anyCorrected, anyAmendedAt);

        // Assert
        Assert.Same(anyCorrected, anyResult.Measurement);
        Assert.Equal(anyAmendedAt, anyResult.ReportedAt);
        Assert.Equal(ResultStatus.Amended, anyResult.Status);
    }

    [Fact]
    public void An_amended_result_can_be_amended_again() {
        // Arrange
        LabResult      anyResult     = AnyFinalResult();
        DateTimeOffset firstAmendedAt = anyResult.ReportedAt + TimeSpan.FromHours(1);
        anyResult.Amend(Any.Measurement().WithUnit(anyResult.Range.Unit).Generate(), firstAmendedAt);

        Measurement    anySecondCorrection = Any.Measurement().WithUnit(anyResult.Range.Unit).Generate();
        DateTimeOffset secondAmendedAt     = firstAmendedAt + TimeSpan.FromHours(1);

        // Act
        anyResult.Amend(anySecondCorrection, secondAmendedAt);

        // Assert
        Assert.Same(anySecondCorrection, anyResult.Measurement);
        Assert.Equal(secondAmendedAt, anyResult.ReportedAt);
        Assert.Equal(ResultStatus.Amended, anyResult.Status);
    }

    [Fact]
    public void An_amendment_one_tick_after_the_report_is_accepted() {
        // Arrange
        LabResult anyResult = AnyFinalResult();

        // Act
        anyResult.Amend(Any.Measurement().WithUnit(anyResult.Range.Unit).Generate(), anyResult.ReportedAt + TimeSpan.FromTicks(1));

        // Assert
        Assert.Equal(ResultStatus.Amended, anyResult.Status);
    }

    [Fact]
    public void An_amendment_at_the_very_instant_of_the_report_is_refused_and_changes_nothing() {
        // Arrange
        LabResult      anyResult     = AnyFinalResult();
        Measurement    original      = anyResult.Measurement;
        DateTimeOffset originalTime  = anyResult.ReportedAt;
        Measurement    anyCorrected  = Any.Measurement().WithUnit(anyResult.Range.Unit).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyResult.Amend(anyCorrected, originalTime));

        // Assert
        Assert.Equal("amendedAt", refusal.ParamName);
        Assert.Same(original, anyResult.Measurement);
        Assert.Equal(originalTime, anyResult.ReportedAt);
        Assert.Equal(ResultStatus.Final, anyResult.Status);
    }

    [Fact]
    public void An_amendment_before_the_report_is_refused() {
        // Arrange
        LabResult anyResult = AnyFinalResult();
        TimeSpan  anyAdvance = Any.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromDays(3_650)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => anyResult.Amend(Any.Measurement().WithUnit(anyResult.Range.Unit).Generate(), anyResult.ReportedAt - anyAdvance));
    }

    [Fact]
    public void An_amendment_is_judged_against_the_latest_report_time_not_the_original_one() {
        // Arrange
        LabResult      anyResult      = AnyFinalResult();
        DateTimeOffset originalTime   = anyResult.ReportedAt;
        DateTimeOffset firstAmendedAt = originalTime + TimeSpan.FromHours(2);
        anyResult.Amend(Any.Measurement().WithUnit(anyResult.Range.Unit).Generate(), firstAmendedAt);

        // Act & Assert: after the original report, but not after the first amendment.
        Assert.Throws<ArgumentException>(() => anyResult.Amend(Any.Measurement().WithUnit(anyResult.Range.Unit).Generate(), originalTime + TimeSpan.FromHours(1)));
        Assert.Equal(firstAmendedAt, anyResult.ReportedAt);
    }

    [Fact]
    public void A_preliminary_result_is_corrected_not_amended() {
        // Arrange
        LabResult   anyResult   = Any.LabResult().Generate();
        Measurement original    = anyResult.Measurement;
        Measurement anyCorrected = Any.Measurement().WithUnit(anyResult.Range.Unit).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyResult.Amend(anyCorrected, anyResult.ReportedAt + TimeSpan.FromHours(1)));
        Assert.Same(original, anyResult.Measurement);
        Assert.Equal(ResultStatus.Preliminary, anyResult.Status);
    }

    [Fact]
    public void A_missing_correction_is_refused_as_a_null_argument_before_the_status_is_looked_at() {
        // Arrange
        LabResult anyPreliminaryResult = Any.LabResult().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPreliminaryResult.Amend(null!, anyPreliminaryResult.ReportedAt + TimeSpan.FromHours(1)));

        // Assert
        Assert.Equal("corrected", refusal.ParamName);
    }

    [Fact]
    public void The_status_is_judged_before_the_unit_of_the_correction() {
        // Arrange: preliminary AND wrong unit.
        LabResult   anyResult    = Any.LabResult().WithRange(RangeWithCriticalLimits(new MeasurementUnit("mmol/L"))).Generate();
        Measurement wrongUnit    = Any.Measurement().WithUnit(new MeasurementUnit("g/dL")).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyResult.Amend(wrongUnit, anyResult.ReportedAt + TimeSpan.FromHours(1)));
    }

    [Fact]
    public void A_correction_in_another_unit_than_the_range_is_refused_and_changes_nothing() {
        // Arrange
        LabResult   anyResult   = Any.LabResult().WithRange(RangeWithCriticalLimits(new MeasurementUnit("mmol/L"))).Generate();
        anyResult.Release();
        Measurement original    = anyResult.Measurement;
        Measurement wrongUnit   = Any.Measurement().WithUnit(new MeasurementUnit("g/dL")).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyResult.Amend(wrongUnit, anyResult.ReportedAt + TimeSpan.FromHours(1)));

        // Assert
        Assert.Equal("corrected", refusal.ParamName);
        Assert.Same(original, anyResult.Measurement);
        Assert.Equal(ResultStatus.Final, anyResult.Status);
    }

    [Fact]
    public void The_unit_of_the_correction_is_judged_before_the_amendment_time() {
        // Arrange: wrong unit AND amendment not after the report.
        LabResult anyResult = Any.LabResult().WithRange(RangeWithCriticalLimits(new MeasurementUnit("mmol/L"))).Generate();
        anyResult.Release();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyResult.Amend(Any.Measurement().WithUnit(new MeasurementUnit("g/dL")).Generate(), anyResult.ReportedAt));

        // Assert
        Assert.Equal("corrected", refusal.ParamName);
    }

    [Fact]
    public void An_amendment_can_move_the_result_across_the_critical_limits() {
        // Arrange
        LabResult result = AnyResultMeasuring(15d);
        result.Release();
        Assert.False(result.RequiresPhoneCall);

        // Act
        result.Amend(new Measurement(31d, result.Range.Unit), result.ReportedAt + TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(ResultFlag.CriticalHigh, result.Flag);
        Assert.True(result.IsOutOfRange);
        Assert.True(result.RequiresPhoneCall);
    }

}
