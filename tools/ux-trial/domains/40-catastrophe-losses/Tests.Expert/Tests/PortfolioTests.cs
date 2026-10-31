using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Recipes;

using JustDummies;

namespace CatastropheLosses.Tests.Expert.Tests;

public sealed class PortfolioTests {

    [Fact]
    public void A_property_cannot_be_exposed_twice_in_one_portfolio() {
        // Arrange
        Exposure  anyExposure      = Any.Exposure().Generate();
        Exposure  anySameProperty  = Any.Exposure().WithProperty(p => p.WithId(anyExposure.Property.Id)).Generate();
        Portfolio anyOpenPortfolio = Any.Portfolio().Generate();
        anyOpenPortfolio.Add(anyExposure);

        // Act
        Exception? refusal = Record.Exception(() => anyOpenPortfolio.Add(anySameProperty));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Single(anyOpenPortfolio.Exposures);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_any_new_exposure() {
        // Arrange
        Portfolio anyFrozenPortfolio = Any.Portfolio().Frozen().Generate();
        Exposure  anyLateExposure    = Any.Exposure().Generate();
        int       exposuresAtFreeze  = anyFrozenPortfolio.Exposures.Count;

        // Act
        Exception? refusal = Record.Exception(() => anyFrozenPortfolio.Add(anyLateExposure));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(exposuresAtFreeze, anyFrozenPortfolio.Exposures.Count);
    }

    [Fact]
    public void An_empty_portfolio_cannot_be_frozen_since_there_is_nothing_to_analyse() {
        // Arrange
        Portfolio anyEmptyPortfolio = Any.Portfolio().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyEmptyPortfolio.Freeze());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.False(anyEmptyPortfolio.IsFrozen);
    }

    [Fact]
    public void A_portfolio_cannot_be_frozen_twice() {
        // Arrange
        Portfolio anyFrozenPortfolio = Any.Portfolio().Frozen().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyFrozenPortfolio.Freeze());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Totals_add_up_the_insured_values_and_the_limits_of_every_exposure() {
        // Arrange
        Exposure  anyHouse     = Any.Exposure().WithProperty(p => p.WithInsuredValue(300_000m)).WithTerms(t => t.WithLimit(100_000m)).Generate();
        Exposure  anyWarehouse = Any.Exposure().WithProperty(p => p.WithInsuredValue(450_000m)).WithTerms(t => t.WithLimit(200_000m)).Generate();
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Add(anyHouse);
        anyPortfolio.Add(anyWarehouse);

        // Act
        decimal totalInsuredValue = anyPortfolio.TotalInsuredValue;
        decimal totalLimit        = anyPortfolio.TotalLimit;

        // Assert
        Assert.Equal(750_000m, totalInsuredValue);
        Assert.Equal(300_000m, totalLimit);
    }

    [Fact]
    public void An_occurrence_loss_sums_the_insured_loss_of_every_exposure_it_reaches() {
        // Arrange
        GeoPoint         ground       = new(18.0, -66.0);
        CatastropheEvent anyCyclone   = Any.CatastropheEvent()
                                           .WithFootprint(f => f.WithCenter(ground).WithRadiusKm(200.0).WithPeakIntensity(50.0))
                                           .Generate();
        Exposure         anyAtGround  = ExposureAt(ground, 200_000m);
        Exposure         anyAlsoThere = ExposureAt(ground, 400_000m);
        Exposure         anyFarAway   = ExposureAt(new GeoPoint(48.0, 2.0), 900_000m);
        Portfolio        anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Add(anyAtGround);
        anyPortfolio.Add(anyAlsoThere);
        anyPortfolio.Add(anyFarAway);

        // Act
        decimal loss = anyPortfolio.EventLoss(anyCyclone, severityFactor: 1.0);

        // Assert
        Assert.Equal(300_000m, loss); // half of 200 000 and of 400 000; nothing in Paris
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("EURO")]
    [InlineData("€")]
    public void A_currency_that_is_not_three_upper_case_letters_is_refused(string malformedCurrency) {
        // Arrange
        AnyPortfolio anyPortfolioInMalformedCurrency = Any.Portfolio().WithCurrency(malformedCurrency);

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolioInMalformedCurrency.Generate());

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    private static Exposure ExposureAt(GeoPoint location, decimal insuredValue) {
        return Any.Exposure()
                  .WithProperty(p => p.WithLocation(location).WithInsuredValue(insuredValue).WithVulnerability(Scenarios.LinearCurve))
                  .WithTerms(Scenarios.FullValueTerms(insuredValue))
                  .Generate();
    }

}
