// Scaffolded by `any` (JustDummies), then completed by hand: the three ties `any` could not read are written in
// Generate() — the measurement takes the range's unit, and the report comes after the specimen's receipt.

using System;

using JustDummies;

namespace Laboratory.Domain;

public sealed partial class AnyLabResult : IAny<LabResult> {

    private IAny<AnalyteCode>    _analyte;
    private IAny<Specimen>       _specimen;
    private IAny<ReferenceRange> _range;

    private AnyAnalyteCode    _analyteRecipe;
    private AnySpecimen       _specimenRecipe;
    private AnyMeasurement    _measurementRecipe;
    private AnyReferenceRange _rangeRecipe;
    private AnyTimeSpan       _reportDelayRecipe;

    private Measurement?    _measurementPin;
    private DateTimeOffset? _reportedAtPin;

    public AnyLabResult()
        : this(new AnyAnalyteCode(), new AnySpecimen(), new AnyMeasurement(), new AnyReferenceRange(), AnyValidReportDelay()) { }

    // Delay between the receipt of the specimen and the report: never negative.
    private static AnyTimeSpan AnyValidReportDelay() {
        return Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(30));
    }

    private AnyLabResult(AnyAnalyteCode analyte, AnySpecimen specimen, AnyMeasurement measurement, AnyReferenceRange range, AnyTimeSpan reportDelay) {
        _analyte           = analyte;
        _analyteRecipe     = analyte;
        _specimen          = specimen;
        _specimenRecipe    = specimen;
        _measurementRecipe = measurement;
        _range             = range;
        _rangeRecipe       = range;
        _reportDelayRecipe = reportDelay;
    }

    private AnyLabResult(AnyLabResult from) {
        _analyte           = from._analyte;
        _analyteRecipe     = from._analyteRecipe;
        _specimen          = from._specimen;
        _specimenRecipe    = from._specimenRecipe;
        _measurementRecipe = from._measurementRecipe;
        _measurementPin    = from._measurementPin;
        _range             = from._range;
        _rangeRecipe       = from._rangeRecipe;
        _reportDelayRecipe = from._reportDelayRecipe;
        _reportedAtPin     = from._reportedAtPin;
    }

    public AnyLabResult WithAnalyte(AnalyteCode value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._analyte = Any.Fixed(value);

        return copy;
    }

    public AnyLabResult WithAnalyte(Func<AnyAnalyteCode, AnyAnalyteCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._analyteRecipe = configure(_analyteRecipe);
        copy._analyte       = copy._analyteRecipe;

        return copy;
    }

    public AnyLabResult WithSpecimen(Specimen value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._specimen = Any.Fixed(value);

        return copy;
    }

    public AnyLabResult WithSpecimen(Func<AnySpecimen, AnySpecimen> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._specimenRecipe = configure(_specimenRecipe);
        copy._specimen       = copy._specimenRecipe;

        return copy;
    }

    /// <summary>Pins the measurement; it is the caller's job to give it the unit of the range.</summary>
    public AnyLabResult WithMeasurement(Measurement value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._measurementPin = value;

        return copy;
    }

    /// <summary>Refines the measurement; whatever the refinement does, it takes the unit of the range.</summary>
    public AnyLabResult WithMeasurement(Func<AnyMeasurement, AnyMeasurement> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._measurementRecipe = configure(_measurementRecipe);
        copy._measurementPin    = null;

        return copy;
    }

    public AnyLabResult WithRange(ReferenceRange value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._range = Any.Fixed(value);

        return copy;
    }

    public AnyLabResult WithRange(Func<AnyReferenceRange, AnyReferenceRange> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._rangeRecipe = configure(_rangeRecipe);
        copy._range       = copy._rangeRecipe;

        return copy;
    }

    public AnyLabResult WithReportedAt(DateTimeOffset value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._reportedAtPin = value;

        return copy;
    }

    /// <summary>Refines the delay between the receipt of the specimen and the report.</summary>
    public AnyLabResult WithReportDelay(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._reportDelayRecipe = configure(_reportDelayRecipe);
        copy._reportedAtPin     = null;

        return copy;
    }

    public LabResult Generate() {
        Specimen       specimen = _specimen.Generate();
        ReferenceRange range    = _range.Generate();
        Measurement    measurement = _measurementPin ?? _measurementRecipe.WithUnit(range.Unit).Generate();
        DateTimeOffset reportedAt  = _reportedAtPin ?? specimen.ReceivedAt + _reportDelayRecipe.Generate();

        return new LabResult(_analyte.Generate(), specimen, measurement, range, reportedAt);
    }

}
