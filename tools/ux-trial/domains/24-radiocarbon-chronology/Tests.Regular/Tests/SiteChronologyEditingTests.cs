using RadiocarbonChronology.Domain;

using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class SiteChronologyEditingTests {

    [Fact]
    public void A_layer_code_can_be_recorded_only_once() {
        // Arrange
        SiteChronology     anySite      = Arbitrary.SiteChronology().Generate();
        StratigraphicLayer anyLayer     = Arbitrary.StratigraphicLayer().Generate();
        StratigraphicLayer anyNamesake  = Arbitrary.StratigraphicLayer().WithCode(anyLayer.Code).Generate();
        anySite.AddLayer(anyLayer);

        // Act
        Action adding = () => anySite.AddLayer(anyNamesake);

        // Assert
        Assert.Throws<InvalidOperationException>(adding);
    }

    [Fact]
    public void A_sample_cannot_be_attached_to_an_unknown_layer() {
        // Arrange
        SiteChronology anySite   = Arbitrary.SiteChronology().Generate();
        Sample         anySample = Arbitrary.Sample().Generate();
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US10").Generate());

        // Act
        Action attaching = () => anySite.AttachSample("US11", anySample);

        // Assert
        Assert.Throws<KeyNotFoundException>(attaching);
    }

    [Fact]
    public void A_laboratory_code_is_attached_once_across_the_whole_site() {
        // Arrange
        SiteChronology anySite      = Arbitrary.SiteChronology().Generate();
        Sample         anySample    = Arbitrary.Sample().Generate();
        Sample         anyDuplicate = Arbitrary.Sample().WithLabCode(anySample.LabCode).Generate();
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US10").Generate());
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US11").Generate());
        anySite.AttachSample("US10", anySample);

        // Act
        Action attaching = () => anySite.AttachSample("US11", anyDuplicate);

        // Assert
        Assert.Throws<InvalidOperationException>(attaching);
    }

    [Fact]
    public void A_succession_between_unknown_layers_is_refused() {
        // Arrange
        SiteChronology anySite = Arbitrary.SiteChronology().Generate();
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US10").Generate());

        // Act
        Action relating = () => anySite.AddRelation(new PrecedenceRelation("US20", "US10"));

        // Assert
        Assert.Throws<KeyNotFoundException>(relating);
    }

    [Fact]
    public void A_succession_closing_a_cycle_is_refused_and_names_both_layers() {
        // Arrange
        SiteChronology anySite = Arbitrary.SiteChronology().Generate();
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US10").Generate());
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US20").Generate());
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US30").Generate());
        anySite.AddRelation(new PrecedenceRelation("US30", "US20"));
        anySite.AddRelation(new PrecedenceRelation("US20", "US10"));

        // Act
        Action relating = () => anySite.AddRelation(new PrecedenceRelation("US10", "US30"));

        // Assert
        StratigraphicInconsistencyException refusal = Assert.Throws<StratigraphicInconsistencyException>(relating);
        Assert.Equal("US10", refusal.OlderLayerCode);
        Assert.Equal("US30", refusal.YoungerLayerCode);
    }

    [Fact]
    public void The_same_succession_is_recorded_once() {
        // Arrange
        SiteChronology anySite = Arbitrary.SiteChronology().Generate();
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US10").Generate());
        anySite.AddLayer(Arbitrary.StratigraphicLayer().WithCode("US20").Generate());
        anySite.AddRelation(new PrecedenceRelation("US20", "US10"));

        // Act
        Action relating = () => anySite.AddRelation(new PrecedenceRelation("US20", "US10"));

        // Assert
        Assert.Throws<InvalidOperationException>(relating);
    }

    [Fact]
    public void Estimates_are_refused_before_the_site_is_modelled() {
        // Arrange
        SiteChronology anySite = Arbitrary.SiteChronology().Generate();

        // Act
        Func<object> reading = () => anySite.Estimates;

        // Assert
        Assert.Throws<InvalidOperationException>(reading);
    }

}
