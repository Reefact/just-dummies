using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class CatastropheModelTests {

    [Fact]
    public void An_open_portfolio_cannot_be_simulated() {
        // Arrange
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        Portfolio        open     = Any.Portfolio().Generate();
        open.Add(Any.Exposure().Generate());
        int anySeed = Any.Int32().Generate();

        // Act
        Exception? error = Record.Exception(() => anyModel.Simulate(open, years: 10, anySeed));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_catalogue_listing_the_same_event_twice_is_refused() {
        // Arrange
        CatastropheEvent anyEvent      = Any.CatastropheEvent().Generate();
        CatastropheEvent anyHomonym    = Any.CatastropheEvent().WithId(anyEvent.Id).Generate();
        double           anyVolatility = Any.Double().Between(0.0, CatastropheModel.MaxSeverityVolatility).Generate();

        // Act
        Exception? error = Record.Exception(() => new CatastropheModel([anyEvent, anyHomonym], anyVolatility));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_simulation_yields_one_annual_loss_per_simulated_year() {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = FrozenPortfolioOf(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        // Act
        LossDistribution distribution = anyModel.Simulate(anyPortfolio, years: 250, anySeed);

        // Assert
        Assert.Equal(250, distribution.Years);
    }

    [Fact]
    public void The_same_seed_replays_the_same_simulated_years() {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = FrozenPortfolioOf(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();
        int              anyYears     = Any.Int32().Between(1, 500).Generate();

        // Act
        LossDistribution first  = anyModel.Simulate(anyPortfolio, anyYears, anySeed);
        LossDistribution replay = anyModel.Simulate(anyPortfolio, anyYears, anySeed);

        // Assert
        Assert.Equal(first.AnnualLosses, replay.AnnualLosses);
    }

    [Fact]
    public void A_portfolio_outside_every_footprint_never_loses_anything() {
        // Arrange
        // A footprint spans at most 2 000 km; the antipode of its centre lies ~20 000 km away.
        CatastropheEvent anyEvent = Any.CatastropheEvent()
                                       .WithFootprint(f => f.WithCenter(new GeoPoint(0.0, 0.0)))
                                       .Generate();
        CatastropheModel model = Any.CatastropheModel().WithCatalogue([anyEvent]).Generate();
        Portfolio portfolio = FrozenPortfolioOf(Any.Exposure().WithProperty(p => p.WithLocation(new GeoPoint(0.0, 180.0))).Generate());
        int       anySeed   = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, years: 1_000, anySeed);

        // Assert
        Assert.Equal(0m, distribution.Maximum);
    }

    [Fact]
    public void Without_severity_volatility_every_year_loses_a_whole_number_of_catalogued_events() {
        // Arrange
        CatastropheEvent anyEvent = Any.CatastropheEvent().WithFootprint(f => f.WithPeakIntensity(50.0)).Generate();
        CatastropheModel model    = new CatastropheModel([anyEvent], severityVolatility: 0.0);
        Portfolio portfolio = FrozenPortfolioOf(ExposureAt(anyEvent.Footprint.Center, insuredValue: 300_000m));
        decimal   oneEventLoss = portfolio.EventLoss(anyEvent, severityFactor: 1.0);
        int       anySeed      = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, years: 2_000, anySeed);

        // Assert
        Assert.Equal(150_000m, oneEventLoss);
        Assert.All(distribution.AnnualLosses, loss => Assert.Equal(0m, loss % 150_000m));
    }

    [Fact]
    public void Exposures_struck_by_one_occurrence_share_its_severity() {
        // Arrange
        // Two identical buildings side by side: if each drew its own severity, their losses would
        // differ year by year; sharing it, the pair loses exactly twice what one of them loses.
        CatastropheEvent anyEvent = Any.CatastropheEvent().WithFootprint(f => f.WithPeakIntensity(50.0)).Generate();
        CatastropheModel model    = new CatastropheModel([anyEvent], severityVolatility: 0.8);
        GeoPoint         site     = anyEvent.Footprint.Center;
        string[] anyIds = Any.SetOf(Any.InsuredProperty().As(property => property.Id)).WithCount(2).Generate().ToArray();
        Portfolio alone = FrozenPortfolioOf(ExposureAt(site, insuredValue: 300_000m, anyIds[0]));
        Portfolio pair  = FrozenPortfolioOf(ExposureAt(site, insuredValue: 300_000m, anyIds[0]),
                                            ExposureAt(site, insuredValue: 300_000m, anyIds[1]));
        int anySeed = Any.Int32().Generate();

        // Act
        LossDistribution aloneLosses = model.Simulate(alone, years: 500, anySeed);
        LossDistribution pairLosses  = model.Simulate(pair, years: 500, anySeed);

        // Assert
        Assert.Equal(aloneLosses.AnnualLosses.Select(loss => loss * 2m), pairLosses.AnnualLosses);
    }

    [Fact]
    public void The_average_annual_loss_converges_to_the_rate_times_the_event_loss() {
        // Arrange
        CatastropheEvent anyEvent = Any.CatastropheEvent()
                                       .WithFootprint(f => f.WithPeakIntensity(50.0))
                                       .WithAnnualRate(0.5)
                                       .Generate();
        CatastropheModel model     = new CatastropheModel([anyEvent], severityVolatility: 0.0);
        Portfolio        portfolio = FrozenPortfolioOf(ExposureAt(anyEvent.Footprint.Center, insuredValue: 300_000m));
        int              anySeed   = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, years: 100_000, anySeed);

        // Assert
        // 0.5 x 150 000 = 75 000; 100 000 years put the standard error near 0.5 %.
        Assert.InRange(distribution.Mean, 72_750m, 77_250m);
    }

    private static Portfolio FrozenPortfolioOf(params Exposure[] exposures) {
        Portfolio portfolio = Any.Portfolio().Generate();
        foreach (Exposure exposure in exposures) {
            portfolio.Add(exposure);
        }

        portfolio.Freeze();

        return portfolio;
    }

    private static Exposure ExposureAt(GeoPoint location, decimal insuredValue, string? id = null) {
        AnyExposure exposure = Any.Exposure()
                                  .WithProperty(p => {
                                      AnyInsuredProperty property = p.WithLocation(location)
                                                                     .WithInsuredValue(insuredValue)
                                                                     .WithVulnerability(Curves.Linear());
                                      return id is null ? property : property.WithId(id);
                                  })
                                  .WithTerms(new PolicyTerms(deductible: 0m, limit: insuredValue));

        return exposure.Generate();
    }

}
