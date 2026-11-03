// Scaffolded by `any` (JustDummies), then completed by hand. This file is yours: read it, edit it, commit it.
// A report ties its results to its sample id and requires distinct analytes; the scaffold drew the two
// independently. Here the results are built one by one, each on the report's sample id, each with its own analyte.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LabReport" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLabReport : IAny<LabReport> {

    private IAny<SampleId> _sampleId;
    private IAny<int>      _count;

    private AnySampleId   _sampleIdRecipe;
    private AnyInt32      _countRecipe;
    private AnyLabResult  _resultRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLabReport()
        : this(sampleId: AnyValidSampleId(),
               count:    AnyValidCount(),
               result:   AnyValidResult()) { }

    private static AnySampleId AnyValidSampleId() {
        return new AnySampleId();
    }

    private static AnyInt32 AnyValidCount() {
        return Any.Int32().Between(1, 6);
    }

    private static AnyLabResult AnyValidResult() {
        return new AnyLabResult();
    }

    private AnyLabReport(AnySampleId  sampleId,
                         AnyInt32     count,
                         AnyLabResult result) {
        _sampleId       = sampleId;
        _sampleIdRecipe = sampleId;
        _count          = count;
        _countRecipe    = count;
        _resultRecipe   = result;
    }

    private AnyLabReport(AnyLabReport from) {
        _sampleId       = from._sampleId;
        _sampleIdRecipe = from._sampleIdRecipe;
        _count          = from._count;
        _countRecipe    = from._countRecipe;
        _resultRecipe   = from._resultRecipe;
    }

    public AnyLabReport WithSampleId(SampleId value) {
        AnyLabReport copy = new AnyLabReport(this);

        copy._sampleId = Any.Fixed(value);

        return copy;
    }

    public AnyLabReport WithSampleId(Func<AnySampleId, AnySampleId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabReport copy = new AnyLabReport(this);

        copy._sampleIdRecipe = configure(_sampleIdRecipe);
        copy._sampleId       = copy._sampleIdRecipe;

        return copy;
    }

    /// <summary>Pins how many results the report holds.</summary>
    public AnyLabReport WithResultCount(int value) {
        AnyLabReport copy = new AnyLabReport(this);

        copy._count = Any.Fixed(value);

        return copy;
    }

    public AnyLabReport WithResultCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabReport copy = new AnyLabReport(this);

        copy._countRecipe = configure(_countRecipe);
        copy._count       = copy._countRecipe;

        return copy;
    }

    /// <summary>Refines every result of the report; the sample id and the distinct analytes are the report's.</summary>
    public AnyLabReport WithEachResult(Func<AnyLabResult, AnyLabResult> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLabReport copy = new AnyLabReport(this);

        copy._resultRecipe = configure(_resultRecipe);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LabReport" />.</summary>
    public LabReport Generate() {
        SampleId id    = _sampleId.Generate();
        int      count = _count.Generate();

        HashSet<AnalyteCode> analytes = Any.SetOf(new AnyAnalyteCode()).WithCount(count).Generate();

        List<LabResult> results = analytes
            .Select(analyte => _resultRecipe.WithAnalyte(analyte)
                                            .WithSpecimen(specimen => specimen.WithId(id))
                                            .Generate())
            .ToList();

        return new LabReport(id, results);
    }

}
