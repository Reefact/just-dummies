using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LabReport" /> values. Started from the scaffold, which could not tie the
///     results to the report's sample id nor keep their analytes distinct: here one specimen carries the drawn
///     (or pinned) sample id, and every result is a result of that specimen for a distinct analyte.
/// </summary>
public sealed partial class AnyLabReport : IAny<LabReport> {

    private IAny<SampleId> _sampleId;
    private IAny<int>      _count;

    public AnyLabReport()
        : this(sampleId: new AnySampleId(), count: Any.Int32().Between(1, 5)) { }

    private AnyLabReport(IAny<SampleId> sampleId, IAny<int> count) {
        _sampleId = sampleId;
        _count    = count;
    }

    public AnyLabReport WithSampleId(SampleId value) {
        return new AnyLabReport(Any.Fixed(value), _count);
    }

    public AnyLabReport WithCount(int value) {
        return new AnyLabReport(_sampleId, Any.Fixed(value));
    }

    public LabReport Generate() {
        SampleId sampleId = _sampleId.Generate();
        Specimen specimen = new AnySpecimen().WithId(sampleId).Generate();

        List<AnalyteCode> analytes = Any.ListOf(new AnyAnalyteCode()).WithCount(_count.Generate()).Distinct().Generate();
        List<LabResult>   results  = analytes.Select(analyte => new AnyLabResult().WithAnalyte(analyte).WithSpecimen(specimen).Generate()).ToList();

        return new LabReport(sampleId, results);
    }

}
