namespace Laboratory.Domain;

/// <summary>One analyte measured on one specimen, judged against its reference range.</summary>
public sealed class LabResult {

    public LabResult(AnalyteCode analyte, Specimen specimen, Measurement measurement, ReferenceRange range, DateTimeOffset reportedAt) {
        ArgumentNullException.ThrowIfNull(analyte);
        ArgumentNullException.ThrowIfNull(specimen);
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(range);
        if (!measurement.Unit.Equals(range.Unit)) {
            throw new ArgumentException($"The measurement is in {measurement.Unit} but the range is in {range.Unit}.", nameof(range));
        }
        if (reportedAt < specimen.ReceivedAt) {
            throw new ArgumentException("A result cannot be reported before its specimen was received.", nameof(reportedAt));
        }

        Analyte     = analyte;
        Specimen    = specimen;
        Measurement = measurement;
        Range       = range;
        ReportedAt  = reportedAt;
        Status      = ResultStatus.Preliminary;
    }

    public AnalyteCode    Analyte     { get; }
    public Specimen       Specimen    { get; }
    public Measurement    Measurement { get; private set; }
    public ReferenceRange Range       { get; }
    public DateTimeOffset ReportedAt  { get; private set; }
    public ResultStatus   Status      { get; private set; }

    public ResultFlag Flag => Range.Classify(Measurement.Value);

    public bool IsOutOfRange => Flag != ResultFlag.Normal;

    public bool RequiresPhoneCall => Flag is ResultFlag.CriticalLow or ResultFlag.CriticalHigh;

    public void Release() {
        if (Status != ResultStatus.Preliminary) {
            throw new InvalidOperationException($"Only a preliminary result can be released; this one is {Status}.");
        }
        Status = ResultStatus.Final;
    }

    public void Amend(Measurement corrected, DateTimeOffset amendedAt) {
        ArgumentNullException.ThrowIfNull(corrected);
        if (Status == ResultStatus.Preliminary) {
            throw new InvalidOperationException("A preliminary result is corrected, not amended.");
        }
        if (!corrected.Unit.Equals(Range.Unit)) {
            throw new ArgumentException("An amendment must keep the unit of the reference range.", nameof(corrected));
        }
        if (amendedAt <= ReportedAt) {
            throw new ArgumentException("An amendment comes after the report it amends.", nameof(amendedAt));
        }
        Measurement = corrected;
        ReportedAt  = amendedAt;
        Status      = ResultStatus.Amended;
    }

}
