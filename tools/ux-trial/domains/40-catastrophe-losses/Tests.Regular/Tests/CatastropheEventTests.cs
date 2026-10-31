namespace CatastropheLosses.Tests.Regular.Tests;

public sealed class CatastropheEventTests {

    [Fact]
    public void An_event_occurring_once_a_century_has_a_hundred_year_return_period() {
        // Arrange
        CatastropheEvent catastrophe = Arbitrary.CatastropheEvent().WithAnnualRate(0.01).Generate();

        // Act
        double returnPeriod = catastrophe.ReturnPeriodYears;

        // Assert
        Assert.Equal(100.0, returnPeriod, precision: 9);
    }

    [Fact]
    public void An_event_id_without_the_EV_prefix_is_refused() {
        // Arrange
        Peril     anyPeril      = Any.Enum<Peril>().Generate();
        Footprint anyFootprint  = Arbitrary.Footprint().Generate();
        double    anyAnnualRate = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(CatastropheEvent.MaxAnnualRate).Generate();

        // Act
        Exception? error = Record.Exception(() => new CatastropheEvent("HU-0042", anyPeril, anyFootprint, anyAnnualRate));

        // Assert
        Assert.IsType<FormatException>(error);
    }

}
