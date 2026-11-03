namespace Laboratory.Tests;

public sealed class LabResultTests {

    // ── construction ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_result_exposes_what_it_was_built_from_and_starts_preliminary() {
        // Arrange
        AnalyteCode    potassium   = new AnalyteCode("2823-3");
        Specimen       specimen    = Any.Specimen().Generate();
        MeasurementUnit unit       = Any.MeasurementUnit().Generate();
        Measurement    measurement = new Measurement(4.2, unit);
        ReferenceRange range       = new ReferenceRange(3.5, 5.1, unit);
        DateTimeOffset reportedAt  = specimen.ReceivedAt + TimeSpan.FromHours(2);

        // Act
        LabResult result = new LabResult(potassium, specimen, measurement, range, reportedAt);

        // Assert
        Assert.Same(potassium, result.Analyte);
        Assert.Same(specimen, result.Specimen);
        Assert.Same(measurement, result.Measurement);
        Assert.Same(range, result.Range);
        Assert.Equal(reportedAt, result.ReportedAt);
        Assert.Equal(ResultStatus.Preliminary, result.Status);
    }

    [Fact]
    public void A_result_without_an_analyte_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LabResult(null!, anyResult.Specimen, anyResult.Measurement, anyResult.Range, anyResult.ReportedAt));

        // Assert
        Assert.Equal("analyte", refusal.ParamName);
    }

    [Fact]
    public void A_result_without_a_specimen_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LabResult(anyResult.Analyte, null!, anyResult.Measurement, anyResult.Range, anyResult.ReportedAt));

        // Assert
        Assert.Equal("specimen", refusal.ParamName);
    }

    [Fact]
    public void A_result_without_a_measurement_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LabResult(anyResult.Analyte, anyResult.Specimen, null!, anyResult.Range, anyResult.ReportedAt));

        // Assert
        Assert.Equal("measurement", refusal.ParamName);
    }

    [Fact]
    public void A_result_without_a_range_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new LabResult(anyResult.Analyte, anyResult.Specimen, anyResult.Measurement, null!, anyResult.ReportedAt));

        // Assert
        Assert.Equal("range", refusal.ParamName);
    }

    [Fact]
    public void A_measurement_in_another_unit_than_the_range_is_refused() {
        // Arrange
        LabResult       anyResult        = Any.LabResult().Generate();
        Measurement inTheOtherUnit = MeasurementInAnotherUnitThan(anyResult.Range.Unit);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(anyResult.Analyte, anyResult.Specimen, inTheOtherUnit, anyResult.Range, anyResult.ReportedAt));

        // Assert
        Assert.Equal("range", refusal.ParamName);
    }

    [Fact]
    public void Units_differing_only_by_case_do_not_match() {
        // Arrange
        Specimen        anySpecimen = Any.Specimen().Generate();
        Measurement     inUpper     = new Measurement(4.2, new MeasurementUnit("mmol/L"));
        ReferenceRange  inLower     = new ReferenceRange(3.5, 5.1, new MeasurementUnit("mmol/l"));

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new LabResult(Any.AnalyteCode().Generate(), anySpecimen, inUpper, inLower, anySpecimen.ReceivedAt));
    }

    [Fact]
    public void A_result_reported_at_the_instant_the_specimen_was_received_is_accepted() {
        // Arrange
        Specimen anySpecimen = Any.Specimen().Generate();

        // Act
        LabResult result = Any.LabResult().WithSpecimen(anySpecimen).WithReportedAt(anySpecimen.ReceivedAt).Generate();

        // Assert
        Assert.Equal(anySpecimen.ReceivedAt, result.ReportedAt);
    }

    [Fact]
    public void A_result_reported_before_the_specimen_was_received_is_refused() {
        // Arrange
        Specimen       anySpecimen   = Any.Specimen().Generate();
        DateTimeOffset oneTickEarlier = anySpecimen.ReceivedAt - TimeSpan.FromTicks(1);
        LabResult      anyResult     = Any.LabResult().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(anyResult.Analyte, anySpecimen, anyResult.Measurement, anyResult.Range, oneTickEarlier));

        // Assert
        Assert.Equal("reportedAt", refusal.ParamName);
    }

    // ── flags ───────────────────────────────────────────────────────────────────────────────────
    // Potassium in mmol/L: normal 3.5 – 5.1, critical at 2.5 and below, 6.5 and above.

    [Theory]
    [InlineData(2.0, ResultFlag.CriticalLow,  true,  true)]
    [InlineData(3.0, ResultFlag.Low,          true,  false)]
    [InlineData(4.2, ResultFlag.Normal,       false, false)]
    [InlineData(5.8, ResultFlag.High,         true,  false)]
    [InlineData(7.0, ResultFlag.CriticalHigh, true,  true)]
    public void A_result_is_flagged_by_its_range_and_only_the_critical_ones_call_for_a_phone_call(double value, ResultFlag expectedFlag, bool outOfRange, bool phoneCall) {
        // Arrange
        MeasurementUnit anyUnit   = Any.MeasurementUnit().Generate();
        ReferenceRange  potassium = new ReferenceRange(3.5, 5.1, anyUnit, criticalLow: 2.5, criticalHigh: 6.5);

        // Act
        LabResult result = Any.LabResult().WithRange(potassium).WithValue(value).Generate();

        // Assert
        Assert.Equal(expectedFlag, result.Flag);
        Assert.Equal(outOfRange, result.IsOutOfRange);
        Assert.Equal(phoneCall, result.RequiresPhoneCall);
    }

    [Fact]
    public void A_value_on_a_critical_limit_requires_a_phone_call() {
        // Arrange
        ReferenceRange potassium = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalLow: 2.5, criticalHigh: 6.5);

        // Act
        LabResult atCriticalLow  = Any.LabResult().WithRange(potassium).WithValue(2.5).Generate();
        LabResult atCriticalHigh = Any.LabResult().WithRange(potassium).WithValue(6.5).Generate();

        // Assert
        Assert.True(atCriticalLow.RequiresPhoneCall);
        Assert.True(atCriticalHigh.RequiresPhoneCall);
    }

    [Fact]
    public void A_value_on_a_bound_of_the_normal_interval_is_not_out_of_range() {
        // Arrange
        ReferenceRange potassium = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalLow: 2.5, criticalHigh: 6.5);

        // Act
        LabResult atLower = Any.LabResult().WithRange(potassium).WithValue(3.5).Generate();
        LabResult atUpper = Any.LabResult().WithRange(potassium).WithValue(5.1).Generate();

        // Assert
        Assert.False(atLower.IsOutOfRange);
        Assert.False(atUpper.IsOutOfRange);
    }

    // ── lifecycle ───────────────────────────────────────────────────────────────────────────────

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
    public void A_final_result_cannot_be_released_again() {
        // Arrange
        LabResult anyFinalResult = Any.LabResult().Generate();
        anyFinalResult.Release();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyFinalResult.Release());
        Assert.Equal(ResultStatus.Final, anyFinalResult.Status);
    }

    [Fact]
    public void An_amended_result_cannot_be_released() {
        // Arrange
        LabResult anyAmendedResult = Any.LabResult().Generate();
        anyAmendedResult.Release();
        anyAmendedResult.Amend(new Measurement(1d, anyAmendedResult.Range.Unit), anyAmendedResult.ReportedAt + TimeSpan.FromMinutes(1));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyAmendedResult.Release());
        Assert.Equal(ResultStatus.Amended, anyAmendedResult.Status);
    }

    [Fact]
    public void Amending_a_final_result_replaces_the_measurement_and_the_report_time() {
        // Arrange
        LabResult      anyFinalResult = Any.LabResult().Generate();
        anyFinalResult.Release();
        Measurement    corrected      = new Measurement(4.4, anyFinalResult.Range.Unit);
        DateTimeOffset amendedAt      = anyFinalResult.ReportedAt + TimeSpan.FromMinutes(30);

        // Act
        anyFinalResult.Amend(corrected, amendedAt);

        // Assert
        Assert.Same(corrected, anyFinalResult.Measurement);
        Assert.Equal(amendedAt, anyFinalResult.ReportedAt);
        Assert.Equal(ResultStatus.Amended, anyFinalResult.Status);
    }

    [Fact]
    public void An_amended_result_can_be_amended_again() {
        // Arrange
        LabResult anyAmendedResult = Any.LabResult().Generate();
        anyAmendedResult.Release();
        anyAmendedResult.Amend(new Measurement(1d, anyAmendedResult.Range.Unit), anyAmendedResult.ReportedAt + TimeSpan.FromMinutes(10));
        Measurement    secondCorrection = new Measurement(2d, anyAmendedResult.Range.Unit);
        DateTimeOffset secondAmendedAt  = anyAmendedResult.ReportedAt + TimeSpan.FromMinutes(10);

        // Act
        anyAmendedResult.Amend(secondCorrection, secondAmendedAt);

        // Assert
        Assert.Same(secondCorrection, anyAmendedResult.Measurement);
        Assert.Equal(secondAmendedAt, anyAmendedResult.ReportedAt);
        Assert.Equal(ResultStatus.Amended, anyAmendedResult.Status);
    }

    [Fact]
    public void An_amendment_can_turn_a_normal_result_into_one_that_requires_a_phone_call() {
        // Arrange
        ReferenceRange potassium = new ReferenceRange(3.5, 5.1, Any.MeasurementUnit().Generate(), criticalLow: 2.5, criticalHigh: 6.5);
        LabResult      normal    = Any.LabResult().WithRange(potassium).WithValue(4.2).Generate();
        normal.Release();

        // Act
        normal.Amend(new Measurement(7.0, potassium.Unit), normal.ReportedAt + TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(ResultFlag.CriticalHigh, normal.Flag);
        Assert.True(normal.RequiresPhoneCall);
    }

    [Fact]
    public void A_preliminary_result_cannot_be_amended() {
        // Arrange
        LabResult anyPreliminaryResult = Any.LabResult().Generate();
        Measurement anyCorrection      = new Measurement(1d, anyPreliminaryResult.Range.Unit);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyPreliminaryResult.Amend(anyCorrection, anyPreliminaryResult.ReportedAt + TimeSpan.FromMinutes(1)));
        Assert.Equal(ResultStatus.Preliminary, anyPreliminaryResult.Status);
    }

    [Fact]
    public void An_amendment_without_a_measurement_is_refused_before_anything_else_is_looked_at() {
        // Arrange: the result is still preliminary, which would be refused too
        LabResult anyPreliminaryResult = Any.LabResult().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPreliminaryResult.Amend(null!, anyPreliminaryResult.ReportedAt));

        // Assert
        Assert.Equal("corrected", refusal.ParamName);
    }

    [Fact]
    public void The_status_is_judged_before_the_unit_of_the_amendment() {
        // Arrange: a preliminary result and a correction in the wrong unit
        LabResult       anyPreliminaryResult = Any.LabResult().Generate();
        Measurement inTheOtherUnit = MeasurementInAnotherUnitThan(anyPreliminaryResult.Range.Unit);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyPreliminaryResult.Amend(inTheOtherUnit, anyPreliminaryResult.ReportedAt + TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void An_amendment_in_another_unit_than_the_range_is_refused_and_changes_nothing() {
        // Arrange
        LabResult       anyFinalResult   = Any.LabResult().Generate();
        anyFinalResult.Release();
        Measurement inTheOtherUnit = MeasurementInAnotherUnitThan(anyFinalResult.Range.Unit);
        Measurement     before           = anyFinalResult.Measurement;
        DateTimeOffset  reportedAtBefore = anyFinalResult.ReportedAt;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyFinalResult.Amend(inTheOtherUnit, reportedAtBefore + TimeSpan.FromMinutes(1)));

        // Assert
        Assert.Equal("corrected", refusal.ParamName);
        Assert.Same(before, anyFinalResult.Measurement);
        Assert.Equal(reportedAtBefore, anyFinalResult.ReportedAt);
        Assert.Equal(ResultStatus.Final, anyFinalResult.Status);
    }

    [Fact]
    public void An_amendment_at_the_very_instant_of_the_report_is_refused() {
        // Arrange
        LabResult anyFinalResult = Any.LabResult().Generate();
        anyFinalResult.Release();
        Measurement anyCorrection = new Measurement(1d, anyFinalResult.Range.Unit);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyFinalResult.Amend(anyCorrection, anyFinalResult.ReportedAt));

        // Assert: strictly after is required
        Assert.Equal("amendedAt", refusal.ParamName);
    }

    [Fact]
    public void An_amendment_dated_before_the_report_is_refused_and_changes_nothing() {
        // Arrange
        LabResult anyFinalResult = Any.LabResult().Generate();
        anyFinalResult.Release();
        Measurement    before           = anyFinalResult.Measurement;
        DateTimeOffset reportedAtBefore = anyFinalResult.ReportedAt;

        // Act
        Assert.Throws<ArgumentException>(() => anyFinalResult.Amend(new Measurement(1d, anyFinalResult.Range.Unit), reportedAtBefore - TimeSpan.FromTicks(1)));

        // Assert
        Assert.Same(before, anyFinalResult.Measurement);
        Assert.Equal(reportedAtBefore, anyFinalResult.ReportedAt);
        Assert.Equal(ResultStatus.Final, anyFinalResult.Status);
    }

    [Fact]
    public void An_amendment_one_tick_after_the_report_is_accepted() {
        // Arrange
        LabResult anyFinalResult = Any.LabResult().Generate();
        anyFinalResult.Release();

        // Act
        anyFinalResult.Amend(new Measurement(1d, anyFinalResult.Range.Unit), anyFinalResult.ReportedAt + TimeSpan.FromTicks(1));

        // Assert
        Assert.Equal(ResultStatus.Amended, anyFinalResult.Status);
    }

    [Fact]
    public void The_unit_is_judged_before_the_date_of_the_amendment() {
        // Arrange: wrong unit AND a date that is not after the report
        LabResult       anyFinalResult = Any.LabResult().Generate();
        anyFinalResult.Release();
        Measurement inTheOtherUnit = MeasurementInAnotherUnitThan(anyFinalResult.Range.Unit);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyFinalResult.Amend(inTheOtherUnit, anyFinalResult.ReportedAt));

        // Assert
        Assert.Equal("corrected", refusal.ParamName);
    }

    private static Measurement MeasurementInAnotherUnitThan(MeasurementUnit unit) {
        return Any.Measurement().WithUnit(Any.MeasurementUnit().DifferentFrom(unit).Generate()).Generate();
    }

}
