using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class SiteChronologyTests {

    private const int OneYear = 1;

    // ---------------------------------------------------------------- construction

    [Fact]
    public void A_new_site_holds_no_layer_no_succession_and_is_not_modelled() {
        // Act
        SiteChronology site = Any.SiteChronology().Generate();

        // Assert
        Assert.Empty(site.Layers);
        Assert.Empty(site.Relations);
        Assert.False(site.IsModelled);
    }

    [Fact]
    public void A_site_keeps_its_name_and_its_curve() {
        // Arrange
        CalibrationCurve curve = Curves.Identity();

        // Act
        SiteChronology site = Any.SiteChronology().WithSiteName("Star Carr").WithCurve(curve).Generate();

        // Assert
        Assert.Equal("Star Carr", site.SiteName);
        Assert.Same(curve, site.Curve);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_site_without_a_name_is_refused(string? blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.SiteChronology().WithSiteName(blank!).Generate());

        // Assert
        Assert.Equal("siteName", refusal.ParamName);
    }

    [Fact]
    public void A_site_without_a_curve_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Any.SiteChronology().WithCurve((CalibrationCurve)null!).Generate());

        // Assert
        Assert.Equal("curve", refusal.ParamName);
    }

    [Fact]
    public void The_estimates_of_a_site_that_was_never_modelled_are_refused() {
        // Arrange
        SiteChronology site = Any.SiteChronology().Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => site.Estimates);
    }

    // ---------------------------------------------------------------- layers

    [Fact]
    public void Layers_are_recorded_in_the_order_they_were_added() {
        // Arrange: the codes are in the opposite order of their alphabet, so an order by code would show
        SiteChronology site = Any.SiteChronology().Generate();

        // Act
        site.AddLayer(Any.StratigraphicLayer().WithCode("US2").Generate());
        site.AddLayer(Any.StratigraphicLayer().WithCode("US1").Generate());

        // Assert
        Assert.Equal(["US2", "US1"], site.Layers.Select(layer => layer.Code));
    }

    [Fact]
    public void A_second_layer_with_the_same_code_is_refused_and_not_recorded() {
        // Arrange
        SiteChronology     site          = Any.SiteChronology().Generate();
        StratigraphicLayer layer         = Any.StratigraphicLayer().Generate();
        StratigraphicLayer sameCodeLayer = Any.StratigraphicLayer().WithCode(layer.Code).Generate();
        site.AddLayer(layer);

        // Act
        Assert.Throws<InvalidOperationException>(() => site.AddLayer(sameCodeLayer));

        // Assert
        Assert.Single(site.Layers);
    }

    [Fact]
    public void A_missing_layer_is_refused() {
        // Arrange
        SiteChronology site = Any.SiteChronology().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => site.AddLayer(null!));
    }

    // ---------------------------------------------------------------- samples

    [Fact]
    public void A_sample_is_attached_to_the_layer_it_is_given_for_and_to_no_other() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").WithLayerWithoutSample("US2").Build();
        Sample         sample = Any.Sample().WithLabCode("OxA-12345").Generate();

        // Act
        site.AttachSample("US2", sample);

        // Assert
        Assert.Empty(site.Layers.Single(layer => layer.Code == "US1").Samples);
        Assert.Equal(["OxA-12345"], site.Layers.Single(layer => layer.Code == "US2").Samples.Select(attached => attached.LabCode));
    }

    [Fact]
    public void Several_samples_accumulate_on_one_layer_in_the_order_they_came() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").Build();

        // Act
        site.AttachSample("US1", Any.Sample().WithLabCode("OxA-30002").Generate());
        site.AttachSample("US1", Any.Sample().WithLabCode("OxA-30001").Generate());

        // Assert
        Assert.Equal(["OxA-30002", "OxA-30001"], site.Layers.Single().Samples.Select(sample => sample.LabCode));
    }

    [Fact]
    public void A_sample_for_a_layer_the_site_does_not_hold_is_refused() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").Build();
        Sample         anySample = Any.Sample().Generate();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => site.AttachSample("US2", anySample));
    }

    [Fact]
    public void A_laboratory_code_already_on_the_layer_is_refused_and_the_sample_not_attached() {
        // Arrange
        SiteChronology site   = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").Build();
        Sample         sample = Any.Sample().Generate();
        site.AttachSample("US1", sample);
        Sample sameLabCodeSample = Any.Sample().WithLabCode(sample.LabCode).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => site.AttachSample("US1", sameLabCodeSample));

        // Assert
        Assert.Single(site.Layers.Single().Samples);
    }

    [Fact]
    public void A_laboratory_code_already_on_another_layer_is_refused() {
        // Arrange
        SiteChronology site   = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").WithLayerWithoutSample("US2").Build();
        Sample         sample = Any.Sample().Generate();
        site.AttachSample("US1", sample);
        Sample sameLabCodeSample = Any.Sample().WithLabCode(sample.LabCode).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => site.AttachSample("US2", sameLabCodeSample));
    }

    [Fact]
    public void A_missing_sample_is_refused() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => site.AttachSample("US1", null!));
    }

    // ---------------------------------------------------------------- successions

    [Fact]
    public void A_succession_between_two_layers_of_the_site_is_recorded() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US2").WithLayerWithoutSample("US1").Build();

        // Act
        site.AddRelation(new PrecedenceRelation("US2", "US1"));

        // Assert
        Assert.Equal([new PrecedenceRelation("US2", "US1")], site.Relations);
    }

    [Fact]
    public void A_succession_from_a_layer_the_site_does_not_hold_is_refused() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").Build();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => site.AddRelation(new PrecedenceRelation("US9", "US1")));
    }

    [Fact]
    public void A_succession_to_a_layer_the_site_does_not_hold_is_refused() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").Build();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => site.AddRelation(new PrecedenceRelation("US1", "US9")));
    }

    [Fact]
    public void The_same_succession_twice_is_refused() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").WithLayerWithoutSample("US2").WithSuccession("US1", "US2").Build();

        // Act
        Assert.Throws<InvalidOperationException>(() => site.AddRelation(new PrecedenceRelation("US1", "US2")));

        // Assert
        Assert.Single(site.Relations);
    }

    [Fact]
    public void The_opposite_of_a_recorded_succession_closes_a_cycle_and_is_refused() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayerWithoutSample("US1").WithLayerWithoutSample("US2").WithSuccession("US1", "US2").Build();

        // Act
        StratigraphicInconsistencyException refusal = Assert.Throws<StratigraphicInconsistencyException>(
            () => site.AddRelation(new PrecedenceRelation("US2", "US1")));

        // Assert
        Assert.Equal("US2", refusal.OlderLayerCode);
        Assert.Equal("US1", refusal.YoungerLayerCode);
        Assert.Single(site.Relations);
    }

    [Fact]
    public void A_succession_closing_a_cycle_through_a_third_layer_is_refused() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayerWithoutSample("US1").WithLayerWithoutSample("US2").WithLayerWithoutSample("US3")
                              .WithSuccession("US1", "US2").WithSuccession("US2", "US3")
                              .Build();

        // Act & Assert
        Assert.Throws<StratigraphicInconsistencyException>(() => site.AddRelation(new PrecedenceRelation("US3", "US1")));
    }

    [Fact]
    public void A_shortcut_over_an_existing_chain_is_not_a_cycle() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayerWithoutSample("US1").WithLayerWithoutSample("US2").WithLayerWithoutSample("US3")
                              .WithSuccession("US1", "US2").WithSuccession("US2", "US3")
                              .Build();

        // Act
        site.AddRelation(new PrecedenceRelation("US1", "US3"));

        // Assert
        Assert.Equal(3, site.Relations.Count);
    }

    [Fact]
    public void Two_branches_meeting_again_are_not_a_cycle() {
        // Arrange: US1 beneath US2 and US3, both beneath US4
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayerWithoutSample("US1").WithLayerWithoutSample("US2").WithLayerWithoutSample("US3").WithLayerWithoutSample("US4")
                              .WithSuccession("US1", "US2").WithSuccession("US1", "US3").WithSuccession("US2", "US4")
                              .Build();

        // Act
        site.AddRelation(new PrecedenceRelation("US3", "US4"));

        // Assert
        Assert.Equal(4, site.Relations.Count);
    }

    [Fact]
    public void A_missing_succession_is_refused() {
        // Arrange
        SiteChronology site = Any.SiteChronology().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => site.AddRelation(null!));
    }

    // ---------------------------------------------------------------- modelling: what it needs

    [Fact]
    public void A_site_without_any_layer_cannot_be_modelled() {
        // Arrange
        SiteChronology site = Any.SiteChronology().WithCurve(Curves.Identity()).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => site.Model());
    }

    [Fact]
    public void A_layer_without_any_dated_sample_prevents_the_modelling() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayer("US1", 3_000).WithLayerWithoutSample("US2").Build();

        // Act
        Assert.Throws<InvalidOperationException>(() => site.Model());

        // Assert
        Assert.False(site.IsModelled);
    }

    // ---------------------------------------------------------------- modelling: what it gives

    [Fact]
    public void A_layer_dated_by_one_sample_is_placed_where_the_curve_puts_that_sample() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayer("US1", 3_000).Build();

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        OccupationEstimate estimate = Assert.Single(estimates);
        Assert.Equal("US1", estimate.LayerCode);
        Assert.Equal(-1_050, estimate.Date.Mean, 1);
        Assert.False(estimate.IsMultimodal);
        Assert.True(estimate.Envelope.Contains(-1_050));
    }

    [Fact]
    public void Unrelated_layers_are_each_placed_by_their_own_samples() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayer("US1", 3_000).WithLayer("US2", 1_000).Build();

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(-1_050, estimates.Single(estimate => estimate.LayerCode == "US1").Date.Mean, 1);
        Assert.Equal(950, estimates.Single(estimate => estimate.LayerCode == "US2").Date.Mean, 1);
    }

    [Fact]
    public void Samples_of_a_layer_are_combined_and_a_layer_dated_by_two_distant_samples_has_two_plausible_intervals() {
        // Arrange: 400 years apart, far beyond the spread of a measurement
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayer("US1", 3_000, 3_400).Build();

        // Act
        OccupationEstimate estimate = Assert.Single(site.Model());

        // Assert
        Assert.True(estimate.IsMultimodal);
        Assert.Equal(-1_250, estimate.Date.Mean, 1);
        Assert.True(estimate.Envelope.Contains(-1_450));
        Assert.True(estimate.Envelope.Contains(-1_050));
    }

    [Fact]
    public void A_layer_dated_by_a_marine_shell_is_placed_at_the_age_corrected_for_the_reservoir() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayerDatedByMarineShell("US1", new RadiocarbonAge(3_400, 30), 400)
                              .Build();

        // Act
        OccupationEstimate estimate = Assert.Single(site.Model());

        // Assert
        Assert.Equal(-1_050, estimate.Date.Mean, 1);
    }

    // ---------------------------------------------------------------- modelling: stratigraphic order

    [Fact]
    public void Estimates_come_in_stratigraphic_order_from_the_oldest_layer_to_the_youngest() {
        // Arrange: added youngest first, and the codes sort the other way round than the layers
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayer("US1", 2_000).WithLayer("US2", 3_000).WithLayer("US3", 4_000)
                              .WithSuccession("US3", "US2").WithSuccession("US2", "US1")
                              .Build();

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(["US3", "US2", "US1"], estimates.Select(estimate => estimate.LayerCode));
    }

    [Fact]
    public void Two_layers_above_the_same_older_one_both_come_after_it() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayer("US1", 2_000).WithLayer("US2", 2_500).WithLayer("US3", 4_000)
                              .WithSuccession("US3", "US1").WithSuccession("US3", "US2")
                              .Build();

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal("US3", estimates[0].LayerCode);
        Assert.Equal(["US1", "US2"], estimates.Skip(1).Select(estimate => estimate.LayerCode).Order());
    }

    [Fact]
    public void Dates_that_agree_with_the_succession_are_left_as_they_are() {
        // Arrange: a thousand years between the two layers
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayer("US1", 3_000).WithLayer("US2", 4_000)
                              .WithSuccession("US2", "US1")
                              .Build();

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(-2_050, estimates.Single(estimate => estimate.LayerCode == "US2").Date.Mean, 1);
        Assert.Equal(-1_050, estimates.Single(estimate => estimate.LayerCode == "US1").Date.Mean, 1);
    }

    [Fact]
    public void A_younger_layer_dated_before_its_older_neighbour_starts_is_cut_to_where_the_older_one_starts() {
        // Arrange: US1 lies above US2, yet its sample is 50 years OLDER than US2's, and a century wide
        CalibrationCurve curve = Curves.Identity();
        RadiocarbonAge   olderLayerAge   = new RadiocarbonAge(3_000, 100);
        RadiocarbonAge   youngerLayerAge = new RadiocarbonAge(3_050, 100);
        SiteChronology site = new SiteBuilder(curve)
                              .WithLayerDatedBy("US2", olderLayerAge).WithLayerDatedBy("US1", youngerLayerAge)
                              .WithSuccession("US2", "US1")
                              .Build();
        CalendarInterval olderAlone   = Envelope(curve.Calibrate(olderLayerAge));
        CalendarInterval youngerAlone = Envelope(curve.Calibrate(youngerLayerAge));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        OccupationEstimate younger = estimates.Single(estimate => estimate.LayerCode == "US1");
        Assert.True(youngerAlone.StartYear < olderAlone.StartYear, "the scenario needs the younger layer to start earlier alone");
        Assert.True(younger.Envelope.StartYear >= olderAlone.StartYear);
        Assert.Equal(0, Dating.MassWithin(younger.Date, new CalendarInterval(younger.Date.FirstYear, olderAlone.StartYear - OneYear)), 9);
    }

    [Fact]
    public void An_older_layer_dated_after_its_younger_neighbour_ends_is_cut_to_where_the_younger_one_ends() {
        // Arrange: US2 lies beneath US1, yet its sample is 50 years YOUNGER than US1's
        CalibrationCurve curve = Curves.Identity();
        RadiocarbonAge   olderLayerAge   = new RadiocarbonAge(3_000, 100);
        RadiocarbonAge   youngerLayerAge = new RadiocarbonAge(3_050, 100);
        SiteChronology site = new SiteBuilder(curve)
                              .WithLayerDatedBy("US2", olderLayerAge).WithLayerDatedBy("US1", youngerLayerAge)
                              .WithSuccession("US2", "US1")
                              .Build();
        CalendarInterval youngerAlone = Envelope(curve.Calibrate(youngerLayerAge));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        OccupationEstimate older = estimates.Single(estimate => estimate.LayerCode == "US2");
        Assert.True(older.Envelope.EndYear <= youngerAlone.EndYear);
    }

    [Fact]
    public void The_succession_removes_the_peak_of_a_date_it_makes_impossible() {
        // Arrange: on this curve 2900 BP is met around -900 and -700; the layer beneath starts around -845, so the
        //          earlier peak of the upper layer cannot be
        CalibrationCurve curve = Curves.WithAReversalAtYearMinus800();
        SiteChronology free = new SiteBuilder(curve)
                              .WithLayerDatedBy("US1", new RadiocarbonAge(2_900, 20))
                              .Build();
        SiteChronology bound = new SiteBuilder(curve)
                               .WithLayerDatedBy("US2", new RadiocarbonAge(2_800, 20)).WithLayerDatedBy("US1", new RadiocarbonAge(2_900, 20))
                               .WithSuccession("US2", "US1")
                               .Build();

        // Act
        OccupationEstimate alone      = Assert.Single(free.Model());
        OccupationEstimate constrained = bound.Model().Single(estimate => estimate.LayerCode == "US1");

        // Assert
        Assert.True(alone.IsMultimodal);
        Assert.False(constrained.IsMultimodal);
        Assert.True(constrained.Envelope.Contains(-700));
        Assert.False(constrained.Envelope.Contains(-900));
    }

    // ---------------------------------------------------------------- modelling: contradictions

    [Fact]
    public void A_younger_layer_dated_long_before_an_older_one_contradicts_the_succession_and_names_both() {
        // Arrange: US1 lies above US2 although its sample is 2000 years older
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayer("US2", 2_000).WithLayer("US1", 4_000)
                              .WithSuccession("US2", "US1")
                              .Build();

        // Act
        StratigraphicInconsistencyException refusal = Assert.Throws<StratigraphicInconsistencyException>(() => site.Model());

        // Assert
        Assert.Equal("US2", refusal.OlderLayerCode);
        Assert.Equal("US1", refusal.YoungerLayerCode);
        Assert.Contains("US2", refusal.Message);
        Assert.Contains("US1", refusal.Message);
    }

    [Fact]
    public void The_contradiction_names_the_pair_that_clashes_and_not_the_whole_chain() {
        // Arrange: US1 < US2 is sound, US2 < US3 is not
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayer("US1", 4_000).WithLayer("US2", 3_000).WithLayer("US3", 3_500)
                              .WithSuccession("US1", "US2").WithSuccession("US2", "US3")
                              .Build();

        // Act
        StratigraphicInconsistencyException refusal = Assert.Throws<StratigraphicInconsistencyException>(() => site.Model());

        // Assert
        Assert.Equal("US2", refusal.OlderLayerCode);
        Assert.Equal("US3", refusal.YoungerLayerCode);
    }

    [Fact]
    public void A_site_whose_dates_contradict_its_succession_stays_unmodelled() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity())
                              .WithLayer("US2", 2_000).WithLayer("US1", 4_000)
                              .WithSuccession("US2", "US1")
                              .Build();

        // Act
        Assert.Throws<StratigraphicInconsistencyException>(() => site.Model());

        // Assert
        Assert.False(site.IsModelled);
        Assert.Throws<InvalidOperationException>(() => site.Estimates);
    }

    // ---------------------------------------------------------------- once modelled

    [Fact]
    public void A_modelled_site_hands_out_the_estimates_it_computed() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayer("US1", 3_000).WithLayer("US2", 1_000).Build();

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.True(site.IsModelled);
        Assert.Equal(estimates, site.Estimates);
    }

    [Fact]
    public void A_modelled_site_cannot_be_modelled_again() {
        // Arrange
        SiteChronology site = ModelledSite();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => site.Model());
    }

    [Fact]
    public void A_modelled_site_refuses_a_new_layer() {
        // Arrange
        SiteChronology     site     = ModelledSite();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().WithCode("US9").Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => site.AddLayer(anyLayer));

        // Assert
        Assert.Single(site.Layers);
    }

    [Fact]
    public void A_modelled_site_refuses_a_new_sample() {
        // Arrange
        SiteChronology site      = ModelledSite();
        Sample         anySample = Any.Sample().Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => site.AttachSample("US1", anySample));
    }

    [Fact]
    public void A_modelled_site_refuses_a_new_succession() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayer("US1", 3_000).WithLayer("US2", 4_000).Build();
        site.Model();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => site.AddRelation(new PrecedenceRelation("US2", "US1")));
    }

    [Fact]
    public void Reopening_a_site_discards_its_estimates() {
        // Arrange
        SiteChronology site = ModelledSite();

        // Act
        site.Reopen();

        // Assert
        Assert.False(site.IsModelled);
        Assert.Throws<InvalidOperationException>(() => site.Estimates);
    }

    [Fact]
    public void A_reopened_site_accepts_changes_and_is_modelled_again_with_them() {
        // Arrange
        SiteChronology site = ModelledSite();
        site.Reopen();
        site.AddLayer(Any.StratigraphicLayer().WithCode("US2").Generate());
        site.AttachSample("US2", Any.Sample().WithMaterial(SampleMaterial.Bone).WithMeasurement(new RadiocarbonAge(1_000, 30)).Generate());

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(["US1", "US2"], estimates.Select(estimate => estimate.LayerCode).Order());
    }

    [Fact]
    public void Modelling_a_reopened_site_that_did_not_change_gives_the_same_dates() {
        // Arrange
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayer("US1", 3_000).Build();
        double         meanBefore = site.Model().Single().Date.Mean;
        site.Reopen();

        // Act
        double meanAfter = site.Model().Single().Date.Mean;

        // Assert
        Assert.Equal(meanBefore, meanAfter);
    }

    [Fact]
    public void Reopening_a_site_that_is_not_modelled_changes_nothing() {
        // Arrange
        SiteChronology site = Any.SiteChronology().Generate();

        // Act
        site.Reopen();

        // Assert
        Assert.False(site.IsModelled);
    }

    // ---------------------------------------------------------------- helpers

    private static SiteChronology ModelledSite() {
        SiteChronology site = new SiteBuilder(Curves.Identity()).WithLayer("US1", 3_000).Build();
        site.Model();

        return site;
    }

    private static CalendarInterval Envelope(CalibratedDate date) {
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(OccupationEstimate.CredibleLevel);

        return new CalendarInterval(region[0].StartYear, region[^1].EndYear);
    }

}
