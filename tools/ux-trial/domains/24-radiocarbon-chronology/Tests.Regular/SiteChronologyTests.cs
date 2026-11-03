using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class SiteChronologyTests {

    private static SiteChronology AnyOpenSite() {
        return new SiteChronology(Any.String().NotBlank().Generate(), TestCurves.Linear());
    }

    private static StratigraphicLayer Layer(string code) {
        return Any.StratigraphicLayer().WithCode(code).Generate();
    }

    private static List<string> DistinctLabCodes(int count) {
        return Any.ListOf(Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}")).WithCount(count).Distinct().Generate();
    }

    private static Sample CharcoalAged(string labCode, int yearsBp, int uncertainty) {
        return Any.Sample()
                  .WithLabCode(labCode)
                  .WithMaterial(SampleMaterial.Charcoal)
                  .WithMeasurement(new RadiocarbonAge(yearsBp, uncertainty))
                  .Generate();
    }

    /// <summary>A layer recorded on the site with one charcoal sample of the given age.</summary>
    private static void AddDatedLayer(SiteChronology site, string code, int yearsBp, int uncertainty) {
        string labCode = DistinctLabCodes(1)[0];
        site.AddLayer(Layer(code));
        site.AttachSample(code, CharcoalAged(labCode, yearsBp, uncertainty));
    }

    [Fact]
    public void A_new_site_has_no_layer_no_relation_and_is_not_modelled() {
        // Arrange
        string           anySiteName = Any.String().NotBlank().Generate();
        CalibrationCurve curve       = TestCurves.Linear();

        // Act
        SiteChronology site = new SiteChronology(anySiteName, curve);

        // Assert
        Assert.Equal(anySiteName, site.SiteName);
        Assert.Same(curve, site.Curve);
        Assert.Empty(site.Layers);
        Assert.Empty(site.Relations);
        Assert.False(site.IsModelled);
    }

    [Fact]
    public void The_scaffolded_generator_gives_an_open_site() {
        // Arrange / Act
        SiteChronology anySite = Any.SiteChronology().Generate();

        // Assert
        Assert.False(anySite.IsModelled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_site_without_a_name_is_refused(string? name) {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentException>(() => new SiteChronology(name!, TestCurves.Linear()));
    }

    [Fact]
    public void A_site_without_a_curve_is_refused() {
        // Arrange
        string anySiteName = Any.String().NotBlank().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SiteChronology(anySiteName, null!));
    }

    [Fact]
    public void A_layer_is_recorded_on_the_site() {
        // Arrange
        SiteChronology     site = AnyOpenSite();
        StratigraphicLayer anyLayer = Layer("US1");

        // Act
        site.AddLayer(anyLayer);

        // Assert
        Assert.Same(anyLayer, Assert.Single(site.Layers));
    }

    [Fact]
    public void A_layer_code_already_on_the_site_is_refused() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => site.AddLayer(Layer("US1")));

        // Assert
        Assert.Contains("US1", error.Message);
        Assert.Single(site.Layers);
    }

    [Fact]
    public void Adding_no_layer_is_refused() {
        // Arrange
        SiteChronology site = AnyOpenSite();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => site.AddLayer(null!));
    }

    [Fact]
    public void A_sample_is_attached_to_its_layer() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        Sample anySample = Any.Sample().Generate();

        // Act
        site.AttachSample("US1", anySample);

        // Assert
        Assert.Same(anySample, Assert.Single(site.Layers[0].Samples));
    }

    [Fact]
    public void A_sample_cannot_be_attached_to_an_unknown_layer() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        Sample anySample = Any.Sample().Generate();

        // Act
        KeyNotFoundException error = Assert.Throws<KeyNotFoundException>(() => site.AttachSample("US2", anySample));

        // Assert
        Assert.Contains("US2", error.Message);
    }

    [Fact]
    public void A_laboratory_code_cannot_be_attached_twice_to_the_same_layer() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        string anyLabCode = DistinctLabCodes(1)[0];
        site.AttachSample("US1", Any.Sample().WithLabCode(anyLabCode).Generate());

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => site.AttachSample("US1", Any.Sample().WithLabCode(anyLabCode).Generate()));

        // Assert
        Assert.Contains(anyLabCode, error.Message);
    }

    [Fact]
    public void A_laboratory_code_cannot_be_reused_in_another_layer_of_the_site() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        site.AddLayer(Layer("US2"));
        string anyLabCode = DistinctLabCodes(1)[0];
        site.AttachSample("US1", Any.Sample().WithLabCode(anyLabCode).Generate());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => site.AttachSample("US2", Any.Sample().WithLabCode(anyLabCode).Generate()));
        Assert.Empty(site.Layers[1].Samples);
    }

    [Fact]
    public void Attaching_no_sample_is_refused() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => site.AttachSample("US1", null!));
    }

    [Fact]
    public void A_succession_between_two_known_layers_is_recorded() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        site.AddLayer(Layer("US2"));
        PrecedenceRelation relation = new PrecedenceRelation("US1", "US2");

        // Act
        site.AddRelation(relation);

        // Assert
        Assert.Same(relation, Assert.Single(site.Relations));
    }

    [Fact]
    public void A_succession_naming_an_unknown_older_layer_is_refused() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US2"));

        // Act
        KeyNotFoundException error = Assert.Throws<KeyNotFoundException>(() => site.AddRelation(new PrecedenceRelation("US1", "US2")));

        // Assert
        Assert.Contains("US1", error.Message);
    }

    [Fact]
    public void A_succession_naming_an_unknown_younger_layer_is_refused() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));

        // Act
        KeyNotFoundException error = Assert.Throws<KeyNotFoundException>(() => site.AddRelation(new PrecedenceRelation("US1", "US2")));

        // Assert
        Assert.Contains("US2", error.Message);
    }

    [Fact]
    public void The_same_succession_cannot_be_recorded_twice() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        site.AddLayer(Layer("US2"));
        site.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => site.AddRelation(new PrecedenceRelation("US1", "US2")));
        Assert.Single(site.Relations);
    }

    [Fact]
    public void Adding_no_succession_is_refused() {
        // Arrange
        SiteChronology site = AnyOpenSite();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => site.AddRelation(null!));
    }

    [Fact]
    public void The_opposite_of_a_recorded_succession_closes_a_cycle() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        site.AddLayer(Layer("US2"));
        site.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        StratigraphicInconsistencyException error = Assert.Throws<StratigraphicInconsistencyException>(() => site.AddRelation(new PrecedenceRelation("US2", "US1")));

        // Assert
        Assert.Equal("US2", error.OlderLayerCode);
        Assert.Equal("US1", error.YoungerLayerCode);
        Assert.Single(site.Relations);
    }

    [Fact]
    public void A_cycle_through_a_third_layer_is_refused_too() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        site.AddLayer(Layer("US2"));
        site.AddLayer(Layer("US3"));
        site.AddRelation(new PrecedenceRelation("US1", "US2"));
        site.AddRelation(new PrecedenceRelation("US2", "US3"));

        // Act
        StratigraphicInconsistencyException error = Assert.Throws<StratigraphicInconsistencyException>(() => site.AddRelation(new PrecedenceRelation("US3", "US1")));

        // Assert
        Assert.Contains("US3", error.Message);
        Assert.Contains("US1", error.Message);
        Assert.Equal(2, site.Relations.Count);
    }

    [Fact]
    public void Two_different_paths_to_the_same_layer_do_not_make_a_cycle() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        site.AddLayer(Layer("US2"));
        site.AddLayer(Layer("US3"));
        site.AddLayer(Layer("US4"));
        site.AddRelation(new PrecedenceRelation("US1", "US2"));
        site.AddRelation(new PrecedenceRelation("US1", "US3"));
        site.AddRelation(new PrecedenceRelation("US2", "US4"));

        // Act
        site.AddRelation(new PrecedenceRelation("US3", "US4"));

        // Assert
        Assert.Equal(4, site.Relations.Count);
    }

    [Fact]
    public void A_site_with_no_layer_cannot_be_modelled() {
        // Arrange
        SiteChronology site = AnyOpenSite();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => site.Model());
    }

    [Fact]
    public void A_layer_without_a_dated_sample_cannot_be_modelled() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);
        site.AddLayer(Layer("US2"));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => site.Model());

        // Assert
        Assert.Contains("US2", error.Message);
        Assert.False(site.IsModelled);
    }

    [Fact]
    public void One_dated_layer_gives_one_estimate_around_its_calendar_year() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        OccupationEstimate estimate = Assert.Single(estimates);
        Assert.Equal("US1", estimate.LayerCode);
        Assert.True(estimate.Envelope.Contains(-1050));
        Assert.Equal(-1050d, estimate.Date.Mean, 0);
        Assert.False(estimate.IsMultimodal);
        Assert.True(site.IsModelled);
    }

    [Fact]
    public void A_precise_measurement_gives_a_narrow_plausible_interval() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);

        // Act
        OccupationEstimate estimate = Assert.Single(site.Model());

        // Assert: 95.4% is about two standard deviations on each side of sqrt(30^2 + 10^2) = 32 years
        Assert.InRange(estimate.Envelope.LengthInYears, 100, 140);
    }

    [Fact]
    public void The_estimates_are_the_ones_returned_by_the_modelling() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);

        // Act
        IReadOnlyList<OccupationEstimate> returned = site.Model();

        // Assert
        Assert.Equal(returned, site.Estimates);
    }

    [Fact]
    public void The_estimates_are_refused_before_the_modelling() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => site.Estimates);
    }

    [Fact]
    public void Estimates_come_in_stratigraphic_order_whatever_the_order_the_layers_were_recorded_in() {
        // Arrange: the layers are recorded from the top down while the succession reads from the bottom up
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US3", 2000, 30);
        AddDatedLayer(site, "US2", 3000, 30);
        AddDatedLayer(site, "US1", 4000, 30);
        site.AddRelation(new PrecedenceRelation("US1", "US2"));
        site.AddRelation(new PrecedenceRelation("US2", "US3"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(new[] { "US1", "US2", "US3" }, estimates.Select(estimate => estimate.LayerCode));
    }

    [Fact]
    public void Layers_without_any_succession_are_all_estimated() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 2000, 30);
        AddDatedLayer(site, "US2", 3000, 30);
        AddDatedLayer(site, "US3", 4000, 30);

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(new[] { "US1", "US2", "US3" }, estimates.Select(estimate => estimate.LayerCode).OrderBy(code => code));
    }

    [Fact]
    public void Consistent_layers_keep_the_dates_their_samples_gave() {
        // Arrange: the older layer is clearly older than the younger one
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 4000, 30);
        AddDatedLayer(site, "US2", 2000, 30);
        site.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(-2050d, estimates[0].Date.Mean, 0);
        Assert.Equal(-50d, estimates[1].Date.Mean, 0);
    }

    [Fact]
    public void The_succession_trims_the_younger_layer_to_start_no_earlier_than_the_older_one() {
        // Arrange: the layer that lies beneath is the younger one by its sample, so the dates overlap and contradict a little
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 2900, 100);
        AddDatedLayer(site, "US2", 3000, 100);
        site.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert: the 95.4% range of US1 alone begins about 200 years before -950
        CalibratedDate younger = estimates[1].Date;
        Assert.Equal(0d, younger.ProbabilityOf(-1160));
        Assert.True(younger.ProbabilityOf(-1100) > 0d);
    }

    [Fact]
    public void The_succession_trims_the_older_layer_to_end_no_later_than_the_younger_one() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 2900, 100);
        AddDatedLayer(site, "US2", 3000, 100);
        site.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert: US2 alone would end about -850; US1 alone would reach -750
        CalibratedDate older = estimates[0].Date;
        Assert.Equal(0d, older.ProbabilityOf(-840));
        Assert.True(older.ProbabilityOf(-950) > 0d);
    }

    [Fact]
    public void A_trimmed_layer_still_has_a_probability_of_one() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 2900, 100);
        AddDatedLayer(site, "US2", 3000, 100);
        site.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.All(estimates, estimate => Assert.Equal(1d, estimate.Date.Probabilities.Sum(), 6));
    }

    [Fact]
    public void Dates_that_contradict_the_succession_are_refused_naming_both_layers() {
        // Arrange: the layer beneath is dated three thousand years after the one above it
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 1000, 30);
        AddDatedLayer(site, "US2", 4000, 30);
        site.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        StratigraphicInconsistencyException error = Assert.Throws<StratigraphicInconsistencyException>(() => site.Model());

        // Assert
        Assert.Equal("US1", error.OlderLayerCode);
        Assert.Equal("US2", error.YoungerLayerCode);
        Assert.Contains("US1", error.Message);
        Assert.Contains("US2", error.Message);
    }

    [Fact]
    public void A_failed_modelling_leaves_the_site_open() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 1000, 30);
        AddDatedLayer(site, "US2", 4000, 30);
        site.AddRelation(new PrecedenceRelation("US1", "US2"));
        Assert.Throws<StratigraphicInconsistencyException>(() => site.Model());

        // Act
        site.AddLayer(Layer("US3"));

        // Assert
        Assert.False(site.IsModelled);
        Assert.Equal(3, site.Layers.Count);
    }

    [Fact]
    public void A_sample_older_than_the_curve_can_reach_fails_the_modelling() {
        // Arrange: the linear test curve stops at about 11950 BP
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        site.AttachSample("US1", CharcoalAged(DistinctLabCodes(1)[0], 50_000, 30));

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => site.Model());
        Assert.False(site.IsModelled);
    }

    [Fact]
    public void Two_samples_far_apart_in_one_layer_make_it_multimodal() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        List<string>   labCodes = DistinctLabCodes(2);
        site.AddLayer(Layer("US1"));
        site.AttachSample("US1", CharcoalAged(labCodes[0], 2000, 30));
        site.AttachSample("US1", CharcoalAged(labCodes[1], 4000, 30));

        // Act
        OccupationEstimate estimate = Assert.Single(site.Model());

        // Assert
        Assert.True(estimate.IsMultimodal);
        Assert.Equal(2, estimate.PlausibleIntervals.Count);
        Assert.True(estimate.Envelope.Contains(-2050));
        Assert.True(estimate.Envelope.Contains(-50));
    }

    [Fact]
    public void Two_close_samples_in_one_layer_give_a_single_interval() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        List<string>   labCodes = DistinctLabCodes(2);
        site.AddLayer(Layer("US1"));
        site.AttachSample("US1", CharcoalAged(labCodes[0], 3000, 50));
        site.AttachSample("US1", CharcoalAged(labCodes[1], 3040, 50));

        // Act
        OccupationEstimate estimate = Assert.Single(site.Model());

        // Assert
        Assert.False(estimate.IsMultimodal);
    }

    [Fact]
    public void A_marine_sample_is_dated_on_its_corrected_age() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        site.AddLayer(Layer("US1"));
        Sample shell = Any.Sample()
                          .WithLabCode(DistinctLabCodes(1)[0])
                          .WithMaterial(SampleMaterial.MarineShell)
                          .WithMeasurement(new RadiocarbonAge(3400, 30))
                          .WithReservoirOffset(400)
                          .Generate();
        site.AttachSample("US1", shell);

        // Act
        OccupationEstimate estimate = Assert.Single(site.Model());

        // Assert
        Assert.Equal(-1050d, estimate.Date.Mean, 0);
    }

    [Fact]
    public void A_modelled_site_refuses_a_new_layer() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);
        site.Model();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => site.AddLayer(Layer("US2")));

        // Assert
        Assert.Contains("reopen", error.Message);
    }

    [Fact]
    public void A_modelled_site_refuses_a_new_sample() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);
        site.Model();
        Sample anySample = Any.Sample().Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => site.AttachSample("US1", anySample));
    }

    [Fact]
    public void A_modelled_site_refuses_a_new_succession() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 4000, 30);
        AddDatedLayer(site, "US2", 3000, 30);
        site.Model();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => site.AddRelation(new PrecedenceRelation("US1", "US2")));
    }

    [Fact]
    public void A_modelled_site_cannot_be_modelled_again() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);
        site.Model();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => site.Model());
    }

    [Fact]
    public void Reopening_allows_the_site_to_be_edited_and_discards_the_estimates() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);
        site.Model();

        // Act
        site.Reopen();
        site.AddLayer(Layer("US2"));

        // Assert
        Assert.False(site.IsModelled);
        Assert.Throws<InvalidOperationException>(() => site.Estimates);
    }

    [Fact]
    public void A_reopened_site_can_be_modelled_again_with_what_was_added() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);
        site.Model();
        site.Reopen();
        AddDatedLayer(site, "US2", 2000, 30);

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(2, estimates.Count);
    }

    [Fact]
    public void Reopening_a_site_that_was_never_modelled_changes_nothing() {
        // Arrange
        SiteChronology site = AnyOpenSite();
        AddDatedLayer(site, "US1", 3000, 30);

        // Act
        site.Reopen();

        // Assert
        Assert.False(site.IsModelled);
        Assert.Single(site.Layers);
    }

}
