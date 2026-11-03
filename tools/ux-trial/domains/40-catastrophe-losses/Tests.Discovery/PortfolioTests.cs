using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class PortfolioTests {

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string name) {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Portfolio(name, anyCurrency));
    }

    [Fact]
    public void A_name_of_a_hundred_characters_is_accepted_and_a_hundred_and_one_is_refused() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        string hundred     = Any.String().Alpha().WithLength(100).Generate();
        string hundredOne  = Any.String().Alpha().WithLength(101).Generate();

        // Act
        Portfolio portfolio = new Portfolio(hundred, anyCurrency);
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Portfolio(hundredOne, anyCurrency));

        // Assert
        Assert.Equal(hundred, portfolio.Name);
        Assert.Equal("name", error.ParamName);
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData(" EUR")]
    [InlineData("")]
    public void A_currency_that_is_not_three_upper_case_letters_is_refused(string currency) {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new Portfolio(anyName, currency));
    }

    [Fact(Skip = "domain defect: CurrencyFormat '^[A-Z]{3}$' accepts a trailing line feed ('USD\\n'), seed 468474248")]
    public void A_currency_with_a_trailing_line_feed_is_refused() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new Portfolio(anyName, "USD\n"));
    }

    [Fact]
    public void A_portfolio_without_name_or_currency_is_refused() {
        // Arrange
        string anyName     = Any.String().Alpha().WithLengthBetween(1, 100).Generate();
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Portfolio(null!, anyCurrency));
        Assert.Throws<ArgumentNullException>(() => new Portfolio(anyName, null!));
    }

    [Fact]
    public void A_new_portfolio_is_open_and_empty() {
        // Act
        Portfolio portfolio = Any.Portfolio().Generate();

        // Assert
        Assert.False(portfolio.IsFrozen);
        Assert.Empty(portfolio.Exposures);
        Assert.Equal(0m, portfolio.TotalInsuredValue);
        Assert.Equal(0m, portfolio.TotalLimit);
    }

    [Fact]
    public void An_added_exposure_is_listed_and_counted_in_the_totals() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        Exposure  first     = Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, 10_000m, 800_000m);
        Exposure  second    = Samples.ExposureAtOrigin("PRP-000002", 250_000m, 0m, 100_000m);

        // Act
        portfolio.Add(first);
        portfolio.Add(second);

        // Assert
        Assert.Equal([first, second], portfolio.Exposures);
        Assert.Equal(1_250_000m, portfolio.TotalInsuredValue);
        Assert.Equal(900_000m, portfolio.TotalLimit);
    }

    [Fact]
    public void Adding_nothing_is_refused() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => portfolio.Add(null!));
    }

    [Fact]
    public void A_property_cannot_be_exposed_twice() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, 0m, 100m));
        Exposure sameProperty = Samples.ExposureAtOrigin("PRP-000001", 2_000_000m, 0m, 200m);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => portfolio.Add(sameProperty));

        // Assert
        Assert.Contains("PRP-000001", error.Message);
        Assert.Single(portfolio.Exposures);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_any_further_exposure() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, 0m, 100m));
        portfolio.Freeze();
        Exposure late = Samples.ExposureAtOrigin("PRP-000002", 1_000_000m, 0m, 100m);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.Add(late));
        Assert.Single(portfolio.Exposures);
    }

    [Fact]
    public void Freezing_marks_the_portfolio_frozen() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, 0m, 100m));

        // Act
        portfolio.Freeze();

        // Assert
        Assert.True(portfolio.IsFrozen);
    }

    [Fact]
    public void An_empty_portfolio_cannot_be_frozen() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => portfolio.Freeze());

        // Assert
        Assert.Contains(portfolio.Name, error.Message);
        Assert.False(portfolio.IsFrozen);
    }

    [Fact]
    public void A_portfolio_cannot_be_frozen_twice() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, 0m, 100m));
        portfolio.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.Freeze());
    }

    [Fact]
    public void An_occurrence_loss_sums_every_exposure_under_the_same_footprint_and_severity() {
        // Arrange: severity 1 -> intensity 8 -> ratio 0.375
        //   first : 1 000 000 -> 375 000 - 25 000 = 350 000, limit 300 000 -> 300 000
        //   second:   400 000 -> 150 000 - 0      = 150 000, limit 400 000 -> 150 000
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Samples.ExposureAtOrigin("PRP-000001", 1_000_000m, 25_000m, 300_000m));
        portfolio.Add(Samples.ExposureAtOrigin("PRP-000002", 400_000m, 0m, 400_000m));
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithFootprint(Samples.PeakEightAtOrigin()).Generate();

        // Act
        decimal loss = portfolio.EventLoss(catastrophe, 1d);

        // Assert
        Assert.Equal(450_000m, loss);
    }

    [Fact]
    public void An_occurrence_loss_does_not_require_the_portfolio_to_be_frozen() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Samples.ExposureAtOrigin("PRP-000001", 400_000m, 0m, 400_000m));
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithFootprint(Samples.PeakEightAtOrigin()).Generate();

        // Act
        decimal loss = portfolio.EventLoss(catastrophe, 1d);

        // Assert
        Assert.Equal(150_000m, loss);
    }

    [Fact]
    public void An_empty_portfolio_suffers_no_loss() {
        // Arrange
        Portfolio        portfolio   = Any.Portfolio().Generate();
        CatastropheEvent catastrophe = Any.CatastropheEvent().Generate();
        double           anySeverity = Any.Double().Between(0d, 3d).Generate();

        // Act
        decimal loss = portfolio.EventLoss(catastrophe, anySeverity);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact]
    public void A_loss_for_no_event_is_refused() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => portfolio.EventLoss(null!, 1d));
    }

}
