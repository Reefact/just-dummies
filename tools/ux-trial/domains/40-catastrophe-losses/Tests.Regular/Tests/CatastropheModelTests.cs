using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

public sealed class CatastropheModelTests {

    // A footprint over the origin with a peak of 100: the exposures below stand at its centre and are
    // damaged in proportion to the severity factor alone (ground-up loss = value x severity, capped at value).
    private static Footprint CentredFootprint() {
        return Any.Footprint().WithCenter(new GeoPoint(0, 0)).WithRadiusKm(300.0).WithPeakIntensity(100.0).Generate();
    }

    private static CatastropheEvent EventOver(Footprint footprint, double annualRate) {
        return Any.CatastropheEvent().WithFootprint(footprint).WithAnnualRate(annualRate).Generate();
    }

    // ---------------------------------------------------------------- construction

    [Fact]
    public void A_model_keeps_its_catalogue_and_volatility() {
        IReadOnlyList<CatastropheEvent> catalogue = Any.ListOf(Any.CatastropheEvent()).WithCount(1).Generate();
        double                          anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        CatastropheModel model = new(catalogue, anyVolatility);

        Assert.Equal(catalogue, model.Catalogue);
        Assert.Equal(anyVolatility, model.SeverityVolatility);
    }

    [Fact]
    public void A_missing_catalogue_is_refused() {
        double anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        Assert.Throws<ArgumentNullException>(() => new CatastropheModel(null!, anyVolatility));
    }

    [Fact]
    public void An_empty_catalogue_is_refused() {
        double anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new CatastropheModel([], anyVolatility));

        Assert.Equal("catalogue", error.ParamName);
    }

    [Fact]
    public void A_catalogue_holding_a_null_event_is_refused() {
        CatastropheEvent anyEvent      = Any.CatastropheEvent().Generate();
        double           anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new CatastropheModel([anyEvent, null!], anyVolatility));

        Assert.Equal("catalogue", error.ParamName);
    }

    [Fact]
    public void Two_events_with_the_same_id_are_refused_naming_the_id() {
        CatastropheEvent first         = Any.CatastropheEvent().WithId("EV-0042").Generate();
        CatastropheEvent second        = Any.CatastropheEvent().WithId("EV-0042").Generate();
        double           anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new CatastropheModel([first, second], anyVolatility));

        Assert.Contains("EV-0042", error.Message);
    }

    [Fact]
    public void Event_ids_are_compared_whole_not_by_prefix() {
        CatastropheEvent first         = Any.CatastropheEvent().WithId("EV-0042").Generate();
        CatastropheEvent second        = Any.CatastropheEvent().WithId("EV-00420").Generate();
        double           anyVolatility = Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility).Generate();

        CatastropheModel model = new([first, second], anyVolatility);

        Assert.Equal(2, model.Catalogue.Count);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(2.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_severity_volatility_outside_zero_to_two_is_refused(double volatility) {
        CatastropheEvent anyEvent = Any.CatastropheEvent().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CatastropheModel([anyEvent], volatility));

        Assert.Equal("severityVolatility", error.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.0)]
    public void The_extreme_valid_volatilities_are_accepted(double volatility) {
        CatastropheModel model = Any.CatastropheModel().WithSeverityVolatility(volatility).Generate();

        Assert.Equal(volatility, model.SeverityVolatility);
    }

    // ---------------------------------------------------------------- Simulate: refusals

    [Fact]
    public void A_portfolio_that_is_not_frozen_cannot_be_simulated() {
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        openPortfolio = Any.Portfolio().Generate();
        openPortfolio.Add(Any.Exposure().Generate());
        int anySeed = Any.Int32().Generate();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyModel.Simulate(openPortfolio, years: 10, anySeed));

        Assert.Contains(openPortfolio.Name, error.Message);
    }

    [Fact]
    public void A_missing_portfolio_is_refused() {
        CatastropheModel anyModel = Any.CatastropheModel().Generate();
        int              anySeed  = Any.Int32().Generate();

        Assert.Throws<ArgumentNullException>(() => anyModel.Simulate(null!, years: 10, anySeed));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    [InlineData(int.MaxValue)]
    public void The_number_of_simulated_years_must_lie_within_one_and_a_million(int years) {
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Fixtures.FrozenPortfolio(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyModel.Simulate(anyPortfolio, years, anySeed));

        Assert.Equal("years", error.ParamName);
    }

    [Fact]
    public void A_single_year_is_a_distribution_of_one_year() {
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Fixtures.FrozenPortfolio(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        LossDistribution distribution = anyModel.Simulate(anyPortfolio, years: 1, anySeed);

        Assert.Equal(1, distribution.Years);
    }

    // ---------------------------------------------------------------- Simulate: behaviour

    [Fact]
    public void The_distribution_has_one_loss_per_simulated_year() {
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Fixtures.FrozenPortfolio(Any.Exposure().Generate());
        int              anySeed      = Any.Int32().Generate();

        LossDistribution distribution = anyModel.Simulate(anyPortfolio, years: 250, anySeed);

        Assert.Equal(250, distribution.Years);
        Assert.Equal(250, distribution.AnnualLosses.Count);
    }

    [Fact]
    public void The_same_seed_gives_the_same_distribution() {
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Fixtures.FrozenPortfolio(Fixtures.DistinctExposures(3).ToArray());
        int              anySeed      = Any.Int32().Generate();

        LossDistribution first  = anyModel.Simulate(anyPortfolio, years: 500, anySeed);
        LossDistribution second = anyModel.Simulate(anyPortfolio, years: 500, anySeed);

        Assert.Equal(first.AnnualLosses, second.AnnualLosses);
    }

    [Fact]
    public void Simulating_leaves_the_portfolio_untouched() {
        CatastropheModel anyModel     = Any.CatastropheModel().Generate();
        Portfolio        anyPortfolio = Fixtures.FrozenPortfolio(Fixtures.DistinctExposures(2).ToArray());
        int              anySeed      = Any.Int32().Generate();
        decimal          valueBefore  = anyPortfolio.TotalInsuredValue;

        anyModel.Simulate(anyPortfolio, years: 100, anySeed);

        Assert.True(anyPortfolio.IsFrozen);
        Assert.Equal(2, anyPortfolio.Exposures.Count);
        Assert.Equal(valueBefore, anyPortfolio.TotalInsuredValue);
    }

    [Fact]
    public void Different_seeds_give_different_distributions() {
        Footprint        footprint = CentredFootprint();
        CatastropheModel model     = new([EventOver(footprint, 2.0)], severityVolatility: 1.0);
        Portfolio        portfolio = Fixtures.FrozenPortfolio(Fixtures.ExposureAtCentre(footprint, "PRP-000001", 1_000m, 0m, 1_000m));

        LossDistribution first  = model.Simulate(portfolio, years: 500, seed: 1);
        LossDistribution second = model.Simulate(portfolio, years: 500, seed: 2);

        Assert.NotEqual(first.AnnualLosses, second.AnnualLosses);
    }

    [Fact]
    public void An_event_that_misses_every_exposure_leaves_every_year_without_loss() {
        Footprint        faraway   = Any.Footprint().WithCenter(new GeoPoint(-60, 150)).WithRadiusKm(100.0).Generate();
        CatastropheModel model     = new([EventOver(faraway, 5.0)], severityVolatility: 1.0);
        Exposure         exposure  = Any.Exposure().WithProperty(p => p.WithLocation(new GeoPoint(50, -100))).Generate();
        Portfolio        portfolio = Fixtures.FrozenPortfolio(exposure);
        int              anySeed   = Any.Int32().Generate();

        LossDistribution distribution = model.Simulate(portfolio, years: 300, anySeed);

        Assert.All(distribution.AnnualLosses, loss => Assert.Equal(0m, loss));
    }

    [Fact]
    public void Without_volatility_every_annual_loss_is_a_whole_number_of_identical_occurrences() {
        // Severity is exactly 1: one occurrence costs exactly value x 1 = 1 000 (full damage at the peak).
        Footprint        footprint = CentredFootprint();
        CatastropheModel model     = new([EventOver(footprint, 2.0)], severityVolatility: 0.0);
        Portfolio        portfolio = Fixtures.FrozenPortfolio(Fixtures.ExposureAtCentre(footprint, "PRP-000001", 1_000m, 0m, 1_000m));
        int              anySeed   = Any.Int32().Generate();

        LossDistribution distribution = model.Simulate(portfolio, years: 2_000, anySeed);

        Assert.All(distribution.AnnualLosses, loss => Assert.Equal(0m, loss % 1_000m));
        Assert.True(distribution.Maximum >= 2_000m, "with a rate of 2 per year over 2 000 years, some year must have had several occurrences");
    }

    [Fact]
    public void The_average_annual_loss_is_the_annual_rate_times_the_loss_of_one_occurrence() {
        Footprint        footprint = CentredFootprint();
        CatastropheModel model     = new([EventOver(footprint, 2.0)], severityVolatility: 0.0);
        Portfolio        portfolio = Fixtures.FrozenPortfolio(Fixtures.ExposureAtCentre(footprint, "PRP-000001", 1_000m, 0m, 1_000m));
        int              anySeed   = Any.Int32().Generate();

        LossDistribution distribution = model.Simulate(portfolio, years: 50_000, anySeed);

        // Poisson(2): standard error of the mean over 50 000 years is 1000 x sqrt(2 / 50 000) = 6.3; allow ten times that.
        Assert.InRange(distribution.Mean, 1_940m, 2_060m);
    }

    [Fact]
    public void A_lognormal_severity_of_mean_one_leaves_the_average_loss_unchanged_when_nothing_caps_it() {
        // Linear region of the curve, no deductible, limit = value: the loss is proportional to the severity, so its mean is
        // rate x value x (peak / full-damage) x E[severity] = 2 x 1 000 x 0.1 x 1.
        Footprint footprint = CentredFootprint();
        Exposure exposure = new(new InsuredProperty("PRP-000001", footprint.Center, 1_000m, Fixtures.LinearCurve(1_000.0)), new PolicyTerms(0m, 1_000m));
        CatastropheModel model     = new([EventOver(footprint, 2.0)], severityVolatility: 0.5);
        Portfolio        portfolio = Fixtures.FrozenPortfolio(exposure);
        int              anySeed   = Any.Int32().Generate();

        LossDistribution distribution = model.Simulate(portfolio, years: 100_000, anySeed);

        // relative standard error of the mean is sqrt(exp(0.25) / 2 / 100 000) = 0.25 %; allow 3 %.
        Assert.InRange(distribution.Mean, 194m, 206m);
    }

    [Fact]
    public void Volatility_spreads_the_annual_losses_around_the_same_mean() {
        Footprint footprint = CentredFootprint();
        Exposure exposure = new(new InsuredProperty("PRP-000001", footprint.Center, 1_000m, Fixtures.LinearCurve(1_000.0)), new PolicyTerms(0m, 1_000m));
        Portfolio portfolio = Fixtures.FrozenPortfolio(exposure);
        int       anySeed   = Any.Int32().Generate();
        CatastropheModel steady   = new([EventOver(footprint, 2.0)], severityVolatility: 0.0);
        CatastropheModel volatile_ = new([EventOver(footprint, 2.0)], severityVolatility: 1.0);

        LossDistribution steadyLosses   = steady.Simulate(portfolio, years: 20_000, anySeed);
        LossDistribution volatileLosses = volatile_.Simulate(portfolio, years: 20_000, anySeed);

        Assert.True(volatileLosses.StandardDeviation > steadyLosses.StandardDeviation);
    }

    [Fact]
    public void All_exposures_share_the_severity_of_an_occurrence() {
        // Two identical exposures, with the same terms, must lose exactly twice what one of them loses, year after year:
        // were the severity drawn per exposure, the annual losses would no longer be twice each other.
        Footprint        footprint = CentredFootprint();
        CatastropheModel model     = new([EventOver(footprint, 3.0)], severityVolatility: 1.0);
        Portfolio        single    = Fixtures.FrozenPortfolio(Fixtures.ExposureAtCentre(footprint, "PRP-000001", 1_000m, 0m, 1_000m));
        Portfolio        pair      = Fixtures.FrozenPortfolio(Fixtures.ExposureAtCentre(footprint, "PRP-000001", 1_000m, 0m, 1_000m),
                                                              Fixtures.ExposureAtCentre(footprint, "PRP-000002", 1_000m, 0m, 1_000m));
        int anySeed = Any.Int32().Generate();

        LossDistribution singleLosses = model.Simulate(single, years: 1_000, anySeed);
        LossDistribution pairLosses   = model.Simulate(pair, years: 1_000, anySeed);

        Assert.Equal(singleLosses.AnnualLosses.Select(loss => 2 * loss), pairLosses.AnnualLosses);
    }

    [Fact]
    public void The_terms_apply_to_each_occurrence_and_not_to_the_year() {
        // Without volatility, an occurrence costs 1 000 - 400 deductible = 600, capped at the 500 limit: every
        // occurrence pays 500, so every year pays a multiple of 500. Terms applied to the annual total would break that.
        Footprint        footprint = CentredFootprint();
        CatastropheModel model     = new([EventOver(footprint, 4.0)], severityVolatility: 0.0);
        Portfolio        portfolio = Fixtures.FrozenPortfolio(Fixtures.ExposureAtCentre(footprint, "PRP-000001", 1_000m, 400m, 500m));
        int              anySeed   = Any.Int32().Generate();

        LossDistribution distribution = model.Simulate(portfolio, years: 2_000, anySeed);

        Assert.All(distribution.AnnualLosses, loss => Assert.Equal(0m, loss % 500m));
        Assert.True(distribution.Maximum >= 1_000m);
    }

    [Fact]
    public void Each_event_of_the_catalogue_contributes_to_the_annual_loss() {
        Footprint        north = Any.Footprint().WithCenter(new GeoPoint(40, 0)).WithRadiusKm(100.0).WithPeakIntensity(100.0).Generate();
        Footprint        south = Any.Footprint().WithCenter(new GeoPoint(-40, 0)).WithRadiusKm(100.0).WithPeakIntensity(100.0).Generate();
        CatastropheModel model = new([Any.CatastropheEvent().WithId("EV-0001").WithFootprint(north).WithAnnualRate(1.0).Generate(),
                                      Any.CatastropheEvent().WithId("EV-0002").WithFootprint(south).WithAnnualRate(1.0).Generate()],
                                     severityVolatility: 0.0);
        Portfolio portfolio = Fixtures.FrozenPortfolio(Fixtures.ExposureAtCentre(north, "PRP-000001", 1_000m, 0m, 1_000m),
                                                       Fixtures.ExposureAtCentre(south, "PRP-000002", 3_000m, 0m, 3_000m));
        int anySeed = Any.Int32().Generate();

        LossDistribution distribution = model.Simulate(portfolio, years: 20_000, anySeed);

        // Each event: rate 1 -> 1 000 + 3 000 per year on average (std. error ~ 25)
        Assert.InRange(distribution.Mean, 3_850m, 4_150m);
    }

}
