using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

using Xunit;

namespace RadiocarbonChronology.Tests.Expert;

/// <summary>
/// Modelling a site, read against the straight curve so that N BP is calendar year 1950 - N. The ages are
/// literals: whether layers agree with their succession is decided by them. Codes, descriptions, lab
/// codes, materials and the site's name are arbitrary.
/// </summary>
public sealed class SiteChronologyModellingTests {

    [Fact]
    public void Estimates_come_in_stratigraphic_order_whatever_the_recording_order() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        anySite.AddDatedLayer("US3", new RadiocarbonAge(2_000, 25));
        anySite.AddDatedLayer("US1", new RadiocarbonAge(3_000, 25));
        anySite.AddDatedLayer("US2", new RadiocarbonAge(2_500, 25));
        anySite.AddRelation(new PrecedenceRelation("US2", "US3"));
        anySite.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = anySite.Model();

        // Assert
        Assert.Equal(["US1", "US2", "US3"], estimates.Select(estimate => estimate.LayerCode));
    }

    [Fact]
    public void A_younger_layer_cannot_start_before_the_plausible_start_of_the_layer_beneath_it() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        anySite.AddDatedLayer("US1", new RadiocarbonAge(2_000, 20));    // older: about -90 to -10
        anySite.AddDatedLayer("US2", new RadiocarbonAge(2_000, 150));   // younger, poorly dated: about -350 to 250
        anySite.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = anySite.Model();

        // Assert
        OccupationEstimate younger = estimates.Single(estimate => estimate.LayerCode == "US2");
        Assert.Equal(0.0, younger.Date.ProbabilityOf(-200));
        Assert.True(younger.Date.ProbabilityOf(100) > 0);
    }

    [Fact]
    public void An_older_layer_cannot_end_after_the_plausible_end_of_the_layer_above_it() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        anySite.AddDatedLayer("US1", new RadiocarbonAge(2_000, 150));   // older, poorly dated: about -350 to 250
        anySite.AddDatedLayer("US2", new RadiocarbonAge(2_000, 20));    // younger: about -90 to -10
        anySite.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        IReadOnlyList<OccupationEstimate> estimates = anySite.Model();

        // Assert
        OccupationEstimate older = estimates.Single(estimate => estimate.LayerCode == "US1");
        Assert.Equal(0.0, older.Date.ProbabilityOf(150));
        Assert.True(older.Date.ProbabilityOf(-200) > 0);
    }

    [Fact]
    public void Dates_contradicting_the_succession_are_refused_naming_both_layers() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        anySite.AddDatedLayer("US1", new RadiocarbonAge(1_000, 20));   // beneath, yet dated around 950
        anySite.AddDatedLayer("US2", new RadiocarbonAge(3_000, 20));   // above, yet dated around -1050
        anySite.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        Action modelling = () => anySite.Model();

        // Assert
        StratigraphicInconsistencyException inconsistency = Assert.Throws<StratigraphicInconsistencyException>(modelling);
        Assert.Equal(("US1", "US2"), (inconsistency.OlderLayerCode, inconsistency.YoungerLayerCode));
    }

    [Fact]
    public void A_layer_dated_by_two_distant_samples_is_read_as_two_plausible_periods() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        anySite.AddDatedLayer("US1", new RadiocarbonAge(2_000, 20), new RadiocarbonAge(2_600, 20));

        // Act
        OccupationEstimate estimate = Assert.Single(anySite.Model());

        // Assert
        Assert.True(estimate.IsMultimodal);
        Assert.Contains(estimate.PlausibleIntervals, interval => interval.Contains(-50));
        Assert.Contains(estimate.PlausibleIntervals, interval => interval.Contains(-650));
    }

    [Fact]
    public void A_marine_shell_dates_its_layer_after_reservoir_correction() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        Sample anyShell = Any.Sample()
                             .WithMaterial(SampleMaterial.MarineShell)
                             .WithMeasurement(new RadiocarbonAge(2_400, 20))
                             .WithReservoirOffset(400)
                             .Generate();
        anySite.AddLayer(Excavation.Layer("US1"));
        anySite.AttachSample("US1", anyShell);

        // Act
        OccupationEstimate estimate = Assert.Single(anySite.Model());

        // Assert: read as 2000 BP, that is around -50, not as 2400 BP around -450.
        Assert.True(estimate.Envelope.Contains(-50), $"{estimate.Envelope} should contain -50.");
        Assert.False(estimate.Envelope.Contains(-450), $"{estimate.Envelope} should not contain -450.");
    }

    [Fact]
    public void Modelling_refuses_a_layer_without_a_dated_sample() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        anySite.AddDatedLayer("US1", new RadiocarbonAge(2_000, 20));
        anySite.AddLayer(Excavation.Layer("US2"));

        // Act
        Action modelling = () => anySite.Model();

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(modelling);
        Assert.Contains("US2", refusal.Message);
    }

    [Fact]
    public void A_modelled_chronology_refuses_changes() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        anySite.AddDatedLayer("US1", new RadiocarbonAge(2_000, 20));
        anySite.Model();

        // Act
        Action recording = () => anySite.AddLayer(Excavation.Layer("US2"));

        // Assert
        Assert.Throws<InvalidOperationException>(recording);
    }

    [Fact]
    public void A_reopened_chronology_accepts_changes_and_forgets_its_estimates() {
        // Arrange
        SiteChronology anySite = Excavation.SiteOnTheStraightCurve();
        anySite.AddDatedLayer("US1", new RadiocarbonAge(2_000, 20));
        anySite.Model();

        // Act
        anySite.Reopen();
        anySite.AddLayer(Excavation.Layer("US2"));

        // Assert
        Assert.False(anySite.IsModelled);
        Assert.Throws<InvalidOperationException>(() => anySite.Estimates);
    }

    [Fact]
    public void Estimates_are_refused_before_modelling() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();

        // Act
        Func<IReadOnlyList<OccupationEstimate>> reading = () => anySite.Estimates;

        // Assert
        Assert.Throws<InvalidOperationException>(reading);
    }

}
