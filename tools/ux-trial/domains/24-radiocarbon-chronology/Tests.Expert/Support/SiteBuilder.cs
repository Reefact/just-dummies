using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert.Support;

/// <summary>
///     Builds a site the way an excavation report reads: layers with the ages measured on them, then the
///     successions. The codes, the ages and the order are the tests' business and are written out; what a report
///     has to carry but a test never reads (the site's name, a layer's description, a sample's laboratory code and
///     material) is drawn.
/// </summary>
internal sealed class SiteBuilder {

    private readonly SiteChronology _site;
    private readonly List<string>   _labCodesInUse = new();

    public SiteBuilder(CalibrationCurve curve) {
        _site = Any.SiteChronology().WithCurve(curve).Generate();
    }

    /// <summary>A layer dated by one terrestrial sample per given age (the age is the ± 30 years a laboratory typically reports).</summary>
    public SiteBuilder WithLayer(string code, params int[] yearsBp) {
        _site.AddLayer(Any.StratigraphicLayer().WithCode(code).Generate());
        foreach (int yearBp in yearsBp) {
            AttachTerrestrialSample(code, new RadiocarbonAge(yearBp, 30));
        }

        return this;
    }

    public SiteBuilder WithLayerDatedBy(string code, RadiocarbonAge age) {
        _site.AddLayer(Any.StratigraphicLayer().WithCode(code).Generate());
        AttachTerrestrialSample(code, age);

        return this;
    }

    /// <summary>A layer dated by a marine shell: the age is the one measured, before the reservoir correction.</summary>
    public SiteBuilder WithLayerDatedByMarineShell(string code, RadiocarbonAge measuredAge, int reservoirOffset) {
        _site.AddLayer(Any.StratigraphicLayer().WithCode(code).Generate());
        Sample shell = Any.Sample()
                          .WithLabCode(labCode => labCode.Except(_labCodesInUse))
                          .WithMaterial(SampleMaterial.MarineShell)
                          .WithMeasurement(measuredAge)
                          .WithReservoirOffset(reservoirOffset)
                          .Generate();

        _labCodesInUse.Add(shell.LabCode);
        _site.AttachSample(code, shell);

        return this;
    }

    public SiteBuilder WithLayerWithoutSample(string code) {
        _site.AddLayer(Any.StratigraphicLayer().WithCode(code).Generate());

        return this;
    }

    public SiteBuilder WithSuccession(string olderCode, string youngerCode) {
        _site.AddRelation(new PrecedenceRelation(olderCode, youngerCode));

        return this;
    }

    public SiteChronology Build() {
        return _site;
    }

    private void AttachTerrestrialSample(string layerCode, RadiocarbonAge age) {
        Sample sample = Any.Sample()
                           .WithLabCode(code => code.Except(_labCodesInUse))
                           .WithMaterial(material => material.Except(SampleMaterial.MarineShell))
                           .WithMeasurement(age)
                           .Generate();

        _labCodesInUse.Add(sample.LabCode);
        _site.AttachSample(layerCode, sample);
    }

}
