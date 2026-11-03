// Scaffolded by `any` (JustDummies), then completed by hand. This file is yours: read it, edit it, commit it.
// Two rules tie the parameters together and the scaffold could not express them:
//   - the measurement and the range share one unit;
//   - the result is reported at or after the specimen's reception.
// The first is met by drawing whichever of the two is not pinned in the unit of the other; the second by
// drawing a reporting delay and deriving reportedAt from the reception (or the reception from a pinned reportedAt).

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LabResult" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLabResult : IAny<LabResult> {

    private IAny<AnalyteCode> _analyte;
    private IAny<TimeSpan>    _delay;

    private AnyAnalyteCode    _analyteRecipe;
    private AnySpecimen       _specimenRecipe;
    private AnyMeasurement    _measurementRecipe;
    private AnyReferenceRange _rangeRecipe;
    private AnyTimeSpan       _delayRecipe;

    private Specimen?        _specimenPin;
    private Measurement?     _measurementPin;
    private ReferenceRange?  _rangePin;
    private DateTimeOffset?  _reportedAtPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLabResult()
        : this(analyte:     AnyValidAnalyte(),
               specimen:    AnyValidSpecimen(),
               measurement: AnyValidMeasurement(),
               range:       AnyValidRange(),
               delay:       AnyValidDelay()) { }

    private static AnyAnalyteCode AnyValidAnalyte() {
        return new AnyAnalyteCode();
    }

    private static AnySpecimen AnyValidSpecimen() {
        return new AnySpecimen();
    }

    private static AnyMeasurement AnyValidMeasurement() {
        return new AnyMeasurement();
    }

    private static AnyReferenceRange AnyValidRange() {
        return new AnyReferenceRange();
    }

    private static AnyTimeSpan AnyValidDelay() {
        return Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(7));
    }

    private AnyLabResult(AnyAnalyteCode    analyte,
                         AnySpecimen       specimen,
                         AnyMeasurement    measurement,
                         AnyReferenceRange range,
                         AnyTimeSpan       delay) {
        _analyte           = analyte;
        _analyteRecipe     = analyte;
        _specimenRecipe    = specimen;
        _measurementRecipe = measurement;
        _rangeRecipe       = range;
        _delay             = delay;
        _delayRecipe       = delay;
    }

    private AnyLabResult(AnyLabResult from) {
        _analyte           = from._analyte;
        _analyteRecipe     = from._analyteRecipe;
        _specimenRecipe    = from._specimenRecipe;
        _specimenPin       = from._specimenPin;
        _measurementRecipe = from._measurementRecipe;
        _measurementPin    = from._measurementPin;
        _rangeRecipe       = from._rangeRecipe;
        _rangePin          = from._rangePin;
        _delay             = from._delay;
        _delayRecipe       = from._delayRecipe;
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

        copy._specimenPin = value;

        return copy;
    }

    public AnyLabResult WithSpecimen(Func<AnySpecimen, AnySpecimen> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._specimenRecipe = configure(_specimenRecipe);
        copy._specimenPin    = null;

        return copy;
    }

    /// <summary>Pins the measurement; the range, unless pinned, is drawn in its unit.</summary>
    public AnyLabResult WithMeasurement(Measurement value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._measurementPin = value;

        return copy;
    }

    /// <summary>Refines the measurement recipe; its unit is that of the range, whatever the refinement says.</summary>
    public AnyLabResult WithMeasurement(Func<AnyMeasurement, AnyMeasurement> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._measurementRecipe = configure(_measurementRecipe);
        copy._measurementPin    = null;

        return copy;
    }

    /// <summary>Pins the range; the measurement, unless pinned, is drawn in its unit.</summary>
    public AnyLabResult WithRange(ReferenceRange value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._rangePin = value;

        return copy;
    }

    public AnyLabResult WithRange(Func<AnyReferenceRange, AnyReferenceRange> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._rangeRecipe = configure(_rangeRecipe);
        copy._rangePin    = null;

        return copy;
    }

    /// <summary>Pins the report instant; the specimen, unless pinned, is received before it.</summary>
    public AnyLabResult WithReportedAt(DateTimeOffset value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._reportedAtPin = value;

        return copy;
    }

    /// <summary>Pins the delay between the specimen's reception and the report.</summary>
    public AnyLabResult WithDelay(TimeSpan value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._delay = Any.Fixed(value);

        return copy;
    }

    public AnyLabResult WithDelay(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabResult copy = new AnyLabResult(this);

        copy._delayRecipe = configure(_delayRecipe);
        copy._delay       = copy._delayRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LabResult" />.</summary>
    public LabResult Generate() {
        ReferenceRange range;
        Measurement    measurement;

        if (_measurementPin is { } pinnedMeasurement) {
            measurement = pinnedMeasurement;
            range       = _rangePin ?? _rangeRecipe.WithUnit(measurement.Unit).Generate();
        } else {
            range       = _rangePin ?? _rangeRecipe.Generate();
            measurement = _measurementRecipe.WithUnit(range.Unit).Generate();
        }

        TimeSpan delay = _delay.Generate();
        Specimen specimen;
        DateTimeOffset reportedAt;

        if (_specimenPin is { } pinnedSpecimen) {
            specimen   = pinnedSpecimen;
            reportedAt = _reportedAtPin ?? specimen.ReceivedAt + delay;
        } else if (_reportedAtPin is { } pinnedReport) {
            reportedAt = pinnedReport;
            specimen   = _specimenRecipe.WithReceivedAt(reportedAt - delay).Generate();
        } else {
            specimen   = _specimenRecipe.Generate();
            reportedAt = specimen.ReceivedAt + delay;
        }

        return new LabResult(_analyte.Generate(),
                             specimen,
                             measurement,
                             range,
                             reportedAt);
    }

}
