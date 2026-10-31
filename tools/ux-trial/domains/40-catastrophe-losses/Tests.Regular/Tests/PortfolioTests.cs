namespace CatastropheLosses.Tests.Regular.Tests;

public sealed class PortfolioTests {

    [Fact]
    public void The_same_property_cannot_be_exposed_twice() {
        // Arrange
        Portfolio       anyPortfolio = Arbitrary.Portfolio().Generate();
        InsuredProperty anyProperty  = Arbitrary.InsuredProperty().WithInsuredValue(1_000_000m).Generate();
        anyPortfolio.Add(Arbitrary.Exposure().WithProperty(anyProperty).Generate());
        Exposure sameProperty = Arbitrary.Exposure().WithProperty(anyProperty).Generate();

        // Act
        Exception? error = Record.Exception(() => anyPortfolio.Add(sameProperty));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Single(anyPortfolio.Exposures);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_new_exposures() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.Add(Arbitrary.Exposure().Generate());
        anyPortfolio.Freeze();
        Exposure anyExposure = Arbitrary.Exposure().Generate();

        // Act
        Exception? error = Record.Exception(() => anyPortfolio.Add(anyExposure));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void An_empty_portfolio_cannot_be_frozen() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();

        // Act
        Exception? error = Record.Exception(anyPortfolio.Freeze);

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.False(anyPortfolio.IsFrozen);
    }

    [Fact]
    public void A_currency_that_is_not_an_ISO_code_is_refused() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act
        Exception? error = Record.Exception(() => new Portfolio(anyName, "eur"));

        // Assert
        Assert.IsType<FormatException>(error);
    }

    [Fact]
    public void Totals_add_up_every_exposure() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.Add(Arbitrary.Exposure().WithProperty(p => p.WithId("PRP-000001").WithInsuredValue(300_000m))
                                             .WithTerms(t => t.WithLimit(200_000m)).Generate());
        anyPortfolio.Add(Arbitrary.Exposure().WithProperty(p => p.WithId("PRP-000002").WithInsuredValue(700_000m))
                                             .WithTerms(t => t.WithLimit(50_000m)).Generate());

        // Act
        decimal totalInsuredValue = anyPortfolio.TotalInsuredValue;
        decimal totalLimit        = anyPortfolio.TotalLimit;

        // Assert
        Assert.Equal(1_000_000m, totalInsuredValue);
        Assert.Equal(250_000m, totalLimit);
    }

    [Fact]
    public void One_occurrence_hits_every_exposure_in_its_footprint_with_the_same_severity() {
        // Arrange
        GeoPoint  anySite      = Arbitrary.GeoPoint().Generate();
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        foreach (string id in new[] { "PRP-000001", "PRP-000002", "PRP-000003" }) {
            anyPortfolio.Add(Arbitrary.Exposure()
                                      .WithProperty(p => p.WithId(id).WithLocation(anySite)
                                                          .WithInsuredValue(200_000m).WithVulnerability(Curves.Linear()))
                                      .WithTerms(new PolicyTerms(deductible: 10_000m, limit: 150_000m))
                                      .Generate());
        }

        CatastropheEvent anyEvent = Arbitrary.CatastropheEvent()
                                              .WithFootprint(f => f.WithCenter(anySite).WithPeakIntensity(25.0))
                                              .Generate();

        // Act
        decimal eventLoss = anyPortfolio.EventLoss(anyEvent, 2.0);

        // Assert — each property loses 90 000 after its deductible.
        Assert.Equal(270_000m, eventLoss);
    }

}
