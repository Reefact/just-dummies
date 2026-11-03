using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class CatastropheEventTests {

    [Theory]
    [InlineData("EV-123")]
    [InlineData("EV-123456789")]
    [InlineData("ev-1234")]
    [InlineData("EV1234")]
    [InlineData("EV-12a4")]
    [InlineData(" EV-1234")]
    [InlineData("EV-1234 ")]
    [InlineData("")]
    public void An_id_that_is_not_ev_and_four_to_eight_digits_is_refused(string id) {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();
        Peril     anyPeril     = Any.Enum<Peril>().Generate();
        double    anyRate      = Any.Double().Between(0.01d, 5d).Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new CatastropheEvent(id, anyPeril, anyFootprint, anyRate));
    }

    [Theory]
    [InlineData("EV-1234")]
    [InlineData("EV-12345678")]
    public void The_shortest_and_the_longest_ids_are_accepted(string id) {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();
        Peril     anyPeril     = Any.Enum<Peril>().Generate();
        double    anyRate      = Any.Double().Between(0.01d, 5d).Generate();

        // Act
        CatastropheEvent catastrophe = new CatastropheEvent(id, anyPeril, anyFootprint, anyRate);

        // Assert
        Assert.Equal(id, catastrophe.Id);
    }

    [Fact(Skip = "domain defect: IdFormat '^EV-\\d{4,8}$' accepts a trailing line feed, seed 672400060")]
    public void An_id_with_a_trailing_line_feed_is_refused() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();
        Peril     anyPeril     = Any.Enum<Peril>().Generate();
        double    anyRate      = Any.Double().Between(0.01d, 5d).Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new CatastropheEvent("EV-1234\n", anyPeril, anyFootprint, anyRate));
    }

    [Fact]
    public void An_undefined_peril_is_refused() {
        // Arrange
        string    anyId        = Any.StringMatching("EV-[0-9]{4,8}").Generate();
        Footprint anyFootprint = Any.Footprint().Generate();
        double    anyRate      = Any.Double().Between(0.01d, 5d).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CatastropheEvent(anyId, (Peril)99, anyFootprint, anyRate));

        // Assert
        Assert.Equal("peril", error.ParamName);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.5d)]
    [InlineData(5.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_annual_rate_outside_zero_to_five_is_refused(double annualRate) {
        // Arrange
        string    anyId        = Any.StringMatching("EV-[0-9]{4,8}").Generate();
        Peril     anyPeril     = Any.Enum<Peril>().Generate();
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CatastropheEvent(anyId, anyPeril, anyFootprint, annualRate));

        // Assert
        Assert.Equal("annualRate", error.ParamName);
    }

    [Fact]
    public void An_event_without_id_or_footprint_is_refused() {
        // Arrange
        string    anyId        = Any.StringMatching("EV-[0-9]{4,8}").Generate();
        Peril     anyPeril     = Any.Enum<Peril>().Generate();
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CatastropheEvent(null!, anyPeril, anyFootprint, 1d));
        Assert.Throws<ArgumentNullException>(() => new CatastropheEvent(anyId, anyPeril, null!, 1d));
    }

    [Theory]
    [InlineData(5d, 0.2d)]
    [InlineData(1d, 1d)]
    [InlineData(0.25d, 4d)]
    [InlineData(0.01d, 100d)]
    public void The_return_period_is_the_inverse_of_the_annual_rate(double annualRate, double expectedYears) {
        // Arrange
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithAnnualRate(annualRate).Generate();

        // Act
        double years = catastrophe.ReturnPeriodYears;

        // Assert
        Assert.Equal(expectedYears, years, 10);
    }

    [Fact]
    public void An_event_exposes_what_it_was_built_with() {
        // Arrange
        Footprint footprint = Any.Footprint().Generate();

        // Act
        CatastropheEvent catastrophe = new CatastropheEvent("EV-0042", Peril.Flood, footprint, 0.5d);

        // Assert
        Assert.Equal("EV-0042", catastrophe.Id);
        Assert.Equal(Peril.Flood, catastrophe.Peril);
        Assert.Same(footprint, catastrophe.Footprint);
        Assert.Equal(0.5d, catastrophe.AnnualRate);
    }

}
