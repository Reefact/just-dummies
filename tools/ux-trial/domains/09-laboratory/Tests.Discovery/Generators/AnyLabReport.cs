// Scaffolded by `any` (JustDummies), then completed by hand: every result is drawn for the report's own sample id,
// and the analytes are drawn distinct (the two guards `any` could not read).

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace Laboratory.Domain;

public sealed partial class AnyLabReport : IAny<LabReport> {

    private IAny<SampleId> _sampleId;

    private AnySampleId _sampleIdRecipe;
    private AnyLabResult _resultRecipe;
    private AnyInt32     _countRecipe;

    public AnyLabReport()
        : this(new AnySampleId(), new AnyLabResult(), Any.Int32().Between(1, 6)) { }

    private AnyLabReport(AnySampleId sampleId, AnyLabResult result, AnyInt32 count) {
        _sampleId       = sampleId;
        _sampleIdRecipe = sampleId;
        _resultRecipe   = result;
        _countRecipe    = count;
    }

    private AnyLabReport(AnyLabReport from) {
        _sampleId       = from._sampleId;
        _sampleIdRecipe = from._sampleIdRecipe;
        _resultRecipe   = from._resultRecipe;
        _countRecipe    = from._countRecipe;
    }

    public AnyLabReport WithSampleId(SampleId value) {
        AnyLabReport copy = new AnyLabReport(this);

        copy._sampleId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines every result of the report; the sample id and the analyte stay the report's business.</summary>
    public AnyLabReport WithResults(Func<AnyLabResult, AnyLabResult> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabReport copy = new AnyLabReport(this);

        copy._resultRecipe = configure(_resultRecipe);

        return copy;
    }

    public AnyLabReport WithResultCount(int count) {
        AnyLabReport copy = new AnyLabReport(this);

        copy._countRecipe = Any.Int32().Between(count, count);

        return copy;
    }

    public LabReport Generate() {
        SampleId sampleId = _sampleId.Generate();

        List<AnalyteCode> analytes = Any.ListOf(new AnyAnalyteCode()).WithCount(_countRecipe.Generate()).Distinct().Generate();

        List<LabResult> results = analytes.Select(analyte => _resultRecipe.WithAnalyte(analyte)
                                                                          .WithSpecimen(specimen => specimen.WithId(sampleId))
                                                                          .Generate())
                                          .ToList();

        return new LabReport(sampleId, results);
    }

}
