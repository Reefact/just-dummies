using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class PortfolioTests {

    [Fact]
    public void A_property_cannot_be_exposed_twice_in_one_portfolio() {
        // Arrange
        Portfolio portfolio   = Any.Portfolio().Generate();
        Exposure  anyExposure = Any.Exposure().Generate();
        Exposure  anyOtherExposureOnTheSameProperty = Any.Exposure()
                                                         .WithProperty(p => p.WithId(anyExposure.Property.Id))
                                                         .Generate();
        portfolio.Add(anyExposure);

        // Act
        Exception? error = Record.Exception(() => portfolio.Add(anyOtherExposureOnTheSameProperty));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Single(portfolio.Exposures);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_any_new_exposure() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Any.Exposure().Generate());
        portfolio.Freeze();
        Exposure anyLateExposure = Any.Exposure().Generate();

        // Act
        Exception? error = Record.Exception(() => portfolio.Add(anyLateExposure));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void An_empty_portfolio_cannot_be_frozen() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();

        // Act
        Exception? error = Record.Exception(() => portfolio.Freeze());

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.False(portfolio.IsFrozen);
    }

    [Fact]
    public void A_portfolio_cannot_be_frozen_twice() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Any.Exposure().Generate());
        portfolio.Freeze();

        // Act
        Exception? error = Record.Exception(() => portfolio.Freeze());

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Totals_add_up_the_insured_values_and_the_limits_of_every_exposure() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Any.Exposure()
                         .WithProperty(p => p.WithInsuredValue(400_000m))
                         .WithTerms(t => t.WithLimit(300_000m))
                         .Generate());
        portfolio.Add(Any.Exposure()
                         .WithProperty(p => p.WithInsuredValue(250_000m))
                         .WithTerms(t => t.WithLimit(200_000m))
                         .Generate());

        // Act
        decimal totalInsuredValue = portfolio.TotalInsuredValue;
        decimal totalLimit        = portfolio.TotalLimit;

        // Assert
        Assert.Equal(650_000m, totalInsuredValue);
        Assert.Equal(500_000m, totalLimit);
    }

    [Fact]
    public void One_occurrence_hits_every_exposure_under_its_footprint_with_the_same_severity() {
        // Arrange
        CatastropheEvent anyCyclone = Any.CatastropheEvent()
                                         .WithFootprint(f => f.WithPeakIntensity(50.0))
                                         .Generate();
        GeoPoint  eye       = anyCyclone.Footprint.Center;
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(ExposureAt(eye, insuredValue: 200_000m));
        portfolio.Add(ExposureAt(eye, insuredValue: 600_000m));

        // Act
        decimal eventLoss = portfolio.EventLoss(anyCyclone, severityFactor: 1.2);

        // Assert
        // intensity 50 x 1.2 = 60 -> 60 % of each value, no deductible, limits not reached
        Assert.Equal(480_000m, eventLoss);
    }

    private static Exposure ExposureAt(GeoPoint location, decimal insuredValue) {
        return Any.Exposure()
                  .WithProperty(p => p.WithLocation(location)
                                      .WithInsuredValue(insuredValue)
                                      .WithVulnerability(Curves.Linear()))
                  .WithTerms(new PolicyTerms(deductible: 0m, limit: insuredValue))
                  .Generate();
    }

}
