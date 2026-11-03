using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;
using Xunit;

namespace CatastropheLosses.Tests.Expert;

public sealed class CatastropheModelTests {

    [Fact]
    public void A_model_keeps_its_catalogue_and_its_volatility() {
        // Arrange
        List<CatastropheEvent> anyCatalogue = Any.ListOf(Any.CatastropheEvent()).WithCount(3).DistinctBy(e => e.Id).Generate();

        // Act
        CatastropheModel model = new CatastropheModel(anyCatalogue, 0.75d);

        // Assert
        Assert.Equal(anyCatalogue, model.Catalogue);
        Assert.Equal(0.75d, model.SeverityVolatility);
    }

    [Fact]
    public void A_model_without_a_catalogue_is_refused() {
        // Arrange
        double anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new CatastropheModel(null!, anyVolatility));

        // Assert
        Assert.Equal("catalogue", refusal.ParamName);
    }

    [Fact]
    public void A_model_with_an_empty_catalogue_is_refused() {
        // Arrange
        double anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CatastropheModel([], anyVolatility));

        // Assert
        Assert.Equal("catalogue", refusal.ParamName);
    }

    [Fact]
    public void A_catalogue_holding_a_null_event_is_refused() {
        // Arrange
        CatastropheEvent anyEvent      = Any.CatastropheEvent().Generate();
        double           anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CatastropheModel([anyEvent, null!], anyVolatility));

        // Assert
        Assert.Equal("catalogue", refusal.ParamName);
    }

    [Fact]
    public void An_event_id_appearing_twice_in_the_catalogue_is_refused_by_name() {
        // Arrange
        CatastropheEvent first         = Any.CatastropheEvent().Generate();
        CatastropheEvent sameId        = Any.CatastropheEvent().WithId(first.Id).Generate();
        double           anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CatastropheModel([first, sameId], anyVolatility));

        // Assert
        Assert.Contains(first.Id, refusal.Message);
        Assert.Equal("catalogue", refusal.ParamName);
    }

    [Fact]
    public void The_same_event_listed_twice_is_a_duplicate() {
        // Arrange
        CatastropheEvent anyEvent      = Any.CatastropheEvent().Generate();
        double           anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new CatastropheModel([anyEvent, anyEvent], anyVolatility));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Theory]
    [InlineData(-0.0001d)]
    [InlineData(2.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_volatility_outside_zero_to_two_is_refused(double volatility) {
        // Arrange
        List<CatastropheEvent> anyCatalogue = Any.ListOf(Any.CatastropheEvent()).WithCount(1).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CatastropheModel(anyCatalogue, volatility));

        // Assert
        Assert.Equal("severityVolatility", refusal.ParamName);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(2d)]
    public void A_volatility_on_the_edge_of_its_range_is_accepted(double volatility) {
        // Act
        CatastropheModel model = Any.CatastropheModel().WithSeverityVolatility(volatility).Generate();

        // Assert
        Assert.Equal(volatility, model.SeverityVolatility);
    }

    [Fact]
    public void A_model_does_not_follow_later_changes_to_the_list_it_was_built_from() {
        // Arrange
        List<CatastropheEvent> catalogue = Any.ListOf(Any.CatastropheEvent()).WithCount(2).DistinctBy(e => e.Id).Generate();
        CatastropheModel       model     = new CatastropheModel(catalogue, 0d);

        // Act
        catalogue.Clear();

        // Assert
        Assert.Equal(2, model.Catalogue.Count);
    }

    [Fact]
    public void An_unfrozen_portfolio_cannot_be_simulated() {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Add(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyModel.Simulate(anyPortfolio, 10, anySeed));

        // Assert
        Assert.Contains(anyPortfolio.Name, refusal.Message);
    }

    [Fact]
    public void A_simulation_of_no_portfolio_is_refused() {
        // Arrange
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        int              anySeed  = Any.Int32().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyModel.Simulate(null!, 10, anySeed));

        // Assert
        Assert.Equal("portfolio", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_number_of_years_outside_one_to_a_million_is_refused(int years) {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Scenes.FrozenPortfolioOf(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyModel.Simulate(anyPortfolio, years, anySeed));

        // Assert
        Assert.Equal("years", refusal.ParamName);
    }

    [Fact]
    public void A_single_year_can_be_simulated() {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Scenes.FrozenPortfolioOf(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        // Act
        LossDistribution distribution = anyModel.Simulate(anyPortfolio, 1, anySeed);

        // Assert
        Assert.Equal(1, distribution.Years);
    }

    [Fact]
    public void A_million_years_can_be_simulated() {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Scenes.FrozenPortfolioOf(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        // Act
        LossDistribution distribution = anyModel.Simulate(anyPortfolio, CatastropheModel.MaxYears, anySeed);

        // Assert
        Assert.Equal(1_000_000, distribution.Years);
    }

    [Fact]
    public void The_distribution_has_one_annual_loss_per_simulated_year() {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Scenes.FrozenPortfolioOf(Any.Exposure().Generate());
        int              anyYears     = Any.Int32().Between(1, 500).Generate();
        int              anySeed      = Any.Int32().Generate();

        // Act
        LossDistribution distribution = anyModel.Simulate(anyPortfolio, anyYears, anySeed);

        // Assert
        Assert.Equal(anyYears, distribution.Years);
    }

    [Fact]
    public void The_same_seed_replays_the_same_simulation() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 40d, 2d);
        CatastropheModel model       = new CatastropheModel([catastrophe], 0.6d);
        Portfolio        portfolio   = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(10_000m, 800_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed     = Any.Int32().Generate();

        // Act
        LossDistribution first  = model.Simulate(portfolio, 500, anySeed);
        LossDistribution second = model.Simulate(portfolio, 500, anySeed);

        // Assert
        Assert.Equal(first.AnnualLosses, second.AnnualLosses);
    }

    [Fact]
    public void Two_different_seeds_give_different_simulations() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 40d, 2d);
        CatastropheModel model       = new CatastropheModel([catastrophe], 0.6d);
        Portfolio        portfolio   = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(10_000m, 800_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed     = Any.Int32().Generate();
        int              anyOtherSeed = Any.Int32().DifferentFrom(anySeed).Generate();

        // Act
        LossDistribution first  = model.Simulate(portfolio, 500, anySeed);
        LossDistribution second = model.Simulate(portfolio, 500, anyOtherSeed);

        // Assert
        Assert.NotEqual(first.AnnualLosses, second.AnnualLosses);
    }

    [Fact]
    public void Simulating_does_not_change_the_portfolio() {
        // Arrange
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Scenes.FrozenPortfolioOf(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        // Act
        anyModel.Simulate(anyPortfolio, 20, anySeed);

        // Assert
        Assert.True(anyPortfolio.IsFrozen);
        Assert.Single(anyPortfolio.Exposures);
    }

    [Fact]
    public void A_portfolio_the_events_never_reach_loses_nothing_in_any_year() {
        // Arrange
        GeoPoint         place       = new GeoPoint(10d, 10d);
        GeoPoint         farAway     = new GeoPoint(-40d, 120d);
        CatastropheEvent catastrophe = Scenes.EventAt(farAway, 50d, 5d);
        CatastropheModel model       = new CatastropheModel([catastrophe], 1d);
        Portfolio        portfolio   = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(place, 100_000m, new PolicyTerms(0m, 100_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed     = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, 1_000, anySeed);

        // Assert
        Assert.Equal(0m, distribution.Maximum);
    }

    [Fact]
    public void Without_volatility_every_occurrence_costs_exactly_the_catalogued_loss() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 10d, 2d);                          // intensity 10 -> ratio 0.1
        CatastropheModel model       = new CatastropheModel([catastrophe], 0d);
        Portfolio        portfolio   = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(0m, 1_000_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed     = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, 2_000, anySeed);

        // Assert
        Assert.All(distribution.AnnualLosses, annualLoss => Assert.Equal(0m, annualLoss % 100_000m));
        Assert.True(distribution.Maximum >= 200_000m, "with two occurrences a year on average, some year should suffer at least two");
    }

    [Fact]
    public void The_average_annual_loss_is_the_annual_rate_times_the_loss_of_one_occurrence() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 10d, 2d);                          // 2 occurrences a year, 100 000 each
        CatastropheModel model       = new CatastropheModel([catastrophe], 0d);
        Portfolio        portfolio   = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(0m, 1_000_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed     = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, 100_000, anySeed);

        // Assert
        Assert.InRange(distribution.Mean, 190_000m, 210_000m);
    }

    [Fact]
    public void A_severity_with_mean_one_leaves_the_average_annual_loss_where_it_was() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 10d, 2d);                          // linear part of the curve: loss is proportional to severity
        CatastropheModel model       = new CatastropheModel([catastrophe], 0.5d);
        Portfolio        portfolio   = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(0m, 1_000_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed     = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, 200_000, anySeed);

        // Assert
        Assert.InRange(distribution.Mean, 185_000m, 215_000m);
    }

    [Fact]
    public void Volatility_widens_the_spread_of_the_annual_losses() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 10d, 2d);
        Portfolio        portfolio   = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(0m, 1_000_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed     = Any.Int32().Generate();

        // Act
        LossDistribution calm      = new CatastropheModel([catastrophe], 0d).Simulate(portfolio, 50_000, anySeed);
        LossDistribution turbulent = new CatastropheModel([catastrophe], 1d).Simulate(portfolio, 50_000, anySeed);

        // Assert
        Assert.True(turbulent.StandardDeviation > calm.StandardDeviation);
    }

    [Fact]
    public void Every_exposure_of_an_occurrence_suffers_the_same_severity() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 10d, 2d);
        CatastropheModel model       = new CatastropheModel([catastrophe], 1d);
        PolicyTerms      terms       = new PolicyTerms(0m, 1_000_000m);
        string           anyId       = Any.StringMatching("PRP-[0-9]{6}").Generate();
        string           anyOtherId  = Any.StringMatching("PRP-[0-9]{6}").DifferentFrom(anyId).Generate();
        Exposure         first       = Scenes.ExposureOf(anyId, anyPlace, 1_000_000m, terms, Scenes.LinearUpToTotalLoss());
        Exposure         twin        = Scenes.ExposureOf(anyOtherId, anyPlace, 1_000_000m, terms, Scenes.LinearUpToTotalLoss());
        int              anySeed     = Any.Int32().Generate();

        // Act
        LossDistribution alone    = model.Simulate(Scenes.FrozenPortfolioOf(first), 300, anySeed);
        LossDistribution together = model.Simulate(Scenes.FrozenPortfolioOf(first, twin), 300, anySeed);

        // Assert
        Assert.Equal(alone.AnnualLosses.Select(loss => 2m * loss), together.AnnualLosses);
    }

    [Fact]
    public void Terms_apply_to_each_occurrence_and_not_to_the_year() {
        // Arrange
        GeoPoint         anyPlace    = Any.GeoPoint().Generate();
        CatastropheEvent catastrophe = Scenes.EventAt(anyPlace, 50d, 3d);                          // 500 000 ground-up each time
        CatastropheModel model       = new CatastropheModel([catastrophe], 0d);
        Portfolio        portfolio   = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(100_000m, 150_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed     = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, 2_000, anySeed);

        // Assert
        Assert.All(distribution.AnnualLosses, annualLoss => Assert.Equal(0m, annualLoss % 150_000m));   // each occurrence pays the limit, never more
        Assert.True(distribution.Maximum >= 450_000m, "with three occurrences a year on average, some year should suffer at least three");
    }

    [Fact]
    public void Every_event_of_the_catalogue_contributes_to_the_losses() {
        // Arrange
        GeoPoint         anyPlace      = Any.GeoPoint().Generate();
        CatastropheEvent small         = Scenes.EventAt(anyPlace, 10d, 1d);                        // 100 000 each
        CatastropheEvent big           = Scenes.EventAt(anyPlace, 30d, 1d);                        // 300 000 each
        CatastropheModel model         = new CatastropheModel([small, big], 0d);
        Portfolio        portfolio     = Scenes.FrozenPortfolioOf(Scenes.ExposureOf(anyPlace, 1_000_000m, new PolicyTerms(0m, 1_000_000m), Scenes.LinearUpToTotalLoss()));
        int              anySeed       = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(portfolio, 100_000, anySeed);

        // Assert
        Assert.InRange(distribution.Mean, 380_000m, 420_000m);                                     // 1 x 100 000 + 1 x 300 000 per year
    }

}
