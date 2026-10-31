namespace CatastropheLosses.Tests.Regular.Tests;

public sealed class CatastropheModelTests {

    private static readonly IAny<int> AnySimulationSeed = Any.Int32();

    /// <summary>One exposure at <paramref name="site" /> which loses exactly 90 000 under an unscaled event of peak 50 there.</summary>
    private static Portfolio FrozenPortfolioAt(GeoPoint site) {
        Portfolio portfolio = Arbitrary.Portfolio().Generate();
        portfolio.Add(Arbitrary.Exposure()
                               .WithProperty(p => p.WithLocation(site).WithInsuredValue(200_000m).WithVulnerability(Curves.Linear()))
                               .WithTerms(new PolicyTerms(deductible: 10_000m, limit: 150_000m))
                               .Generate());
        portfolio.Freeze();

        return portfolio;
    }

    [Fact]
    public void An_open_portfolio_cannot_be_simulated() {
        // Arrange
        CatastropheModel anyModel     = new CatastropheModel([Arbitrary.CatastropheEvent().Generate()], 0.5);
        Portfolio        anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.Add(Arbitrary.Exposure().Generate());

        // Act
        Exception? error = Record.Exception(() => anyModel.Simulate(anyPortfolio, 100, AnySimulationSeed.Generate()));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_catalogue_listing_the_same_event_twice_is_refused() {
        // Arrange
        CatastropheEvent first  = Arbitrary.CatastropheEvent().WithId("EV-0042").Generate();
        CatastropheEvent second = Arbitrary.CatastropheEvent().WithId("EV-0042").Generate();

        // Act
        Exception? error = Record.Exception(() => new CatastropheModel([first, second], 0.5));

        // Assert
        Assert.Contains("EV-0042", Assert.IsType<ArgumentException>(error).Message);
    }

    [Fact]
    public void The_same_seed_simulates_the_same_years() {
        // Arrange
        GeoPoint         anySite  = Arbitrary.GeoPoint().Generate();
        Portfolio        anyBook  = FrozenPortfolioAt(anySite);
        CatastropheModel anyModel = new CatastropheModel(
            [Arbitrary.CatastropheEvent().WithFootprint(f => f.WithCenter(anySite)).Generate()],
            Any.Double().Between(0.0, CatastropheModel.MaxSeverityVolatility).Generate());
        int anySeed = AnySimulationSeed.Generate();

        // Act
        LossDistribution first  = anyModel.Simulate(anyBook, 500, anySeed);
        LossDistribution second = anyModel.Simulate(anyBook, 500, anySeed);

        // Assert
        Assert.Equal(first.AnnualLosses, second.AnnualLosses);
    }

    [Fact]
    public void A_portfolio_out_of_reach_of_every_event_never_loses_anything() {
        // Arrange
        Portfolio        book     = FrozenPortfolioAt(new GeoPoint(-21.1, 55.5));                 // La Réunion
        CatastropheEvent anyQuake = Arbitrary.CatastropheEvent()
                                             .WithFootprint(f => f.WithCenter(new GeoPoint(35.7, 139.7)).WithRadiusKm(r => r.LessThanOrEqualTo(500.0)))
                                             .Generate();
        CatastropheModel anyModel = new CatastropheModel([anyQuake], Any.Double().Between(0.0, CatastropheModel.MaxSeverityVolatility).Generate());

        // Act
        LossDistribution distribution = anyModel.Simulate(book, 200, AnySimulationSeed.Generate());

        // Assert
        Assert.Equal(0m, distribution.Maximum);
    }

    [Fact]
    public void Without_severity_volatility_every_year_is_a_whole_number_of_catalogued_occurrences() {
        // Arrange
        GeoPoint         anySite   = Arbitrary.GeoPoint().Generate();
        Portfolio        book      = FrozenPortfolioAt(anySite);
        CatastropheEvent hurricane = Arbitrary.CatastropheEvent()
                                              .WithFootprint(f => f.WithCenter(anySite).WithPeakIntensity(50.0))
                                              .WithAnnualRate(0.5)
                                              .Generate();
        CatastropheModel model = new CatastropheModel([hurricane], severityVolatility: 0.0);

        // Act
        LossDistribution distribution = model.Simulate(book, 2_000, AnySimulationSeed.Generate());

        // Assert — one occurrence costs 90 000; a year holds zero, one or several of them, never a fraction.
        Assert.All(distribution.AnnualLosses, loss => Assert.Equal(0m, loss % 90_000m));
        Assert.Contains(distribution.AnnualLosses, loss => loss > 0m);
    }

    [Fact]
    public void Simulated_years_outside_the_supported_range_are_refused() {
        // Arrange
        GeoPoint         anySite  = Arbitrary.GeoPoint().Generate();
        Portfolio        anyBook  = FrozenPortfolioAt(anySite);
        CatastropheModel anyModel = new CatastropheModel([Arbitrary.CatastropheEvent().Generate()], 0.5);

        // Act
        Exception? error = Record.Exception(() => anyModel.Simulate(anyBook, 0, AnySimulationSeed.Generate()));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(error);
    }

}
