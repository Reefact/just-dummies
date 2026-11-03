using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LabResult" /> values. Started from the scaffold, then reworked by hand
///     around the two ties the constructor imposes: the measurement is in the unit of the range (so only the
///     numeric value is drawn, the unit is the range's), and the report follows the reception of the specimen.
///     Nothing in a drawn result says which flag it carries: a test about the flag pins the value and the range.
/// </summary>
public sealed partial class AnyLabResult : IAny<LabResult> {

    private IAny<AnalyteCode>    _analyte;
    private IAny<Specimen>       _specimen;
    private IAny<double>         _value;
    private IAny<ReferenceRange> _range;
    private IAny<TimeSpan>       _reportDelay;

    private AnySpecimen       _specimenRecipe;
    private AnyReferenceRange _rangeRecipe;

    private DateTimeOffset? _reportedAtPin;

    public AnyLabResult()
        : this(analyte:     new AnyAnalyteCode(),
               specimen:    new AnySpecimen(),
               range:       new AnyReferenceRange(),
               value:       Any.Double().Between(0d, 100_000d),
               reportDelay: Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(7))) { }

    private AnyLabResult(IAny<AnalyteCode> analyte, AnySpecimen specimen, AnyReferenceRange range, IAny<double> value, IAny<TimeSpan> reportDelay) {
        _analyte        = analyte;
        _specimen       = specimen;
        _specimenRecipe = specimen;
        _range          = range;
        _rangeRecipe    = range;
        _value          = value;
        _reportDelay    = reportDelay;
    }

    private AnyLabResult(AnyLabResult from) {
        _analyte        = from._analyte;
        _specimen       = from._specimen;
        _specimenRecipe = from._specimenRecipe;
        _range          = from._range;
        _rangeRecipe    = from._rangeRecipe;
        _value          = from._value;
        _reportDelay    = from._reportDelay;
        _reportedAtPin  = from._reportedAtPin;
    }

    public AnyLabResult WithAnalyte(AnalyteCode value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._analyte = Any.Fixed(value);

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

    /// <summary>Pins the numeric value; its unit is the range's.</summary>
    public AnyLabResult WithValue(double value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins the whole range; the measurement takes its unit.</summary>
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

    /// <summary>Pins the reporting instant. It must not precede the reception of the specimen drawn or pinned.</summary>
    public AnyLabResult WithReportedAt(DateTimeOffset value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._reportedAtPin = value;

        return copy;
    }

    public LabResult Generate() {
        Specimen       specimen = _specimen.Generate();
        ReferenceRange range    = _range.Generate();
        DateTimeOffset reported = _reportedAtPin ?? specimen.ReceivedAt + _reportDelay.Generate();

        return new LabResult(_analyte.Generate(), specimen, new Measurement(_value.Generate(), range.Unit), range, reported);
    }

}
