// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LabReport --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using Laboratory.Tests;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LabReport" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLabReport : IAny<LabReport> {

    private readonly IAny<SampleId>                 _sampleId;
    private readonly IAny<IReadOnlyList<LabResult>> _results;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLabReport()
        : this(sampleId: AnyValidSampleId(),
               results:  AnyValidResults()) { }

    private static AnySampleId AnyValidSampleId() {
        return new AnySampleId();
    }

    private static AnyList<LabResult> AnyValidResults() {
        // One result per analyte: distinct by analyte code, not by reference.
        return Any.ListOf(new AnyLabResult()).WithCountBetween(1, 8).Distinct(LabResultByAnalyte.Instance);
    }

    private AnyLabReport(IAny<SampleId>                 sampleId,
                         IAny<IReadOnlyList<LabResult>> results) {
        _sampleId = sampleId;
        _results  = results;
    }

    /// <summary>Pins <c>sampleId</c> to a fixed value.</summary>
    public AnyLabReport WithSampleId(SampleId value) {
        return new AnyLabReport(new FixedValue<SampleId>(value), _results);
    }

    /// <summary>Draws <c>sampleId</c> from <c>AnyValidSampleId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLabReport WithSampleId(Func<AnySampleId, AnySampleId> configure) {
        return new AnyLabReport(configure(AnyValidSampleId()), _results);
    }

    /// <summary>Pins <c>results</c> to a fixed value.</summary>
    public AnyLabReport WithResults(IReadOnlyList<LabResult> value) {
        return new AnyLabReport(_sampleId, new FixedValue<IReadOnlyList<LabResult>>(value));
    }

    /// <summary>Draws <c>results</c> from <c>AnyValidResults()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLabReport WithResults(Func<AnyList<LabResult>, AnyList<LabResult>> configure) {
        return new AnyLabReport(_sampleId, configure(AnyValidResults()));
    }

    /// <summary>Produces one arbitrary <see cref="LabReport" />.</summary>
    public LabReport Generate() {
        // Hand edit: every result of a report comes from the report's specimen, a rule across the two
        // parameters. Each drawn result is re-homed on one specimen carrying the drawn sample id.
        SampleId sampleId = _sampleId.Generate();
        Specimen specimen = new AnySpecimen().WithId(sampleId).Generate();
        List<LabResult> results = _results.Generate()
                                          .Select(result => new LabResult(result.Analyte, specimen, result.Measurement, result.Range, result.ReportedAt))
                                          .ToList();

        return new LabReport(sampleId, results);
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
