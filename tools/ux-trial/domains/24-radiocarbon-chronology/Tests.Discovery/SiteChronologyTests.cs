using JustDummies;
using JustDummies.Xunit;

using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Discovery;

[Reproducible]
public sealed class SiteChronologyTests {

    private static readonly IEqualityComparer<StratigraphicLayer> SameCode =
        EqualityComparer<StratigraphicLayer>.Create((a, b) => a!.Code == b!.Code, l => l.Code.GetHashCode());

    private static readonly IEqualityComparer<Sample> SameLabCode =
        EqualityComparer<Sample>.Create((a, b) => a!.LabCode == b!.LabCode, s => s.LabCode.GetHashCode());

    /// <summary>Two layers with different codes: a succession needs two.</summary>
    private static List<StratigraphicLayer> AnyTwoLayers() {
        return Any.ListOf(Any.StratigraphicLayer()).WithCount(2).Distinct(SameCode).Generate();
    }

    /// <summary>A terrestrial sample measured at the given age, everything else arbitrary.</summary>
    private static AnySample AnyTerrestrialSampleAt(int yearsBp, int uncertainty) {
        return Any.Sample()
                  .WithMaterial(m => m.DifferentFrom(SampleMaterial.MarineShell))
                  .WithMeasurement(new RadiocarbonAge(yearsBp, uncertainty));
    }

    private static SiteChronology AnySiteOnTheLinearCurve() {
        return Any.SiteChronology().WithCurve(c => c.WithPoints(Curves.Linear())).Generate();
    }

    [Fact]
    public void A_layer_code_is_recorded_only_once_on_a_site() {
        // Arrange
        SiteChronology     anySite  = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();
        StratigraphicLayer anyHomonym = Any.StratigraphicLayer().WithCode(anyLayer.Code).Generate();
        anySite.AddLayer(anyLayer);

        // Act
        Action adding = () => anySite.AddLayer(anyHomonym);

        // Assert
        Assert.Throws<InvalidOperationException>(adding);
    }

    [Fact]
    public void A_sample_cannot_be_attached_to_a_layer_the_site_does_not_record() {
        // Arrange
        SiteChronology anySite   = Any.SiteChronology().Generate();
        Sample         anySample = Any.Sample().Generate();

        // Act
        Action attaching = () => anySite.AttachSample("US404", anySample);

        // Assert
        Assert.Throws<KeyNotFoundException>(attaching);
    }

    [Fact]
    public void A_lab_code_already_attached_to_another_layer_is_refused() {
        // Arrange
        SiteChronology           anySite   = Any.SiteChronology().Generate();
        List<StratigraphicLayer> anyLayers = AnyTwoLayers();
        Sample                   anySample = Any.Sample().Generate();
        Sample                   anyRemeasurement = Any.Sample().WithLabCode(anySample.LabCode).Generate();
        anySite.AddLayer(anyLayers[0]);
        anySite.AddLayer(anyLayers[1]);
        anySite.AttachSample(anyLayers[0].Code, anySample);

        // Act
        Action attaching = () => anySite.AttachSample(anyLayers[1].Code, anyRemeasurement);

        // Assert
        Assert.Throws<InvalidOperationException>(attaching);
    }

    [Fact]
    public void A_succession_that_would_close_a_cycle_is_refused_naming_both_layers() {
        // Arrange
        SiteChronology           anySite   = Any.SiteChronology().Generate();
        List<StratigraphicLayer> anyLayers = AnyTwoLayers();
        string lower = anyLayers[0].Code;
        string upper = anyLayers[1].Code;
        anySite.AddLayer(anyLayers[0]);
        anySite.AddLayer(anyLayers[1]);
        anySite.AddRelation(new PrecedenceRelation(lower, upper));

        // Act
        StratigraphicInconsistencyException refusal =
            Assert.Throws<StratigraphicInconsistencyException>(() => anySite.AddRelation(new PrecedenceRelation(upper, lower)));

        // Assert
        Assert.Equal((upper, lower), (refusal.OlderLayerCode, refusal.YoungerLayerCode));
    }

    [Fact]
    public void A_site_with_an_undated_layer_cannot_be_modelled() {
        // Arrange
        SiteChronology     anySite  = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();
        anySite.AddLayer(anyLayer);

        // Act
        Action modelling = () => anySite.Model();

        // Assert
        Assert.Throws<InvalidOperationException>(modelling);
    }

    [Fact]
    public void Estimates_are_returned_from_the_deepest_layer_up() {
        // Arrange
        SiteChronology     site  = AnySiteOnTheLinearCurve();
        StratigraphicLayer upper = Any.StratigraphicLayer().WithCode("US1").Generate();
        StratigraphicLayer lower = Any.StratigraphicLayer().WithCode("US2").Generate();
        site.AddLayer(upper);
        site.AddLayer(lower);
        site.AttachSample("US1", AnyTerrestrialSampleAt(2000, 30).Generate());
        site.AttachSample("US2", AnyTerrestrialSampleAt(2500, 30).Generate());
        site.AddRelation(new PrecedenceRelation("US2", "US1"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(["US2", "US1"], estimates.Select(e => e.LayerCode));
    }

    [Fact]
    public void A_layer_cannot_start_before_the_plausible_start_of_the_layer_beneath_it() {
        // Arrange
        // Beneath: 3000 ± 50 BP, about 1050 BCE. Above: 3100 ± 150 BP, wide enough to reach well before it.
        SiteChronology     site  = AnySiteOnTheLinearCurve();
        StratigraphicLayer lower = Any.StratigraphicLayer().WithCode("US2").Generate();
        StratigraphicLayer upper = Any.StratigraphicLayer().WithCode("US1").Generate();
        site.AddLayer(lower);
        site.AddLayer(upper);
        site.AttachSample("US2", AnyTerrestrialSampleAt(3000, 50).Generate());
        site.AttachSample("US1", AnyTerrestrialSampleAt(3100, 150).Generate());
        site.AddRelation(new PrecedenceRelation("US2", "US1"));
        int lowerPlausibleStart = site.Curve.Calibrate(new RadiocarbonAge(3000, 50)).HighestDensityRegion(OccupationEstimate.CredibleLevel)[0].StartYear;

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        OccupationEstimate upperEstimate = estimates.Single(e => e.LayerCode == "US1");
        Assert.Equal(0.0, upperEstimate.Date.ProbabilityOf(lowerPlausibleStart - 1));
        Assert.True(upperEstimate.Date.ProbabilityOf(lowerPlausibleStart) > 0);
    }

    [Fact]
    public void Dates_that_contradict_the_succession_are_refused_naming_both_layers() {
        // Arrange
        // Beneath dated about 950 CE, above dated about 1050 BCE: the upper layer would be two thousand years older.
        SiteChronology     site  = AnySiteOnTheLinearCurve();
        StratigraphicLayer lower = Any.StratigraphicLayer().WithCode("US2").Generate();
        StratigraphicLayer upper = Any.StratigraphicLayer().WithCode("US1").Generate();
        site.AddLayer(lower);
        site.AddLayer(upper);
        site.AttachSample("US2", AnyTerrestrialSampleAt(1000, 30).Generate());
        site.AttachSample("US1", AnyTerrestrialSampleAt(3000, 30).Generate());
        site.AddRelation(new PrecedenceRelation("US2", "US1"));

        // Act
        StratigraphicInconsistencyException refusal = Assert.Throws<StratigraphicInconsistencyException>(() => site.Model());

        // Assert
        Assert.Equal(("US2", "US1"), (refusal.OlderLayerCode, refusal.YoungerLayerCode));
    }

    [Fact]
    public void A_layer_dated_by_two_distant_samples_has_a_multimodal_occupation() {
        // Arrange
        SiteChronology     site     = AnySiteOnTheLinearCurve();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();
        List<Sample>       samples  = Any.ListOf(Any.Sample()).WithCount(2).Distinct(SameLabCode).Generate();
        site.AddLayer(anyLayer);
        site.AttachSample(anyLayer.Code, AnyTerrestrialSampleAt(2000, 20).WithLabCode(samples[0].LabCode).Generate());
        site.AttachSample(anyLayer.Code, AnyTerrestrialSampleAt(3000, 20).WithLabCode(samples[1].LabCode).Generate());

        // Act
        OccupationEstimate estimate = site.Model().Single();

        // Assert
        Assert.Equal(2, estimate.PlausibleIntervals.Count);
    }

    [Fact]
    public void A_modelled_chronology_refuses_changes() {
        // Arrange
        SiteChronology     site     = AnySiteOnTheLinearCurve();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();
        StratigraphicLayer anyOtherLayer = Any.StratigraphicLayer().WithCode(c => c.DifferentFrom(anyLayer.Code)).Generate();
        site.AddLayer(anyLayer);
        site.AttachSample(anyLayer.Code, AnyTerrestrialSampleAt(2000, 30).Generate());
        site.Model();

        // Act
        Action adding = () => site.AddLayer(anyOtherLayer);

        // Assert
        Assert.Throws<InvalidOperationException>(adding);
    }

    [Fact]
    public void A_reopened_chronology_accepts_changes_and_forgets_its_estimates() {
        // Arrange
        SiteChronology     site     = AnySiteOnTheLinearCurve();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();
        StratigraphicLayer anyOtherLayer = Any.StratigraphicLayer().WithCode(c => c.DifferentFrom(anyLayer.Code)).Generate();
        site.AddLayer(anyLayer);
        site.AttachSample(anyLayer.Code, AnyTerrestrialSampleAt(2000, 30).Generate());
        site.Model();

        // Act
        site.Reopen();
        site.AddLayer(anyOtherLayer);

        // Assert
        Assert.False(site.IsModelled);
        Assert.Equal(2, site.Layers.Count);
    }

    [Fact]
    public void Estimates_are_refused_before_the_chronology_is_modelled() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();

        // Act
        Func<IReadOnlyList<OccupationEstimate>> reading = () => anySite.Estimates;

        // Assert
        Assert.Throws<InvalidOperationException>(reading);
    }

}
