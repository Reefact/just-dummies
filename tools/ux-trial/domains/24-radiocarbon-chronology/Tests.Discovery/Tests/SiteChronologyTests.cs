namespace RadiocarbonChronology.Tests.Discovery;

public sealed class SiteChronologyTests {

    // ---- Creating a site

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_site_needs_a_name(string? siteName) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.SiteChronology().WithSiteName(siteName!).Generate());

        // Assert
        Assert.Equal("siteName", refusal.ParamName);
    }

    [Fact]
    public void A_site_needs_a_calibration_curve() {
        // Arrange
        string anySiteName = Any.SiteChronology().Generate().SiteName;

        // Assert
        Assert.Throws<ArgumentNullException>(() => new SiteChronology(anySiteName, null!));
    }

    [Fact]
    public void A_new_site_is_open_and_empty() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();

        // Assert
        Assert.False(anySite.IsModelled);
        Assert.Empty(anySite.Layers);
        Assert.Empty(anySite.Relations);
    }

    // ---- Layers

    [Fact]
    public void A_layer_is_recorded_on_the_site() {
        // Arrange
        SiteChronology     anySite  = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();

        // Act
        anySite.AddLayer(anyLayer);

        // Assert
        Assert.Same(anyLayer, Assert.Single(anySite.Layers));
    }

    [Fact]
    public void A_missing_layer_is_refused() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySite.AddLayer(null!));
    }

    [Fact]
    public void Two_layers_cannot_share_a_code() {
        // Arrange
        SiteChronology     anySite       = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer      = Any.StratigraphicLayer().Generate();
        StratigraphicLayer anyOtherLayer = Any.StratigraphicLayer().WithCode(anyLayer.Code).Generate();
        anySite.AddLayer(anyLayer);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anySite.AddLayer(anyOtherLayer));

        // Assert
        Assert.Contains(anyLayer.Code, refusal.Message);
        Assert.Single(anySite.Layers);
    }

    // ---- Samples

    [Fact]
    public void A_sample_is_attached_to_the_layer_it_came_from() {
        // Arrange
        SiteChronology     anySite  = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();
        Sample             anySample = Any.Sample().Generate();
        anySite.AddLayer(anyLayer);

        // Act
        anySite.AttachSample(anyLayer.Code, anySample);

        // Assert
        Assert.Same(anySample, Assert.Single(anyLayer.Samples));
    }

    [Fact]
    public void A_sample_cannot_be_attached_to_a_layer_the_site_does_not_know() {
        // Arrange
        SiteChronology     anySite         = Any.SiteChronology().Generate();
        StratigraphicLayer anyKnownLayer   = Any.StratigraphicLayer().Generate();
        StratigraphicLayer anyUnknownLayer = Any.StratigraphicLayer().WithCode(c => c.DifferentFrom(anyKnownLayer.Code)).Generate();
        anySite.AddLayer(anyKnownLayer);

        // Act
        KeyNotFoundException refusal = Assert.Throws<KeyNotFoundException>(() => anySite.AttachSample(anyUnknownLayer.Code, Any.Sample().Generate()));

        // Assert
        Assert.Contains(anyUnknownLayer.Code, refusal.Message);
    }

    [Fact]
    public void A_missing_sample_is_refused() {
        // Arrange
        SiteChronology     anySite  = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();
        anySite.AddLayer(anyLayer);

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySite.AttachSample(anyLayer.Code, null!));
    }

    [Fact]
    public void A_lab_code_cannot_be_attached_twice_to_the_same_layer() {
        // Arrange
        SiteChronology     anySite   = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer  = Any.StratigraphicLayer().Generate();
        Sample             anySample = Any.Sample().Generate();
        anySite.AddLayer(anyLayer);
        anySite.AttachSample(anyLayer.Code, anySample);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anySite.AttachSample(anyLayer.Code, anySample));

        // Assert
        Assert.Contains(anySample.LabCode, refusal.Message);
        Assert.Single(anyLayer.Samples);
    }

    [Fact]
    public void A_lab_code_cannot_be_attached_to_two_layers_of_the_site() {
        // Arrange
        SiteChronology     anySite       = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer      = Any.StratigraphicLayer().Generate();
        StratigraphicLayer anyOtherLayer = Any.StratigraphicLayer().WithCode(c => c.DifferentFrom(anyLayer.Code)).Generate();
        Sample             anySample     = Any.Sample().Generate();
        Sample             anyResample   = Any.Sample().WithLabCode(anySample.LabCode).Generate();
        anySite.AddLayer(anyLayer);
        anySite.AddLayer(anyOtherLayer);
        anySite.AttachSample(anyLayer.Code, anySample);

        // Assert
        Assert.Throws<InvalidOperationException>(() => anySite.AttachSample(anyOtherLayer.Code, anyResample));
        Assert.Empty(anyOtherLayer.Samples);
    }

    // ---- Succession

    [Fact]
    public void A_succession_between_two_known_layers_is_recorded() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        Sites.AddDatedLayer(anySite, "US2", 2_500);
        Sites.AddDatedLayer(anySite, "US1", 2_000);

        // Act
        Sites.Precedes(anySite, "US2", "US1");

        // Assert
        Assert.Equal([new PrecedenceRelation("US2", "US1")], anySite.Relations);
    }

    [Fact]
    public void A_missing_succession_is_refused() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anySite.AddRelation(null!));
    }

    [Theory]
    [InlineData("US1", "US9")]   // the younger layer is unknown
    [InlineData("US9", "US1")]   // the older layer is unknown
    public void A_succession_with_an_unknown_layer_is_refused(string olderLayerCode, string youngerLayerCode) {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        anySite.AddLayer(Any.StratigraphicLayer().WithCode("US1").Generate());

        // Act
        KeyNotFoundException refusal = Assert.Throws<KeyNotFoundException>(() => anySite.AddRelation(new PrecedenceRelation(olderLayerCode, youngerLayerCode)));

        // Assert
        Assert.Contains("US9", refusal.Message);
        Assert.Empty(anySite.Relations);
    }

    [Fact]
    public void The_same_succession_cannot_be_recorded_twice() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        Sites.AddDatedLayer(anySite, "US2", 2_500);
        Sites.AddDatedLayer(anySite, "US1", 2_000);
        Sites.Precedes(anySite, "US2", "US1");

        // Assert
        Assert.Throws<InvalidOperationException>(() => Sites.Precedes(anySite, "US2", "US1"));
        Assert.Single(anySite.Relations);
    }

    [Fact]
    public void A_succession_that_reverses_a_recorded_one_closes_a_cycle() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        Sites.AddDatedLayer(anySite, "US2", 2_500);
        Sites.AddDatedLayer(anySite, "US1", 2_000);
        Sites.Precedes(anySite, "US2", "US1");

        // Act
        StratigraphicInconsistencyException refusal = Assert.Throws<StratigraphicInconsistencyException>(() => Sites.Precedes(anySite, "US1", "US2"));

        // Assert
        Assert.Equal("US1", refusal.OlderLayerCode);
        Assert.Equal("US2", refusal.YoungerLayerCode);
        Assert.Contains("cycle", refusal.Message);
        Assert.Single(anySite.Relations);
    }

    [Fact]
    public void A_succession_that_closes_a_cycle_through_a_third_layer_is_refused() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        Sites.AddDatedLayer(anySite, "US3", 3_000);
        Sites.AddDatedLayer(anySite, "US2", 2_500);
        Sites.AddDatedLayer(anySite, "US1", 2_000);
        Sites.Precedes(anySite, "US3", "US2");
        Sites.Precedes(anySite, "US2", "US1");

        // Assert
        Assert.Throws<StratigraphicInconsistencyException>(() => Sites.Precedes(anySite, "US1", "US3"));
        Assert.Equal(2, anySite.Relations.Count);
    }

    [Fact]
    public void Two_routes_between_the_same_layers_are_not_a_cycle() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        Sites.AddDatedLayer(anySite, "US3", 3_000);
        Sites.AddDatedLayer(anySite, "US2", 2_500);
        Sites.AddDatedLayer(anySite, "US1", 2_000);
        Sites.Precedes(anySite, "US3", "US2");
        Sites.Precedes(anySite, "US2", "US1");

        // Act
        Sites.Precedes(anySite, "US3", "US1");

        // Assert
        Assert.Equal(3, anySite.Relations.Count);
    }

    // ---- Modelling

    [Fact]
    public void A_site_without_layer_cannot_be_modelled() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => anySite.Model());
        Assert.False(anySite.IsModelled);
    }

    [Fact]
    public void A_layer_without_sample_cannot_be_modelled() {
        // Arrange
        SiteChronology anySite = Sites.OnLinearCurve();
        Sites.AddDatedLayer(anySite, "US1", 2_000);
        anySite.AddLayer(Any.StratigraphicLayer().WithCode("US2").Generate());

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anySite.Model());

        // Assert
        Assert.Contains("US2", refusal.Message);
    }

    [Fact]
    public void A_single_layer_is_dated_by_its_calibrated_sample() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US1", 2_000);

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        OccupationEstimate estimate = Assert.Single(estimates);
        Assert.Equal("US1", estimate.LayerCode);
        Assert.Equal(-50.0, estimate.Date.Mean, 0.5);
        Assert.False(estimate.IsMultimodal);
    }

    [Fact]
    public void A_layer_dated_by_two_distant_samples_spans_both_periods() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US1", 3_000, 2_000);

        // Act
        OccupationEstimate estimate = Assert.Single(site.Model());

        // Assert
        Assert.True(estimate.IsMultimodal);
        Assert.Equal(-550.0, estimate.Date.Mean, 1.0);
        Assert.True(estimate.PlausibleIntervals[0].Contains(-1_050), estimate.PlausibleIntervals[0].ToString());
        Assert.True(estimate.PlausibleIntervals[1].Contains(-50), estimate.PlausibleIntervals[1].ToString());
    }

    [Fact]
    public void Estimates_come_oldest_layer_first_whatever_the_order_layers_and_successions_were_recorded_in() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US30", 2_000);
        Sites.AddDatedLayer(site, "US10", 3_000);
        Sites.AddDatedLayer(site, "US20", 2_500);
        Sites.Precedes(site, "US20", "US30");
        Sites.Precedes(site, "US10", "US20");

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(["US10", "US20", "US30"], estimates.Select(estimate => estimate.LayerCode));
        Assert.Equal([-1_050.0, -550.0, -50.0], estimates.Select(estimate => estimate.Date.Mean), new MeanComparer(1.0));
    }

    [Fact]
    public void Layers_whose_dates_overlap_are_pulled_apart_by_their_succession() {
        // Arrange
        SiteChronology free = Sites.OnLinearCurve();
        Sites.AddLayerMeasuredTo(free, "US2", 100, 2_000);
        Sites.AddLayerMeasuredTo(free, "US1", 100, 2_000);

        SiteChronology ordered = Sites.OnLinearCurve();
        Sites.AddLayerMeasuredTo(ordered, "US2", 100, 2_000);
        Sites.AddLayerMeasuredTo(ordered, "US1", 100, 2_000);
        Sites.Precedes(ordered, "US2", "US1");

        // Act
        IReadOnlyList<OccupationEstimate> unconstrained = free.Model();
        IReadOnlyList<OccupationEstimate> constrained   = ordered.Model();

        // Assert
        OccupationEstimate olderFree = unconstrained.Single(estimate => estimate.LayerCode == "US2");
        OccupationEstimate youngerFree = unconstrained.Single(estimate => estimate.LayerCode == "US1");
        OccupationEstimate older = constrained.Single(estimate => estimate.LayerCode == "US2");
        OccupationEstimate younger = constrained.Single(estimate => estimate.LayerCode == "US1");

        Assert.Equal(olderFree.Date.Mean, youngerFree.Date.Mean, 1e-9);
        Assert.True(older.Date.Mean < olderFree.Date.Mean, $"{older.Date.Mean} against {olderFree.Date.Mean}");
        Assert.True(younger.Date.Mean > youngerFree.Date.Mean, $"{younger.Date.Mean} against {youngerFree.Date.Mean}");
        Assert.True(older.Date.Mean < younger.Date.Mean);
    }

    [Fact]
    public void A_layer_dated_after_the_layers_it_lies_above_is_refused_naming_both() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US2", 1_000);   // the older layer dates to about 950
        Sites.AddDatedLayer(site, "US1", 2_500);   // the younger one to about -550
        Sites.Precedes(site, "US2", "US1");

        // Act
        StratigraphicInconsistencyException refusal = Assert.Throws<StratigraphicInconsistencyException>(() => site.Model());

        // Assert
        Assert.Equal("US2", refusal.OlderLayerCode);
        Assert.Equal("US1", refusal.YoungerLayerCode);
        Assert.Contains("'US2'", refusal.Message);
        Assert.Contains("'US1'", refusal.Message);
        Assert.False(site.IsModelled);
    }

    [Fact]
    public void A_diamond_of_successions_is_modelled_with_the_common_ancestor_first_and_the_common_descendant_last() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US4", 2_000);
        Sites.AddDatedLayer(site, "US2", 2_500);
        Sites.AddDatedLayer(site, "US3", 2_500);
        Sites.AddDatedLayer(site, "US1", 3_000);
        Sites.Precedes(site, "US1", "US2");
        Sites.Precedes(site, "US1", "US3");
        Sites.Precedes(site, "US2", "US4");
        Sites.Precedes(site, "US3", "US4");

        // Act
        IReadOnlyList<string> order = site.Model().Select(estimate => estimate.LayerCode).ToList();

        // Assert
        Assert.Equal("US1", order[0]);
        Assert.Equal("US4", order[3]);
        Assert.Equivalent(new[] { "US2", "US3" }, order.Skip(1).Take(2));
    }

    [Fact]
    public void A_sample_older_than_the_curve_can_calibrate_stops_the_modelling() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US1", 50_000);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => site.Model());
        Assert.False(site.IsModelled);
    }

    // ---- Once modelled

    [Fact]
    public void Estimates_are_refused_before_the_site_is_modelled() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anySite.Estimates);

        // Assert
        Assert.Contains(anySite.SiteName, refusal.Message);
    }

    [Fact]
    public void Once_modelled_the_site_keeps_its_estimates() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US1", 2_000);

        // Act
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.True(site.IsModelled);
        Assert.Same(estimates, site.Estimates);
    }

    [Fact]
    public void Once_modelled_every_change_is_refused() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US1", 2_000);
        site.Model();

        // Assert
        Assert.Throws<InvalidOperationException>(() => site.AddLayer(Any.StratigraphicLayer().WithCode("US2").Generate()));
        Assert.Throws<InvalidOperationException>(() => site.AttachSample("US1", Any.Sample().Generate()));
        Assert.Throws<InvalidOperationException>(() => site.AddRelation(new PrecedenceRelation("US1", "US2")));
        Assert.Throws<InvalidOperationException>(() => site.Model());
    }

    [Fact]
    public void Reopening_allows_a_change_and_a_new_modelling_that_takes_it_into_account() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US1", 2_000);
        site.Model();

        // Act
        site.Reopen();
        Sites.AddDatedLayer(site, "US2", 2_500);
        Sites.Precedes(site, "US2", "US1");
        IReadOnlyList<OccupationEstimate> estimates = site.Model();

        // Assert
        Assert.Equal(["US2", "US1"], estimates.Select(estimate => estimate.LayerCode));
    }

    [Fact]
    public void Reopening_discards_the_estimates() {
        // Arrange
        SiteChronology site = Sites.OnLinearCurve();
        Sites.AddDatedLayer(site, "US1", 2_000);
        site.Model();

        // Act
        site.Reopen();

        // Assert
        Assert.False(site.IsModelled);
        Assert.Throws<InvalidOperationException>(() => site.Estimates);
    }

    private sealed class MeanComparer : IEqualityComparer<double> {

        private readonly double _tolerance;

        public MeanComparer(double tolerance) {
            _tolerance = tolerance;
        }

        public bool Equals(double x, double y) => Math.Abs(x - y) <= _tolerance;

        public int GetHashCode(double obj) => 0;

    }

}
