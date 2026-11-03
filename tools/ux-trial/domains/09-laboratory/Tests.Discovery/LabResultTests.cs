using JustDummies;

using Laboratory.Domain;

using Xunit;

namespace Laboratory.Tests.Discovery;

public sealed class LabResultTests {

    private static readonly DateTimeOffset ReceivedAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static Specimen ReceivedSpecimen() => Any.Specimen().WithCollectedAt(ReceivedAt.AddHours(-4)).WithReceivedAt(ReceivedAt).Generate();

    // A result whose range runs 10..20, critical at or below 5 and at or above 30, in the drawn unit of both.
    private static LabResult ResultOf(double value) {
        MeasurementUnit unit  = Any.MeasurementUnit().Generate();
        ReferenceRange  range = new ReferenceRange(10d, 20d, unit, 5d, 30d);

        return new LabResult(Any.AnalyteCode().Generate(), ReceivedSpecimen(), new Measurement(value, unit), range, ReceivedAt.AddHours(2));
    }

    [Fact]
    public void A_result_keeps_what_it_was_given_and_starts_preliminary() {
        // Arrange
        AnalyteCode     anyAnalyte = Any.AnalyteCode().Generate();
        Specimen        specimen   = ReceivedSpecimen();
        MeasurementUnit anyUnit    = Any.MeasurementUnit().Generate();
        Measurement     measure    = new Measurement(12d, anyUnit);
        ReferenceRange  range      = new ReferenceRange(10d, 20d, anyUnit);
        DateTimeOffset  reportedAt = ReceivedAt.AddHours(1);

        // Act
        LabResult result = new LabResult(anyAnalyte, specimen, measure, range, reportedAt);

        // Assert
        Assert.Same(anyAnalyte, result.Analyte);
        Assert.Same(specimen, result.Specimen);
        Assert.Same(measure, result.Measurement);
        Assert.Same(range, result.Range);
        Assert.Equal(reportedAt, result.ReportedAt);
        Assert.Equal(ResultStatus.Preliminary, result.Status);
    }

    [Fact]
    public void A_missing_analyte_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LabResult(null!, anyResult.Specimen, anyResult.Measurement, anyResult.Range, anyResult.ReportedAt));
    }

    [Fact]
    public void A_missing_specimen_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LabResult(anyResult.Analyte, null!, anyResult.Measurement, anyResult.Range, anyResult.ReportedAt));
    }

    [Fact]
    public void A_missing_measurement_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LabResult(anyResult.Analyte, anyResult.Specimen, null!, anyResult.Range, anyResult.ReportedAt));
    }

    [Fact]
    public void A_missing_range_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LabResult(anyResult.Analyte, anyResult.Specimen, anyResult.Measurement, null!, anyResult.ReportedAt));
    }

    [Fact]
    public void A_measurement_in_another_unit_than_the_range_is_refused() {
        // Arrange
        MeasurementUnit anyUnit      = Any.MeasurementUnit().Generate();
        MeasurementUnit anyOtherUnit = Any.MeasurementUnit().WithSymbol(s => s.DifferentFrom(anyUnit.Symbol)).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new LabResult(Any.AnalyteCode().Generate(),
                                                             ReceivedSpecimen(),
                                                             new Measurement(12d, anyOtherUnit),
                                                             new ReferenceRange(10d, 20d, anyUnit),
                                                             ReceivedAt.AddHours(1)));
    }

    [Fact]
    public void Units_that_differ_only_by_case_do_not_match() {
        // Assert
        Assert.Throws<ArgumentException>(() => new LabResult(Any.AnalyteCode().Generate(),
                                                             ReceivedSpecimen(),
                                                             new Measurement(12d, new MeasurementUnit("mmol/l")),
                                                             new ReferenceRange(10d, 20d, new MeasurementUnit("mmol/L")),
                                                             ReceivedAt.AddHours(1)));
    }

    [Fact]
    public void Units_with_the_same_symbol_match_even_when_they_are_distinct_instances() {
        // Arrange
        string anySymbol = Any.MeasurementUnit().Generate().Symbol;

        // Act
        LabResult result = new LabResult(Any.AnalyteCode().Generate(),
                                         ReceivedSpecimen(),
                                         new Measurement(12d, new MeasurementUnit(anySymbol)),
                                         new ReferenceRange(10d, 20d, new MeasurementUnit(anySymbol)),
                                         ReceivedAt.AddHours(1));

        // Assert
        Assert.Equal(anySymbol, result.Measurement.Unit.Symbol);
    }

    [Fact]
    public void A_result_cannot_be_reported_before_its_specimen_was_received() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new LabResult(anyResult.Analyte, anyResult.Specimen, anyResult.Measurement, anyResult.Range,
                                                             anyResult.Specimen.ReceivedAt.AddTicks(-1)));
    }

    [Fact]
    public void A_result_reported_at_the_very_instant_of_the_receipt_is_accepted() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        LabResult result = new LabResult(anyResult.Analyte, anyResult.Specimen, anyResult.Measurement, anyResult.Range, anyResult.Specimen.ReceivedAt);

        // Assert
        Assert.Equal(anyResult.Specimen.ReceivedAt, result.ReportedAt);
    }

    [Fact]
    public void A_result_reported_after_the_collection_but_before_the_receipt_is_refused() {
        // Arrange: reported after the collection but before the receipt
        Specimen specimen = ReceivedSpecimen();
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new LabResult(anyResult.Analyte, specimen, new Measurement(1d, anyResult.Range.Unit), anyResult.Range, specimen.CollectedAt.AddHours(1)));
    }

    [Theory]
    [InlineData(2d,   ResultFlag.CriticalLow,  true,  true)]
    [InlineData(5d,   ResultFlag.CriticalLow,  true,  true)]
    [InlineData(7d,   ResultFlag.Low,          true,  false)]
    [InlineData(10d,  ResultFlag.Normal,       false, false)]
    [InlineData(20d,  ResultFlag.Normal,       false, false)]
    [InlineData(25d,  ResultFlag.High,         true,  false)]
    [InlineData(30d,  ResultFlag.CriticalHigh, true,  true)]
    [InlineData(99d,  ResultFlag.CriticalHigh, true,  true)]
    public void The_flag_follows_the_measurement_and_drives_the_two_derived_properties(double value, ResultFlag flag, bool outOfRange, bool phoneCall) {
        // Arrange
        LabResult result = ResultOf(value);

        // Assert
        Assert.Equal(flag, result.Flag);
        Assert.Equal(outOfRange, result.IsOutOfRange);
        Assert.Equal(phoneCall, result.RequiresPhoneCall);
    }

    [Fact]
    public void A_drawn_result_is_valid_and_preliminary() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Assert
        Assert.Equal(ResultStatus.Preliminary, anyResult.Status);
        Assert.Equal(anyResult.Range.Unit, anyResult.Measurement.Unit);
        Assert.True(anyResult.ReportedAt >= anyResult.Specimen.ReceivedAt);
    }

    [Fact]
    public void A_preliminary_result_can_be_released() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        anyResult.Release();

        // Assert
        Assert.Equal(ResultStatus.Final, anyResult.Status);
    }

    [Fact]
    public void Releasing_leaves_the_measurement_and_the_report_date_alone() {
        // Arrange
        LabResult      anyResult  = Any.LabResult().Generate();
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
        LabResult anyResult = Any.LabResult().Generate();
        anyResult.Release();

        // Assert
        Assert.Throws<InvalidOperationException>(() => anyResult.Release());
        Assert.Equal(ResultStatus.Final, anyResult.Status);
    }

    [Fact]
    public void An_amended_result_cannot_be_released() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();
        anyResult.Release();
        anyResult.Amend(new Measurement(1d, anyResult.Range.Unit), anyResult.ReportedAt.AddMinutes(1));

        // Assert
        Assert.Throws<InvalidOperationException>(() => anyResult.Release());
        Assert.Equal(ResultStatus.Amended, anyResult.Status);
    }

    [Fact]
    public void A_final_result_can_be_amended_and_the_flag_follows_the_new_measurement() {
        // Arrange
        LabResult      result    = ResultOf(2d);
        DateTimeOffset amendedAt = result.ReportedAt.AddHours(1);
        Measurement    corrected = new Measurement(15d, result.Range.Unit);
        result.Release();

        // Act
        result.Amend(corrected, amendedAt);

        // Assert
        Assert.Equal(ResultStatus.Amended, result.Status);
        Assert.Same(corrected, result.Measurement);
        Assert.Equal(amendedAt, result.ReportedAt);
        Assert.Equal(ResultFlag.Normal, result.Flag);
        Assert.False(result.RequiresPhoneCall);
    }

    [Fact]
    public void An_amended_result_can_be_amended_again() {
        // Arrange
        LabResult      anyResult = Any.LabResult().Generate();
        DateTimeOffset firstAmendment  = anyResult.ReportedAt.AddHours(1);
        DateTimeOffset secondAmendment = firstAmendment.AddHours(1);
        anyResult.Release();
        anyResult.Amend(new Measurement(1d, anyResult.Range.Unit), firstAmendment);

        // Act
        anyResult.Amend(new Measurement(2d, anyResult.Range.Unit), secondAmendment);

        // Assert
        Assert.Equal(ResultStatus.Amended, anyResult.Status);
        Assert.Equal(2d, anyResult.Measurement.Value);
        Assert.Equal(secondAmendment, anyResult.ReportedAt);
    }

    [Fact]
    public void A_preliminary_result_is_corrected_not_amended() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();
        double    before    = anyResult.Measurement.Value;

        // Assert
        Assert.Throws<InvalidOperationException>(() => anyResult.Amend(new Measurement(1d, anyResult.Range.Unit), anyResult.ReportedAt.AddHours(1)));
        Assert.Equal(ResultStatus.Preliminary, anyResult.Status);
        Assert.Equal(before, anyResult.Measurement.Value);
    }

    [Fact]
    public void An_amendment_without_a_measurement_is_refused_whatever_the_status() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Assert: the null check comes before the status check
        Assert.Throws<ArgumentNullException>(() => anyResult.Amend(null!, anyResult.ReportedAt.AddHours(1)));
    }

    [Fact]
    public void The_status_is_checked_before_the_unit_of_an_amendment() {
        // Arrange
        LabResult       anyResult   = Any.LabResult().Generate();
        MeasurementUnit anyOtherUnit = Any.MeasurementUnit().WithSymbol(s => s.DifferentFrom(anyResult.Range.Unit.Symbol)).Generate();

        // Assert: preliminary AND wrong unit: the status is reported
        Assert.Throws<InvalidOperationException>(() => anyResult.Amend(new Measurement(1d, anyOtherUnit), anyResult.ReportedAt.AddHours(1)));
    }

    [Fact]
    public void An_amendment_must_keep_the_unit_of_the_range() {
        // Arrange
        LabResult       anyResult    = Any.LabResult().Generate();
        MeasurementUnit anyOtherUnit = Any.MeasurementUnit().WithSymbol(s => s.DifferentFrom(anyResult.Range.Unit.Symbol)).Generate();
        anyResult.Release();
        Measurement before = anyResult.Measurement;

        // Assert
        Assert.Throws<ArgumentException>(() => anyResult.Amend(new Measurement(1d, anyOtherUnit), anyResult.ReportedAt.AddHours(1)));
        Assert.Equal(ResultStatus.Final, anyResult.Status);
        Assert.Same(before, anyResult.Measurement);
    }

    [Fact]
    public void The_unit_is_checked_before_the_date_of_an_amendment() {
        // Arrange
        LabResult       anyResult    = Any.LabResult().Generate();
        MeasurementUnit anyOtherUnit = Any.MeasurementUnit().WithSymbol(s => s.DifferentFrom(anyResult.Range.Unit.Symbol)).Generate();
        anyResult.Release();

        // Assert: wrong unit AND a date in the past: both are ArgumentException, the parameter name tells which one
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyResult.Amend(new Measurement(1d, anyOtherUnit), anyResult.ReportedAt.AddHours(-1)));
        Assert.Equal("corrected", refusal.ParamName);
    }

    [Fact]
    public void An_amendment_dated_at_the_report_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();
        anyResult.Release();

        // Assert: strictly after
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyResult.Amend(new Measurement(1d, anyResult.Range.Unit), anyResult.ReportedAt));
        Assert.Equal("amendedAt", refusal.ParamName);
    }

    [Fact]
    public void An_amendment_dated_before_the_report_is_refused() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();
        anyResult.Release();

        // Assert
        Assert.Throws<ArgumentException>(() => anyResult.Amend(new Measurement(1d, anyResult.Range.Unit), anyResult.ReportedAt.AddTicks(-1)));
    }

    [Fact]
    public void An_amendment_one_tick_after_the_report_is_accepted() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();
        anyResult.Release();

        // Act
        anyResult.Amend(new Measurement(1d, anyResult.Range.Unit), anyResult.ReportedAt.AddTicks(1));

        // Assert
        Assert.Equal(ResultStatus.Amended, anyResult.Status);
    }

    [Fact]
    public void A_refused_amendment_changes_nothing() {
        // Arrange
        LabResult      anyResult = Any.LabResult().Generate();
        anyResult.Release();
        Measurement    measurement = anyResult.Measurement;
        DateTimeOffset reportedAt  = anyResult.ReportedAt;

        // Act
        Assert.Throws<ArgumentException>(() => anyResult.Amend(new Measurement(1d, anyResult.Range.Unit), reportedAt.AddHours(-1)));

        // Assert
        Assert.Equal(ResultStatus.Final, anyResult.Status);
        Assert.Same(measurement, anyResult.Measurement);
        Assert.Equal(reportedAt, anyResult.ReportedAt);
    }

}
