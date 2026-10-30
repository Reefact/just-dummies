// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LabResult --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Laboratory.Tests;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LabResult" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLabResult : IAny<LabResult> {

    private readonly IAny<AnalyteCode>    _analyte;
    private readonly IAny<Specimen>       _specimen;
    private readonly IAny<Measurement>    _measurement;
    private readonly IAny<ReferenceRange> _range;
    private readonly IAny<DateTimeOffset> _reportedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLabResult()
        : this(analyte:     AnyValidAnalyte(),
               specimen:    AnyValidSpecimen(),
               measurement: AnyValidMeasurement(),
               range:       AnyValidRange(),
               reportedAt:  AnyValidReportedAt()) { }

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

    private static AnyDateTimeOffset AnyValidReportedAt() {
        // Reported after any specimen AnySpecimen can draw was received.
        return Any.DateTimeOffset().Between(LaboratoryCalendar.ReceptionEnd, LaboratoryCalendar.ReportingEnd);
    }

    private AnyLabResult(IAny<AnalyteCode>    analyte,
                         IAny<Specimen>       specimen,
                         IAny<Measurement>    measurement,
                         IAny<ReferenceRange> range,
                         IAny<DateTimeOffset> reportedAt) {
        _analyte     = analyte;
        _specimen    = specimen;
        _measurement = measurement;
        _range       = range;
        _reportedAt  = reportedAt;
    }

    /// <summary>Pins <c>analyte</c> to a fixed value.</summary>
    public AnyLabResult WithAnalyte(AnalyteCode value) {
        return new AnyLabResult(new FixedValue<AnalyteCode>(value), _specimen, _measurement, _range, _reportedAt);
    }

    /// <summary>Draws <c>analyte</c> from <c>AnyValidAnalyte()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLabResult WithAnalyte(Func<AnyAnalyteCode, AnyAnalyteCode> configure) {
        return new AnyLabResult(configure(AnyValidAnalyte()), _specimen, _measurement, _range, _reportedAt);
    }

    /// <summary>Pins <c>specimen</c> to a fixed value.</summary>
    public AnyLabResult WithSpecimen(Specimen value) {
        return new AnyLabResult(_analyte, new FixedValue<Specimen>(value), _measurement, _range, _reportedAt);
    }

    /// <summary>Draws <c>specimen</c> from <c>AnyValidSpecimen()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLabResult WithSpecimen(Func<AnySpecimen, AnySpecimen> configure) {
        return new AnyLabResult(_analyte, configure(AnyValidSpecimen()), _measurement, _range, _reportedAt);
    }

    /// <summary>Pins <c>measurement</c> to a fixed value.</summary>
    public AnyLabResult WithMeasurement(Measurement value) {
        return new AnyLabResult(_analyte, _specimen, new FixedValue<Measurement>(value), _range, _reportedAt);
    }

    /// <summary>Draws <c>measurement</c> from <c>AnyValidMeasurement()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLabResult WithMeasurement(Func<AnyMeasurement, AnyMeasurement> configure) {
        return new AnyLabResult(_analyte, _specimen, configure(AnyValidMeasurement()), _range, _reportedAt);
    }

    /// <summary>Pins <c>range</c> to a fixed value.</summary>
    public AnyLabResult WithRange(ReferenceRange value) {
        return new AnyLabResult(_analyte, _specimen, _measurement, new FixedValue<ReferenceRange>(value), _reportedAt);
    }

    /// <summary>Draws <c>range</c> from <c>AnyValidRange()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLabResult WithRange(Func<AnyReferenceRange, AnyReferenceRange> configure) {
        return new AnyLabResult(_analyte, _specimen, _measurement, configure(AnyValidRange()), _reportedAt);
    }

    /// <summary>Pins <c>reportedAt</c> to a fixed value.</summary>
    public AnyLabResult WithReportedAt(DateTimeOffset value) {
        return new AnyLabResult(_analyte, _specimen, _measurement, _range, new FixedValue<DateTimeOffset>(value));
    }

    /// <summary>Draws <c>reportedAt</c> from <c>AnyValidReportedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLabResult WithReportedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyLabResult(_analyte, _specimen, _measurement, _range, configure(AnyValidReportedAt()));
    }

    /// <summary>Produces one arbitrary <see cref="LabResult" />.</summary>
    public LabResult Generate() {
        // Hand edit: the measurement must be in the range's unit, a rule across two parameters that no
        // AnyValid…() factory can state. The range is drawn first and lends its unit to the measurement.
        AnalyteCode    analyte     = _analyte.Generate();
        Specimen       specimen    = _specimen.Generate();
        Measurement    drawn       = _measurement.Generate();
        ReferenceRange range       = _range.Generate();
        Measurement    measurement = drawn.Unit.Equals(range.Unit) ? drawn : new Measurement(drawn.Value, range.Unit);

        return new LabResult(analyte, specimen, measurement, range, _reportedAt.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
