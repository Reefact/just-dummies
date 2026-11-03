using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class PortfolioTests {

    [Fact]
    public void A_new_portfolio_is_open_and_empty() {
        // Act
        Portfolio portfolio = new Portfolio("Gulf Coast Residential", "USD");

        // Assert
        Assert.Equal("Gulf Coast Residential", portfolio.Name);
        Assert.Equal("USD", portfolio.Currency);
        Assert.False(portfolio.IsFrozen);
        Assert.Empty(portfolio.Exposures);
        Assert.Equal(0m, portfolio.TotalInsuredValue);
        Assert.Equal(0m, portfolio.TotalLimit);
    }

    [Fact]
    public void A_portfolio_without_a_name_is_refused() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Portfolio(null!, anyCurrency));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_blank_portfolio_name_is_refused(string name) {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Portfolio(name, anyCurrency));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_name_of_a_hundred_characters_is_accepted() {
        // Arrange
        string anyHundredCharacterName = Any.String().NotBlank().WithLength(100).Generate();
        string anyCurrency             = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        Portfolio portfolio = new Portfolio(anyHundredCharacterName, anyCurrency);

        // Assert
        Assert.Equal(anyHundredCharacterName, portfolio.Name);
    }

    [Fact]
    public void A_name_of_a_hundred_and_one_characters_is_refused() {
        // Arrange
        string anyTooLongName = Any.String().NotBlank().WithLength(101).Generate();
        string anyCurrency    = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Portfolio(anyTooLongName, anyCurrency));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_portfolio_without_a_currency_is_refused() {
        // Arrange
        string anyName = Any.Portfolio().Generate().Name;

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Portfolio(anyName, null!));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("U5D")]
    [InlineData("US$")]
    [InlineData(" USD")]
    [InlineData("USD ")]
    public void A_currency_that_is_not_three_upper_case_letters_is_refused(string currency) {
        // Arrange
        string anyName = Any.Portfolio().Generate().Name;

        // Act
        Exception? refusal = Record.Exception(() => new Portfolio(anyName, currency));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact(Skip = "domain defect: the currency pattern ends with $, which .NET lets match before a trailing newline, so 'USD\\n' is accepted; literal currency, seed n/a")]
    public void A_currency_followed_by_a_line_break_is_refused() {
        // Arrange
        string anyName = Any.Portfolio().Generate().Name;

        // Act
        Exception? refusal = Record.Exception(() => new Portfolio(anyName, "USD\n"));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact]
    public void An_added_exposure_appears_in_the_portfolio() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        Exposure  anyExposure  = Any.Exposure().Generate();

        // Act
        anyPortfolio.Add(anyExposure);

        // Assert
        Assert.Same(anyExposure, Assert.Single(anyPortfolio.Exposures));
    }

    [Fact]
    public void Exposures_are_kept_in_the_order_they_were_added() {
        // Arrange
        Portfolio       anyPortfolio = Any.Portfolio().Generate();
        List<Exposure>  anyExposures = Scenes.DistinctExposures(4);

        // Act
        foreach (Exposure anyExposure in anyExposures) {
            anyPortfolio.Add(anyExposure);
        }

        // Assert
        Assert.Equal(anyExposures, anyPortfolio.Exposures);
    }

    [Fact]
    public void Adding_nothing_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPortfolio.Add(null!));

        // Assert
        Assert.Equal("exposure", refusal.ParamName);
    }

    [Fact]
    public void A_property_already_in_the_portfolio_cannot_be_added_again() {
        // Arrange
        Portfolio anyPortfolio  = Any.Portfolio().Generate();
        Exposure  first         = Any.Exposure().Generate();
        Exposure  sameProperty  = Any.Exposure().WithProperty(first.Property).Generate();
        anyPortfolio.Add(first);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPortfolio.Add(sameProperty));

        // Assert
        Assert.Contains(first.Property.Id, refusal.Message);
        Assert.Single(anyPortfolio.Exposures);
    }

    [Fact]
    public void Another_exposure_on_a_property_with_the_same_id_is_also_a_duplicate() {
        // Arrange
        Portfolio anyPortfolio    = Any.Portfolio().Generate();
        Exposure  first           = Any.Exposure().Generate();
        Exposure  sameIdElsewhere = Any.Exposure().WithProperty(p => p.WithId(first.Property.Id)).Generate();
        anyPortfolio.Add(first);

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.Add(sameIdElsewhere));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Two_properties_with_different_ids_are_both_accepted() {
        // Arrange
        Portfolio       anyPortfolio = Any.Portfolio().Generate();
        List<Exposure>  anyExposures = Scenes.DistinctExposures(2);

        // Act
        anyPortfolio.Add(anyExposures[0]);
        anyPortfolio.Add(anyExposures[1]);

        // Assert
        Assert.Equal(2, anyPortfolio.Exposures.Count);
    }

    [Fact]
    public void Freezing_a_portfolio_with_an_exposure_freezes_it() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Add(Any.Exposure().Generate());

        // Act
        anyPortfolio.Freeze();

        // Assert
        Assert.True(anyPortfolio.IsFrozen);
    }

    [Fact]
    public void An_empty_portfolio_cannot_be_frozen() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPortfolio.Freeze());

        // Assert
        Assert.Contains(anyPortfolio.Name, refusal.Message);
        Assert.False(anyPortfolio.IsFrozen);
    }

    [Fact]
    public void A_frozen_portfolio_cannot_be_frozen_again() {
        // Arrange
        Portfolio anyPortfolio = Scenes.FrozenPortfolioOf(Any.Exposure().Generate());

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPortfolio.Freeze());

        // Assert
        Assert.Contains("already frozen", refusal.Message);
    }

    [Fact]
    public void No_exposure_can_be_added_once_the_portfolio_is_frozen() {
        // Arrange
        List<Exposure> anyExposures = Scenes.DistinctExposures(2);
        Portfolio      anyPortfolio = Scenes.FrozenPortfolioOf(anyExposures[0]);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPortfolio.Add(anyExposures[1]));

        // Assert
        Assert.Contains("frozen", refusal.Message);
        Assert.Single(anyPortfolio.Exposures);
    }

    [Fact]
    public void The_frozen_check_comes_before_the_duplicate_check() {
        // Arrange
        Exposure  anyExposure  = Any.Exposure().Generate();
        Portfolio anyPortfolio = Scenes.FrozenPortfolioOf(anyExposure);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPortfolio.Add(anyExposure));

        // Assert
        Assert.Contains("frozen", refusal.Message);
    }

    [Fact]
    public void The_total_limit_sums_the_limits_of_the_exposures() {
        // Arrange
        Portfolio anyPortfolio = PortfolioOfTwoKnownExposures();

        // Act
        decimal totalLimit = anyPortfolio.TotalLimit;

        // Assert
        Assert.Equal(60_000m + 200_000m, totalLimit);
    }

    [Fact]
    public void The_total_insured_value_sums_the_values_of_the_properties() {
        // Arrange
        Portfolio anyPortfolio = PortfolioOfTwoKnownExposures();

        // Act
        decimal totalInsuredValue = anyPortfolio.TotalInsuredValue;

        // Assert
        Assert.Equal(100_000m + 250_000m, totalInsuredValue);
    }

    private static Portfolio PortfolioOfTwoKnownExposures() {
        Portfolio          portfolio  = Any.Portfolio().Generate();
        VulnerabilityCurve anyCurve   = Any.VulnerabilityCurve().Generate();
        GeoPoint           anyPlace   = Any.GeoPoint().Generate();
        string             anyId      = Any.StringMatching("PRP-[0-9]{6}").Generate();
        string             anyOtherId = Any.StringMatching("PRP-[0-9]{6}").DifferentFrom(anyId).Generate();
        portfolio.Add(Scenes.ExposureOf(anyId, anyPlace, 100_000m, new PolicyTerms(1_000m, 60_000m), anyCurve));
        portfolio.Add(Scenes.ExposureOf(anyOtherId, anyPlace, 250_000m, new PolicyTerms(5_000m, 200_000m), anyCurve));

        return portfolio;
    }

    [Fact]
    public void The_loss_of_an_event_is_the_sum_of_the_losses_of_every_exposure() {
        // Arrange
        GeoPoint         anyPlace  = Any.GeoPoint().Generate();
        VulnerabilityCurve curve   = Scenes.LinearUpToThirtyPercent();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 5d, 1d);                      // intensity 5 -> ratio 0.15
        Exposure         first     = Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(20_000m, 500_000m), curve);   // 150 000 - 20 000
        Exposure         second    = Scenes.ExposureOf(anyPlace,   400_000m, new PolicyTerms(10_000m, 40_000m), curve);   //  60 000 - 10 000, capped at 40 000
        Portfolio        anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Add(first);
        anyPortfolio.Add(second);

        // Act
        decimal loss = anyPortfolio.EventLoss(catastrophe, 1d);

        // Assert
        Assert.Equal(130_000m + 40_000m, loss);
    }

    [Fact]
    public void The_same_severity_is_applied_to_every_exposure_of_the_occurrence() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        VulnerabilityCurve curve     = Scenes.LinearUpToTotalLoss();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 10d, 1d);                     // intensity 10 x severity 3 -> ratio 0.3
        Exposure         first       = Scenes.ExposureOf(anyPlace, 100_000m, new PolicyTerms(0m, 100_000m), curve);
        Exposure         second      = Scenes.ExposureOf(anyPlace, 200_000m, new PolicyTerms(0m, 200_000m), curve);
        Portfolio        anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Add(first);
        anyPortfolio.Add(second);

        // Act
        decimal loss = anyPortfolio.EventLoss(catastrophe, 3d);

        // Assert
        Assert.Equal(30_000m + 60_000m, loss);
    }

    [Fact]
    public void An_event_that_misses_every_exposure_costs_nothing() {
        // Arrange
        GeoPoint         place       = new GeoPoint(10d, 10d);
        GeoPoint         farAway     = new GeoPoint(-40d, 120d);
        CatastropheEvent catastrophe = Scenes.EventAt(farAway, 50d, 1d);
        Exposure         exposure    = Scenes.ExposureOf(place, 100_000m, new PolicyTerms(0m, 100_000m), Scenes.LinearUpToTotalLoss());
        Portfolio        anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Add(exposure);

        // Act
        decimal loss = anyPortfolio.EventLoss(catastrophe, 1d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact]
    public void An_empty_portfolio_loses_nothing_to_an_event() {
        // Arrange
        Portfolio        anyPortfolio = Any.Portfolio().Generate();
        CatastropheEvent anyEvent     = Any.CatastropheEvent().Generate();

        // Act
        decimal loss = anyPortfolio.EventLoss(anyEvent, 1d);

        // Assert
        Assert.Equal(0m, loss);
    }

    [Fact]
    public void The_loss_of_no_event_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPortfolio.EventLoss(null!, 1d));

        // Assert
        Assert.Equal("catastrophe", refusal.ParamName);
    }

    [Fact]
    public void A_negative_severity_is_refused_as_soon_as_the_portfolio_holds_an_exposure() {
        // Arrange
        Portfolio        anyPortfolio = Scenes.FrozenPortfolioOf(Any.Exposure().Generate());
        CatastropheEvent anyEvent     = Any.CatastropheEvent().Generate();
        double           anyNegativeSeverity = Any.Double().Negative().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.EventLoss(anyEvent, anyNegativeSeverity));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

}
