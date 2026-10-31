using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class SiteChronologyModellingTests {

    private static SiteChronology AnySiteOnTheLinearCurve() {
        return Arbitrary.SiteChronology().WithCurve(Curves.Linear()).Generate();
    }

    private static void AddDatedLayer(SiteChronology site, string code, params int[] agesBp) {
        site.AddLayer(Arbitrary.StratigraphicLayer().WithCode(code).Generate());
        foreach (int ageBp in agesBp) {
            site.AttachSample(code, Arbitrary.Sample().WithMeasurement(new RadiocarbonAge(ageBp, 20)).Generate());
        }
    }

    [Fact]
    public void A_site_without_layers_cannot_be_modelled() {
        // Arrange
        SiteChronology anySite = Arbitrary.SiteChronology().Generate();

        // Act
        Action modelling = () => anySite.Model();

        // Assert
        Assert.Throws<InvalidOperationException>(modelling);
    }

    [Fact]
    public void A_layer_without_a_dated_sample_cannot_be_modelled() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US20", 3000);
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US10").Generate());

        // Act
        Action modelling = () => anySite.Model();

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(modelling);
        Assert.Contains("US10", refusal.Message);
    }

    [Fact]
    public void Estimates_come_in_stratigraphic_order_oldest_first() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US10", 2450);
        AddDatedLayer(anySite, "US20", 3000);
        AddDatedLayer(anySite, "US30", 3950);
        anySite.AddRelation(new PrecedenceRelation("US20", "US10"));
        anySite.AddRelation(new PrecedenceRelation("US30", "US20"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = anySite.Model();

        // Assert
        Assert.Equal(["US30", "US20", "US10"], estimates.Select(e => e.LayerCode));
    }

    [Fact]
    public void Without_succession_a_layer_dated_by_two_distant_samples_reads_as_two_periods() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US10", 3950, 2450);

        // Act
        OccupationEstimate estimate = anySite.Model().Single();

        // Assert
        Assert.True(estimate.IsMultimodal);
    }

    [Fact]
    public void The_layer_beneath_rules_out_the_older_period_of_the_layer_above() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US20", 3000);
        AddDatedLayer(anySite, "US10", 3950, 2450);
        anySite.AddRelation(new PrecedenceRelation("US20", "US10"));

        // Act
        OccupationEstimate upper = anySite.Model().Single(e => e.LayerCode == "US10");

        // Assert
        Assert.False(upper.IsMultimodal);
        Assert.True(upper.Envelope.StartYear > -600);
    }

    [Fact]
    public void The_layer_above_rules_out_the_younger_period_of_the_layer_beneath() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US20", 3950, 2450);
        AddDatedLayer(anySite, "US10", 3000);
        anySite.AddRelation(new PrecedenceRelation("US20", "US10"));

        // Act
        OccupationEstimate lower = anySite.Model().Single(e => e.LayerCode == "US20");

        // Assert
        Assert.False(lower.IsMultimodal);
        Assert.True(lower.Envelope.EndYear < -1900);
    }

    [Fact]
    public void Dates_contradicting_the_succession_are_reported_with_both_layers() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US20", 2450);
        AddDatedLayer(anySite, "US10", 3950);
        anySite.AddRelation(new PrecedenceRelation("US20", "US10"));

        // Act
        Action modelling = () => anySite.Model();

        // Assert
        StratigraphicInconsistencyException refusal = Assert.Throws<StratigraphicInconsistencyException>(modelling);
        Assert.Equal("US20", refusal.OlderLayerCode);
        Assert.Equal("US10", refusal.YoungerLayerCode);
    }

    [Fact]
    public void A_modelled_site_refuses_changes_until_it_is_reopened() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US10", 3000);
        anySite.Model();
        StratigraphicLayer anyLayer = Arbitrary.StratigraphicLayer().WithCode("US20").Generate();

        // Act
        Action adding = () => anySite.AddLayer(anyLayer);

        // Assert
        Assert.Throws<InvalidOperationException>(adding);
    }

    [Fact]
    public void A_reopened_site_accepts_changes_and_forgets_its_estimates() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US10", 3000);
        anySite.Model();

        // Act
        anySite.Reopen();
        AddDatedLayer(anySite, "US20", 3950);

        // Assert
        Assert.False(anySite.IsModelled);
        Assert.Equal(2, anySite.Layers.Count);
    }

    [Fact]
    public void A_modelled_site_cannot_be_modelled_twice() {
        // Arrange
        SiteChronology anySite = AnySiteOnTheLinearCurve();
        AddDatedLayer(anySite, "US10", 3000);
        anySite.Model();

        // Act
        Action modellingAgain = () => anySite.Model();

        // Assert
        Assert.Throws<InvalidOperationException>(modellingAgain);
    }

}
