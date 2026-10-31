using JustDummies;

using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Expert.Support;

using Xunit;

namespace RadiocarbonChronology.Tests.Expert;

/// <summary>How a site records its layers, samples and successions — no calibration is read here.</summary>
public sealed class SiteChronologyRecordingTests {

    [Fact]
    public void A_layer_code_is_recorded_once_per_site() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        anySite.AddLayer(Excavation.Layer("US12"));

        // Act
        Action recordingAgain = () => anySite.AddLayer(Excavation.Layer("US12"));

        // Assert
        Assert.Throws<InvalidOperationException>(recordingAgain);
    }

    [Fact]
    public void A_sample_cannot_be_attached_to_a_layer_the_site_does_not_have() {
        // Arrange
        SiteChronology     anySite        = Any.SiteChronology().Generate();
        StratigraphicLayer anyLayer       = Any.StratigraphicLayer().Generate();
        string             anyUnknownCode = AnyStratigraphicLayer.Codes().DifferentFrom(anyLayer.Code).Generate();
        Sample             anySample      = Any.Sample().Generate();
        anySite.AddLayer(anyLayer);

        // Act
        Action attaching = () => anySite.AttachSample(anyUnknownCode, anySample);

        // Assert
        Assert.Throws<KeyNotFoundException>(attaching);
    }

    [Fact]
    public void A_lab_code_is_attached_once_per_site_even_in_another_layer() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        anySite.AddLayer(Excavation.Layer("US1"));
        anySite.AddLayer(Excavation.Layer("US2"));
        anySite.AttachSample("US1", Any.Sample().WithLabCode("OxA-12345").Generate());

        // Act
        Action attachingAgain = () => anySite.AttachSample("US2", Any.Sample().WithLabCode("OxA-12345").Generate());

        // Assert
        Assert.Throws<InvalidOperationException>(attachingAgain);
    }

    [Fact]
    public void A_succession_closing_a_cycle_is_refused_naming_both_layers() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        anySite.AddLayer(Excavation.Layer("US1"));
        anySite.AddLayer(Excavation.Layer("US2"));
        anySite.AddLayer(Excavation.Layer("US3"));
        anySite.AddRelation(new PrecedenceRelation("US1", "US2"));
        anySite.AddRelation(new PrecedenceRelation("US2", "US3"));

        // Act
        Action closingTheCycle = () => anySite.AddRelation(new PrecedenceRelation("US3", "US1"));

        // Assert
        StratigraphicInconsistencyException inconsistency = Assert.Throws<StratigraphicInconsistencyException>(closingTheCycle);
        Assert.Equal(("US3", "US1"), (inconsistency.OlderLayerCode, inconsistency.YoungerLayerCode));
    }

    [Fact]
    public void A_succession_is_recorded_once() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        anySite.AddLayer(Excavation.Layer("US1"));
        anySite.AddLayer(Excavation.Layer("US2"));
        anySite.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Act
        Action recordingAgain = () => anySite.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Assert
        Assert.Throws<InvalidOperationException>(recordingAgain);
    }

    [Fact]
    public void A_succession_involving_an_unrecorded_layer_is_refused() {
        // Arrange
        SiteChronology anySite = Any.SiteChronology().Generate();
        anySite.AddLayer(Excavation.Layer("US1"));

        // Act
        Action relating = () => anySite.AddRelation(new PrecedenceRelation("US1", "US2"));

        // Assert
        Assert.Throws<KeyNotFoundException>(relating);
    }

    [Fact]
    public void A_layer_cannot_precede_itself() {
        // Arrange
        string anyCode = AnyStratigraphicLayer.Codes().Generate();

        // Act
        Action relating = () => new PrecedenceRelation(anyCode, anyCode);

        // Assert
        Assert.Throws<ArgumentException>(relating);
    }

}
