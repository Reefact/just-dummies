using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Recipes;

using JustDummies;

namespace CatastropheLosses.Tests.Expert.Tests;

public sealed class CatastropheModelTests {

    private static readonly GeoPoint Landfall = new(25.8, -80.2);

    [Fact]
    public void An_open_portfolio_is_refused_since_only_a_frozen_snapshot_may_be_simulated() {
        // Arrange
        CatastropheModel anyModel         = Any.CatastropheModel().Generate();
        Portfolio        anyOpenPortfolio = Any.Portfolio().Holding(Exposures.Distinct()).Generate();
        int              anySeed          = Any.Int32().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyModel.Simulate(anyOpenPortfolio, 10, anySeed));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CatastropheModel.MaxYears + 1)]
    public void A_simulation_length_outside_one_to_a_million_years_is_refused(int years) {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Any.Portfolio().Frozen().Generate();
        int              anySeed      = Any.Int32().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyModel.Simulate(anyPortfolio, years, anySeed));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_catalogue_listing_the_same_event_twice_is_refused() {
        // Arrange
        CatastropheEvent anyEvent    = Any.CatastropheEvent().Generate();
        CatastropheEvent anyNamesake = Any.CatastropheEvent().WithId(anyEvent.Id).Generate();
        double           anyVolatility = Any.Double().Between(0.0, CatastropheModel.MaxSeverityVolatility).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new CatastropheModel([anyEvent, anyNamesake], anyVolatility));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Contains(anyEvent.Id, refusal.Message);
    }

    [Fact]
    public void The_same_seed_replays_the_same_simulated_years() {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Any.Portfolio().Frozen().Generate();
        int              anySeed      = Any.Int32().Generate();
        int              anyYears     = Any.Int32().Between(1, 500).Generate();

        // Act
        LossDistribution first  = anyModel.Simulate(anyPortfolio, anyYears, anySeed);
        LossDistribution replay = anyModel.Simulate(anyPortfolio, anyYears, anySeed);

        // Assert
        Assert.Equal(first.AnnualLosses, replay.AnnualLosses);
    }

    [Fact]
    public void A_portfolio_no_footprint_reaches_has_a_loss_free_distribution() {
        // Arrange
        // Every footprint stays within about 2 100 km of (0, 0); every property stands in Siberia.
        AnyCatastropheEvent anyTropicalEvent = Any.CatastropheEvent()
                                                  .WithFootprint(f => f.WithCenter(c => c.WithLatitude(l => l.Between(-10.0, 10.0))
                                                                                          .WithLongitude(l => l.Between(-10.0, 10.0)))
                                                                       .WithRadiusKm(r => r.LessThanOrEqualTo(500.0)));
        List<CatastropheEvent> anyCatalogue = Any.ListOf(anyTropicalEvent).WithCountBetween(1, 6).Distinct(EventIdentity.Comparer).Generate();
        CatastropheModel       anyModel     = Any.CatastropheModel().WithCatalogue(anyCatalogue).Generate();
        Portfolio anySiberianPortfolio = Any.Portfolio()
                                            .FrozenWith(Exposures.Distinct().WithMaxCount(5))
                                            .Generate();
        int anySeed = Any.Int32().Generate();

        // Act
        LossDistribution distribution = anyModel.Simulate(anySiberianPortfolio, 200, anySeed);

        // Assert
        Assert.Equal(0m, distribution.Maximum);
    }

    [Fact]
    public void One_occurrence_strikes_every_exposure_with_the_same_severity_so_a_twin_exposure_doubles_every_year() {
        // Arrange
        // Prerequisites, not the point: a frequent event, a volatile severity, a property it reaches.
        CatastropheEvent anyHurricane  = HurricaneOverLandfall(annualRate: 1.0);
        double           anyVolatility = Any.Double().Between(0.5, CatastropheModel.MaxSeverityVolatility).Generate();
        CatastropheModel model         = new([anyHurricane], anyVolatility);
        Exposure         anyExposure   = ExposureAtLandfall(Any.InsuredProperty().Generate().Id);
        Exposure         anyTwin       = ExposureAtLandfall(Any.InsuredProperty().WithId(id => id.DifferentFrom(anyExposure.Property.Id)).Generate().Id);
        Portfolio        anyAlone      = Any.Portfolio().FrozenWith(anyExposure).Generate();
        Portfolio        anyPair       = Any.Portfolio().FrozenWith(anyExposure, anyTwin).Generate();
        int              anySeed       = Any.Int32().Generate();

        // Act
        LossDistribution alone = model.Simulate(anyAlone, 100, anySeed);
        LossDistribution pair  = model.Simulate(anyPair, 100, anySeed);

        // Assert
        Assert.Contains(alone.AnnualLosses, loss => loss > 0m); // the scenario does strike
        Assert.Equal(alone.AnnualLosses.Select(loss => 2 * loss), pair.AnnualLosses);
    }

    [Fact]
    public void Without_severity_volatility_every_year_costs_a_whole_number_of_catalogued_occurrences() {
        // Arrange
        CatastropheEvent anyHurricane = HurricaneOverLandfall(annualRate: 2.0);
        CatastropheModel model        = new([anyHurricane], severityVolatility: 0.0);
        Portfolio        anyPortfolio = Any.Portfolio().FrozenWith(ExposureAtLandfall("PRP-000001")).Generate();
        decimal          occurrence   = anyPortfolio.EventLoss(anyHurricane, severityFactor: 1.0);
        int              anySeed      = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(anyPortfolio, 200, anySeed);

        // Assert
        Assert.Equal(400_000m, occurrence); // 80 % of 500 000, under full-value terms
        Assert.All(distribution.AnnualLosses, loss => Assert.Equal(0m, loss % occurrence));
    }

    [Fact]
    public void Without_severity_volatility_the_average_annual_loss_converges_to_rate_times_occurrence_loss() {
        // Arrange
        CatastropheEvent anyHurricane = HurricaneOverLandfall(annualRate: 0.2);
        CatastropheModel model        = new([anyHurricane], severityVolatility: 0.0);
        Portfolio        anyPortfolio = Any.Portfolio().FrozenWith(ExposureAtLandfall("PRP-000001")).Generate();
        int              anySeed      = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(anyPortfolio, 200_000, anySeed);

        // Assert
        // 0.2 occurrences a year x 400 000 = 80 000; 3 % is six standard errors at 200 000 years.
        Assert.InRange(distribution.Mean, 77_600m, 82_400m);
    }

    private static CatastropheEvent HurricaneOverLandfall(double annualRate) {
        return Any.CatastropheEvent()
                  .WithPeril(Peril.TropicalCyclone)
                  .WithAnnualRate(annualRate)
                  .WithFootprint(f => f.WithCenter(Landfall).WithPeakIntensity(80.0))
                  .Generate();
    }

    private static Exposure ExposureAtLandfall(string propertyId) {
        return Any.Exposure()
                  .WithProperty(p => p.WithId(propertyId)
                                      .WithLocation(Landfall)
                                      .WithInsuredValue(500_000m)
                                      .WithVulnerability(Scenarios.LinearCurve))
                  .WithTerms(Scenarios.FullValueTerms(500_000m))
                  .Generate();
    }

}
