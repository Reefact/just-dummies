using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

public sealed class CatastropheModelTests {

    // One exposure at the origin worth 400 000, no deductible: an occurrence of the sample footprint
    // at severity 1 (intensity 8, ratio 0.375) costs exactly 150 000.
    private const decimal OccurrenceLoss = 150_000m;

    private static CatastropheEvent EventAtOrigin(string id, double annualRate) {
        return Any.CatastropheEvent().WithId(id).WithFootprint(Samples.PeakEightAtOrigin()).WithAnnualRate(annualRate).Generate();
    }

    private static Portfolio PortfolioWithOneExposure(decimal limit = 400_000m) {
        return Samples.Frozen(Samples.ExposureAtOrigin("PRP-000001", 400_000m, 0m, limit));
    }

    [Fact]
    public void A_model_without_catalogue_is_refused() {
        // Arrange
        double anyVolatility = Any.Double().Between(0d, 2d).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CatastropheModel(null!, anyVolatility));
    }

    [Fact]
    public void An_empty_catalogue_is_refused() {
        // Arrange
        double anyVolatility = Any.Double().Between(0d, 2d).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new CatastropheModel([], anyVolatility));

        // Assert
        Assert.Equal("catalogue", error.ParamName);
    }

    [Fact]
    public void A_catalogue_holding_a_null_event_is_refused() {
        // Arrange
        CatastropheEvent anyEvent      = Any.CatastropheEvent().Generate();
        double           anyVolatility = Any.Double().Between(0d, 2d).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new CatastropheModel([anyEvent, null!], anyVolatility));

        // Assert
        Assert.Equal("catalogue", error.ParamName);
    }

    [Fact]
    public void A_catalogue_holding_the_same_event_id_twice_is_refused() {
        // Arrange
        CatastropheEvent first         = Any.CatastropheEvent().WithId("EV-0042").Generate();
        CatastropheEvent second        = Any.CatastropheEvent().WithId("EV-0042").Generate();
        double           anyVolatility = Any.Double().Between(0d, 2d).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new CatastropheModel([first, second], anyVolatility));

        // Assert
        Assert.Contains("EV-0042", error.Message);
    }

    [Theory]
    [InlineData(-0.0001d)]
    [InlineData(2.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_volatility_outside_zero_to_two_is_refused(double volatility) {
        // Arrange
        CatastropheEvent anyEvent = Any.CatastropheEvent().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CatastropheModel([anyEvent], volatility));

        // Assert
        Assert.Equal("severityVolatility", error.ParamName);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(2d)]
    public void The_extreme_volatilities_are_accepted(double volatility) {
        // Arrange
        CatastropheEvent anyEvent = Any.CatastropheEvent().Generate();

        // Act
        CatastropheModel model = new CatastropheModel([anyEvent], volatility);

        // Assert
        Assert.Equal(volatility, model.SeverityVolatility);
    }

    [Fact]
    public void The_model_does_not_follow_the_list_of_events_it_was_built_from() {
        // Arrange
        CatastropheEvent       first     = Any.CatastropheEvent().WithId("EV-0001").Generate();
        List<CatastropheEvent> catalogue = [first];
        CatastropheModel       model     = new CatastropheModel(catalogue, 0.5d);

        // Act
        catalogue.Add(Any.CatastropheEvent().WithId("EV-0002").Generate());

        // Assert
        Assert.Single(model.Catalogue);
    }

    [Fact]
    public void Simulating_a_portfolio_that_is_not_frozen_is_refused() {
        // Arrange
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        Portfolio        open     = Any.Portfolio().Generate();
        open.Add(Samples.ExposureAtOrigin("PRP-000001", 400_000m, 0m, 400_000m));
        int anySeed = Any.Int32().Generate();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyModel.Simulate(open, 10, anySeed));

        // Assert
        Assert.Contains(open.Name, error.Message);
    }

    [Fact]
    public void Simulating_nothing_is_refused() {
        // Arrange
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        int              anySeed  = Any.Int32().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyModel.Simulate(null!, 10, anySeed));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    [InlineData(int.MaxValue)]
    public void A_number_of_years_outside_one_to_a_million_is_refused(int years) {
        // Arrange
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        Portfolio        frozen   = PortfolioWithOneExposure();
        int              anySeed  = Any.Int32().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyModel.Simulate(frozen, years, anySeed));

        // Assert
        Assert.Equal("years", error.ParamName);
    }

    [Fact]
    public void A_single_year_can_be_simulated() {
        // Arrange
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        Portfolio        frozen   = PortfolioWithOneExposure();
        int              anySeed  = Any.Int32().Generate();

        // Act
        LossDistribution distribution = anyModel.Simulate(frozen, 1, anySeed);

        // Assert
        Assert.Equal(1, distribution.Years);
    }

    [Fact]
    public void The_distribution_holds_one_loss_per_simulated_year() {
        // Arrange
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        Portfolio        frozen   = PortfolioWithOneExposure();
        int              anySeed  = Any.Int32().Generate();

        // Act
        LossDistribution distribution = anyModel.Simulate(frozen, 250, anySeed);

        // Assert
        Assert.Equal(250, distribution.Years);
    }

    [Fact]
    public void The_same_seed_gives_the_same_distribution() {
        // Arrange
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        Portfolio        frozen   = PortfolioWithOneExposure();
        int              anySeed  = Any.Int32().Generate();

        // Act
        LossDistribution first  = anyModel.Simulate(frozen, 300, anySeed);
        LossDistribution second = anyModel.Simulate(frozen, 300, anySeed);

        // Assert
        Assert.Equal(first.AnnualLosses, second.AnnualLosses);
    }

    [Fact]
    public void The_same_seed_does_not_change_after_the_portfolio_is_simulated_elsewhere() {
        // Arrange
        CatastropheModel anyModel   = Any.CatastropheModel().Generate();
        Portfolio        frozen     = PortfolioWithOneExposure();
        Portfolio        other      = PortfolioWithOneExposure(limit: 100_000m);
        int              anySeed    = Any.Int32().Generate();
        LossDistribution before     = anyModel.Simulate(frozen, 200, anySeed);

        // Act
        anyModel.Simulate(other, 200, anySeed);
        LossDistribution after = anyModel.Simulate(frozen, 200, anySeed);

        // Assert
        Assert.Equal(before.AnnualLosses, after.AnnualLosses);
    }

    [Fact]
    public void A_portfolio_out_of_reach_of_every_event_never_loses_anything() {
        // Arrange
        Footprint        faraway   = new Footprint(new GeoPoint(60d, 100d), 200d, 8d);
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithFootprint(faraway).WithAnnualRate(5d).Generate();
        double           anyVolatility = Any.Double().Between(0d, 2d).Generate();
        CatastropheModel model     = new CatastropheModel([catastrophe], anyVolatility);
        int              anySeed   = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(PortfolioWithOneExposure(), 500, anySeed);

        // Assert
        Assert.Equal(0m, distribution.Maximum);
    }

    [Fact]
    public void Without_volatility_every_year_loses_a_whole_number_of_catalogued_occurrences() {
        // Arrange
        CatastropheModel model   = new CatastropheModel([EventAtOrigin("EV-0001", 2d)], 0d);
        int              anySeed = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(PortfolioWithOneExposure(), 500, anySeed);

        // Assert
        Assert.All(distribution.AnnualLosses, loss => Assert.Equal(0m, loss % OccurrenceLoss));
        Assert.True(distribution.Maximum >= OccurrenceLoss);
    }

    [Fact]
    public void The_average_annual_loss_follows_the_rate_of_the_event() {
        // Arrange: rate 2 per year, 150 000 per occurrence -> about 300 000 a year
        CatastropheModel model   = new CatastropheModel([EventAtOrigin("EV-0001", 2d)], 0d);
        int              anySeed = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(PortfolioWithOneExposure(), 20_000, anySeed);

        // Assert
        Assert.InRange(distribution.Mean, 285_000m, 315_000m);
    }

    [Fact]
    public void The_limit_applies_to_each_occurrence_and_not_to_the_year() {
        // Arrange: ground-up per occurrence 150 000, limit 100 000, up to several occurrences a year
        CatastropheModel model   = new CatastropheModel([EventAtOrigin("EV-0001", 5d)], 0d);
        int              anySeed = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(PortfolioWithOneExposure(limit: 100_000m), 500, anySeed);

        // Assert
        Assert.True(distribution.Maximum > 100_000m);
        Assert.All(distribution.AnnualLosses, loss => Assert.Equal(0m, loss % 100_000m));
    }

    [Fact]
    public void Two_exposures_in_the_same_footprint_share_each_severity_draw() {
        // Arrange: identical exposures, limit never binding -> every year loses exactly twice as much
        CatastropheModel model = new CatastropheModel([EventAtOrigin("EV-0001", 3d)], 1.2d);
        Portfolio single = Samples.Frozen(Samples.ExposureAtOrigin("PRP-000001", 400_000m, 0m, 400_000m));
        Portfolio twin   = Samples.Frozen(Samples.ExposureAtOrigin("PRP-000001", 400_000m, 0m, 400_000m),
                                          Samples.ExposureAtOrigin("PRP-000002", 400_000m, 0m, 400_000m));
        int anySeed = Any.Int32().Generate();

        // Act
        LossDistribution alone   = model.Simulate(single, 300, anySeed);
        LossDistribution together = model.Simulate(twin, 300, anySeed);

        // Assert
        Assert.Equal(alone.AnnualLosses.Select(loss => loss * 2m), together.AnnualLosses);
    }

    [Fact]
    public void Volatility_makes_the_loss_of_an_occurrence_vary_around_the_catalogued_footprint() {
        // Arrange
        CatastropheModel model   = new CatastropheModel([EventAtOrigin("EV-0001", 1d)], 1.5d);
        int              anySeed = Any.Int32().Generate();

        // Act
        LossDistribution distribution = model.Simulate(PortfolioWithOneExposure(), 2_000, anySeed);

        // Assert
        Assert.Contains(distribution.AnnualLosses, loss => loss % OccurrenceLoss != 0m);
    }

}
