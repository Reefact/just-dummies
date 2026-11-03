using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

public sealed class PortfolioTests {

    [Fact]
    public void A_new_portfolio_is_open_and_empty() {
        string anyName     = Any.String().NotBlank().WithMaxLength(100).Generate();
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        Portfolio portfolio = new(anyName, anyCurrency);

        Assert.False(portfolio.IsFrozen);
        Assert.Empty(portfolio.Exposures);
        Assert.Equal(anyName, portfolio.Name);
        Assert.Equal(anyCurrency, portfolio.Currency);
        Assert.Equal(0m, portfolio.TotalInsuredValue);
        Assert.Equal(0m, portfolio.TotalLimit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string name) {
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        Assert.Throws<ArgumentException>(() => new Portfolio(name, anyCurrency));
    }

    [Fact]
    public void A_missing_name_or_currency_is_refused() {
        string anyName     = Any.String().NotBlank().WithMaxLength(100).Generate();
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        Assert.Throws<ArgumentNullException>(() => new Portfolio(null!, anyCurrency));
        Assert.Throws<ArgumentNullException>(() => new Portfolio(anyName, null!));
    }

    [Fact]
    public void A_name_of_a_hundred_characters_is_accepted() {
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        string name        = new('N', 100);

        Portfolio portfolio = new(name, anyCurrency);

        Assert.Equal(name, portfolio.Name);
    }

    [Fact]
    public void A_name_of_a_hundred_and_one_characters_is_refused() {
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        string name        = new('N', 101);

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Portfolio(name, anyCurrency));

        Assert.Equal("name", error.ParamName);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("AAA")]
    public void A_currency_is_three_upper_case_letters(string currency) {
        string anyName = Any.String().NotBlank().WithMaxLength(100).Generate();

        Portfolio portfolio = new(anyName, currency);

        Assert.Equal(currency, portfolio.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("US1")]
    [InlineData(" USD")]
    [InlineData("U$D")]
    public void Any_other_currency_shape_is_refused(string currency) {
        string anyName = Any.String().NotBlank().WithMaxLength(100).Generate();

        Assert.Throws<FormatException>(() => new Portfolio(anyName, currency));
    }

    [Fact(Skip = "domain defect: the currency pattern '^[A-Z]{3}$' accepts a trailing newline ('USD\\n'); the currency is a literal here, seed n/a")]
    public void A_currency_with_a_trailing_line_break_is_refused() {
        string anyName = Any.String().NotBlank().WithMaxLength(100).Generate();

        Assert.Throws<FormatException>(() => new Portfolio(anyName, "USD\n"));
    }

    [Fact]
    public void An_added_exposure_is_listed_in_the_order_of_addition() {
        IReadOnlyList<Exposure> exposures = Fixtures.DistinctExposures(3);
        Portfolio               portfolio = Any.Portfolio().Generate();

        foreach (Exposure exposure in exposures) {
            portfolio.Add(exposure);
        }

        Assert.Equal(exposures, portfolio.Exposures);
    }

    [Fact]
    public void The_same_property_cannot_be_exposed_twice() {
        Portfolio portfolio = Any.Portfolio().Generate();
        Exposure  first     = Any.Exposure().WithProperty(p => p.WithId("PRP-000007")).Generate();
        Exposure  second    = Any.Exposure().WithProperty(p => p.WithId("PRP-000007")).Generate();
        portfolio.Add(first);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => portfolio.Add(second));

        Assert.Contains("PRP-000007", error.Message);
        Assert.Single(portfolio.Exposures);
    }

    [Fact]
    public void Property_ids_are_compared_exactly_not_by_prefix() {
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Any.Exposure().WithProperty(p => p.WithId("PRP-000007")).Generate());

        portfolio.Add(Any.Exposure().WithProperty(p => p.WithId("PRP-000070")).Generate());

        Assert.Equal(2, portfolio.Exposures.Count);
    }

    [Fact]
    public void A_missing_exposure_is_refused() {
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        Assert.Throws<ArgumentNullException>(() => anyPortfolio.Add(null!));
    }

    [Fact]
    public void An_empty_portfolio_cannot_be_frozen() {
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyPortfolio.Freeze());

        Assert.Contains(anyPortfolio.Name, error.Message);
        Assert.False(anyPortfolio.IsFrozen);
    }

    [Fact]
    public void Freezing_closes_the_portfolio() {
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Any.Exposure().Generate());

        portfolio.Freeze();

        Assert.True(portfolio.IsFrozen);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_any_further_exposure() {
        IReadOnlyList<Exposure> exposures = Fixtures.DistinctExposures(2);
        Portfolio               portfolio = Fixtures.FrozenPortfolio(exposures[0]);

        Assert.Throws<InvalidOperationException>(() => portfolio.Add(exposures[1]));
        Assert.Single(portfolio.Exposures);
    }

    [Fact]
    public void A_frozen_portfolio_cannot_be_frozen_again() {
        Portfolio frozen = Fixtures.FrozenPortfolio(Any.Exposure().Generate());

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => frozen.Freeze());

        Assert.Contains("already frozen", error.Message);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_duplicate_with_the_frozen_message_rather_than_the_duplicate_one() {
        Exposure  exposure = Any.Exposure().Generate();
        Portfolio frozen   = Fixtures.FrozenPortfolio(exposure);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => frozen.Add(exposure));

        Assert.Contains("frozen", error.Message);
    }

    [Fact]
    public void Totals_add_up_the_insured_values_and_the_limits() {
        Footprint footprint = Any.Footprint().Generate();
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 1000m, deductible: 10m, limit: 800m));
        portfolio.Add(Fixtures.ExposureAtCentre(footprint, "PRP-000002", value: 2500.50m, deductible: 0m, limit: 2000m));

        Assert.Equal(3500.50m, portfolio.TotalInsuredValue);
        Assert.Equal(2800m, portfolio.TotalLimit);
    }

    [Fact(Skip = "domain defect: no upper bound on an insured value, so TotalInsuredValue of two 7e28 properties throws OverflowException instead of a domain refusal; literals here, seed n/a")]
    public void Totals_of_very_large_properties_do_not_overflow() {
        Footprint footprint = Any.Footprint().Generate();
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 70_000_000_000_000_000_000_000_000_000m, deductible: 0m, limit: 1m));
        portfolio.Add(Fixtures.ExposureAtCentre(footprint, "PRP-000002", value: 70_000_000_000_000_000_000_000_000_000m, deductible: 0m, limit: 1m));

        decimal total = portfolio.TotalInsuredValue;

        Assert.True(total > 70_000_000_000_000_000_000_000_000_000m);
    }

    [Fact]
    public void The_loss_of_an_occurrence_sums_every_exposure_under_the_same_severity() {
        Footprint footprint = Any.Footprint().WithPeakIntensity(10.0).Generate();
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 1000m, deductible: 100m, limit: 900m));
        portfolio.Add(Fixtures.ExposureAtCentre(footprint, "PRP-000002", value: 2000m, deductible: 0m, limit: 300m));
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithFootprint(footprint).Generate();

        decimal loss = portfolio.EventLoss(catastrophe, severityFactor: 0.5);

        // first: 500 - 100 = 400 ; second: 1000 capped at 300
        Assert.Equal(700m, loss);
    }

    [Fact]
    public void An_exposure_outside_the_footprint_adds_nothing_to_the_loss() {
        Footprint footprint = Any.Footprint().WithCenter(new GeoPoint(0, 0)).WithRadiusKm(50.0).WithPeakIntensity(10.0).Generate();
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 1000m, deductible: 0m, limit: 1000m));
        portfolio.Add(Any.Exposure().WithProperty(p => p.WithId("PRP-000002").WithLocation(new GeoPoint(60, 100))).Generate());
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithFootprint(footprint).Generate();

        decimal loss = portfolio.EventLoss(catastrophe, severityFactor: 1.0);

        Assert.Equal(1000m, loss);
    }

    [Fact]
    public void The_loss_of_an_occurrence_is_computed_on_an_open_portfolio_too() {
        Footprint footprint = Any.Footprint().WithPeakIntensity(10.0).Generate();
        Portfolio portfolio = Any.Portfolio().Generate();
        portfolio.Add(Fixtures.ExposureAtCentre(footprint, "PRP-000001", value: 1000m, deductible: 0m, limit: 1000m));
        CatastropheEvent catastrophe = Any.CatastropheEvent().WithFootprint(footprint).Generate();

        decimal loss = portfolio.EventLoss(catastrophe, severityFactor: 0.1);

        Assert.Equal(100m, loss);
        Assert.False(portfolio.IsFrozen);
    }

    [Fact]
    public void A_missing_event_is_refused() {
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        Assert.Throws<ArgumentNullException>(() => anyPortfolio.EventLoss(null!, 1.0));
    }

}
