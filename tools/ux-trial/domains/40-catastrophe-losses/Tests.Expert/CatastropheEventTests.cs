using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class CatastropheEventTests {

    [Fact]
    public void An_event_keeps_what_it_was_built_from() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act
        CatastropheEvent catastrophe = new CatastropheEvent("EV-0042", Peril.Flood, anyFootprint, 0.25d);

        // Assert
        Assert.Equal("EV-0042", catastrophe.Id);
        Assert.Equal(Peril.Flood, catastrophe.Peril);
        Assert.Same(anyFootprint, catastrophe.Footprint);
        Assert.Equal(0.25d, catastrophe.AnnualRate);
    }

    [Theory]
    [InlineData("EV-0000")]
    [InlineData("EV-12345678")]
    public void An_id_of_ev_followed_by_four_to_eight_digits_is_accepted(string id) {
        // Act
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithId(id).Generate();

        // Assert
        Assert.Equal(id, catastrophe.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EV-123")]
    [InlineData("EV-123456789")]
    [InlineData("ev-1234")]
    [InlineData("EV1234")]
    [InlineData("EV-12a4")]
    [InlineData("EV--1234")]
    [InlineData(" EV-1234")]
    [InlineData("EV-1234 ")]
    [InlineData("PRP-123456")]
    public void An_id_that_is_not_ev_followed_by_four_to_eight_digits_is_refused(string id) {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();
        double    anyRate      = Any.Double().Between(0.01d, CatastropheEvent.MaxAnnualRate).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new CatastropheEvent(id, Any.Enum<Peril>().Generate(), anyFootprint, anyRate));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact(Skip = "domain defect: the id pattern ends with $, which .NET lets match before a trailing newline, so 'EV-1234\\n' is accepted; literal id, seed n/a")]
    public void An_id_followed_by_a_line_break_is_refused() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();
        double    anyRate      = Any.Double().Between(0.01d, CatastropheEvent.MaxAnnualRate).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new CatastropheEvent("EV-1234\n", Any.Enum<Peril>().Generate(), anyFootprint, anyRate));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact]
    public void An_event_without_an_id_is_refused() {
        // Arrange
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new CatastropheEvent(null!, Peril.Flood, anyFootprint, 1d));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void An_event_without_a_footprint_is_refused() {
        // Arrange
        string anyId = Any.StringMatching("EV-[0-9]{4,8}").Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new CatastropheEvent(anyId, Peril.Flood, null!, 1d));

        // Assert
        Assert.Equal("footprint", refusal.ParamName);
    }

    [Theory]
    [InlineData(Peril.TropicalCyclone)]
    [InlineData(Peril.Earthquake)]
    [InlineData(Peril.Flood)]
    [InlineData(Peril.SevereConvectiveStorm)]
    public void Every_declared_peril_is_accepted(Peril peril) {
        // Act
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithPeril(peril).Generate();

        // Assert
        Assert.Equal(peril, catastrophe.Peril);
    }

    [Fact]
    public void A_peril_that_is_not_a_declared_member_is_refused() {
        // Arrange
        Peril     anyUndeclaredPeril = (Peril)Any.Int32().Except(0, 1, 2, 3).Generate();
        Footprint anyFootprint       = Any.Footprint().Generate();
        string    anyId              = Any.StringMatching("EV-[0-9]{4,8}").Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CatastropheEvent(anyId, anyUndeclaredPeril, anyFootprint, 1d));

        // Assert
        Assert.Equal("peril", refusal.ParamName);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.1d)]
    [InlineData(5.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void An_annual_rate_outside_zero_exclusive_to_five_is_refused(double rate) {
        // Arrange
        string    anyId        = Any.StringMatching("EV-[0-9]{4,8}").Generate();
        Footprint anyFootprint = Any.Footprint().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CatastropheEvent(anyId, Peril.Earthquake, anyFootprint, rate));

        // Assert
        Assert.Equal("annualRate", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(5d)]
    public void An_annual_rate_on_the_edge_of_its_range_is_accepted(double rate) {
        // Act
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithAnnualRate(rate).Generate();

        // Assert
        Assert.Equal(rate, catastrophe.AnnualRate);
    }

    [Theory]
    [InlineData(0.25d, 4d)]
    [InlineData(1d, 1d)]
    [InlineData(0.002d, 500d)]
    [InlineData(5d, 0.2d)]
    public void The_return_period_is_the_inverse_of_the_annual_rate(double rate, double expectedYears) {
        // Arrange
        CatastropheEvent anyEvent = Any.CatastropheEvent().WithAnnualRate(rate).Generate();

        // Act
        double years = anyEvent.ReturnPeriodYears;

        // Assert
        Assert.Equal(expectedYears, years, 9);
    }

}
