using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class PortfolioLoadingTests {

    private static Portfolio OpenPortfolio(Currency currency, DateOnly asOf) {
        return Any.Portfolio().WithReportingCurrency(currency).WithAsOfDate(asOf).Generate();
    }

    private static ContractualFlow Flow(string id, DateOnly date, Currency currency) {
        return Any.ContractualFlow().WithFlowId(id).WithValueDate(date).WithCurrency(currency).Generate();
    }

    [Fact]
    public void A_new_portfolio_is_open_and_empty() {
        // Arrange
        string   anyId       = Any.String().NotBlank().Generate();
        Currency anyCurrency = Any.Currency().Generate();
        DateOnly anyAsOf     = Any.DateOnly().Generate();

        // Act
        Portfolio portfolio = new Portfolio(anyId, anyCurrency, anyAsOf);

        // Assert
        Assert.Equal(anyId, portfolio.PortfolioId);
        Assert.Same(anyCurrency, portfolio.ReportingCurrency);
        Assert.Equal(anyAsOf, portfolio.AsOfDate);
        Assert.False(portfolio.IsFrozen);
        Assert.Empty(portfolio.ContractualFlows);
        Assert.Empty(portfolio.Deposits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void A_blank_portfolio_identifier_is_refused(string? blankId) {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        DateOnly anyAsOf     = Any.DateOnly().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Portfolio(blankId!, anyCurrency, anyAsOf));

        // Assert
        Assert.Equal("portfolioId", refusal.ParamName);
    }

    [Fact]
    public void A_portfolio_without_a_reporting_currency_is_refused() {
        // Arrange
        string   anyId   = Any.String().NotBlank().Generate();
        DateOnly anyAsOf = Any.DateOnly().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Portfolio(anyId, null!, anyAsOf));

        // Assert
        Assert.Equal("reportingCurrency", refusal.ParamName);
    }

    [Fact]
    public void An_added_flow_is_kept() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        ContractualFlow flow  = Flow("F-1", anyAsOf.AddDays(10), anyCurrency);

        // Act
        portfolio.AddFlow(flow);

        // Assert
        Assert.Same(flow, Assert.Single(portfolio.ContractualFlows));
    }

    [Fact]
    public void A_flow_settling_on_the_as_of_date_is_accepted() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);

        // Act
        portfolio.AddFlow(Flow("F-1", anyAsOf, anyCurrency));

        // Assert
        Assert.Single(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_settling_the_day_before_the_as_of_date_is_refused_and_not_kept() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        ContractualFlow late  = Flow("F-1", anyAsOf.AddDays(-1), anyCurrency);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(late));

        // Assert
        Assert.Equal("flow", refusal.ParamName);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_in_a_foreign_currency_is_refused_naming_both_currencies() {
        // Arrange
        Portfolio       portfolio = Any.Portfolio().WithReportingCurrency(new Currency("EUR")).Generate();
        ContractualFlow dollars   = Any.ContractualFlow().WithCurrency(new Currency("USD")).WithValueDate(portfolio.AsOfDate.AddDays(1)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(dollars));

        // Assert
        Assert.Contains("USD", refusal.Message);
        Assert.Contains("EUR", refusal.Message);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_second_flow_with_the_same_identifier_is_refused() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        portfolio.AddFlow(Flow("F-1", anyAsOf.AddDays(1), anyCurrency));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow("F-1", anyAsOf.AddDays(2), anyCurrency)));

        // Assert
        Assert.Contains("F-1", refusal.Message);
        Assert.Single(portfolio.ContractualFlows);
    }

    [Fact]
    public void Flow_identifiers_are_compared_case_sensitively() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        portfolio.AddFlow(Flow("F-1", anyAsOf.AddDays(1), anyCurrency));

        // Act
        portfolio.AddFlow(Flow("f-1", anyAsOf.AddDays(1), anyCurrency));

        // Assert
        Assert.Equal(2, portfolio.ContractualFlows.Count);
    }

    [Fact]
    public void A_missing_flow_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPortfolio.AddFlow(null!));
    }

    [Fact]
    public void A_schedule_adds_all_its_payments_when_they_all_settle_on_or_after_the_as_of_date() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithCurrency(anyCurrency)
                                       .WithFirstPaymentDate(anyAsOf.AddDays(40))
                                       .WithConvention(BusinessDayConvention.Following)
                                       .WithNumberOfPayments(6)
                                       .Generate();

        // Act
        portfolio.AddSchedule(schedule);

        // Assert
        Assert.Equal(6, portfolio.ContractualFlows.Count);
    }

    [Fact]
    public void A_schedule_keeps_only_the_payments_on_or_after_the_as_of_date() {
        // Arrange: monthly payments on the 15th from January 2025, as of 2025-03-15 (a Saturday, Unadjusted).
        Currency  anyCurrency = Any.Currency().Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, new DateOnly(2025, 3, 15));
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithScheduleId("LN")
                                       .WithCurrency(anyCurrency)
                                       .WithFirstPaymentDate(new DateOnly(2025, 1, 15))
                                       .WithFrequencyMonths(1)
                                       .WithNumberOfPayments(5)
                                       .WithConvention(BusinessDayConvention.Unadjusted)
                                       .Generate();

        // Act
        portfolio.AddSchedule(schedule);

        // Assert: payments 3, 4 and 5 remain; the one falling exactly on the as-of date counts.
        Assert.Equal(["LN-3", "LN-4", "LN-5"], portfolio.ContractualFlows.Select(static f => f.FlowId));
    }

    [Fact]
    public void A_schedule_entirely_in_the_past_adds_nothing_and_is_not_an_error() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, new DateOnly(2025, 3, 15));
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithCurrency(anyCurrency)
                                       .WithFirstPaymentDate(new DateOnly(2024, 1, 15))
                                       .WithFrequencyMonths(1)
                                       .WithNumberOfPayments(3)
                                       .Generate();

        // Act
        portfolio.AddSchedule(schedule);

        // Assert
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void The_business_day_adjustment_decides_whether_a_payment_is_still_ahead() {
        // Arrange: 2025-03-16 is a Sunday; as of Monday 2025-03-17, a Preceding payment lands on Friday the 14th (history)
        // while a Following one lands on the 17th (kept).
        Currency  anyCurrency = Any.Currency().Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, new DateOnly(2025, 3, 17));
        CashFlowSchedule preceding = Any.CashFlowSchedule().WithScheduleId("P").WithCurrency(anyCurrency)
                                        .WithFirstPaymentDate(new DateOnly(2025, 3, 16)).WithNumberOfPayments(1)
                                        .WithConvention(BusinessDayConvention.Preceding).Generate();
        CashFlowSchedule following = Any.CashFlowSchedule().WithScheduleId("F").WithCurrency(anyCurrency)
                                        .WithFirstPaymentDate(new DateOnly(2025, 3, 16)).WithNumberOfPayments(1)
                                        .WithConvention(BusinessDayConvention.Following).Generate();

        // Act
        portfolio.AddSchedule(preceding);
        portfolio.AddSchedule(following);

        // Assert
        Assert.Equal(["F-1"], portfolio.ContractualFlows.Select(static f => f.FlowId));
    }

    [Fact]
    public void A_schedule_in_a_foreign_currency_is_refused() {
        // Arrange
        Portfolio portfolio = Any.Portfolio().WithReportingCurrency(new Currency("EUR")).Generate();
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithCurrency(new Currency("GBP")).WithFirstPaymentDate(portfolio.AsOfDate).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddSchedule(schedule));

        // Assert
        Assert.Contains("GBP", refusal.Message);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_schedule_clashing_with_a_known_position_is_refused_as_a_whole() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        portfolio.AddFlow(Flow("LN-3", anyAsOf.AddDays(5), anyCurrency));
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithScheduleId("LN").WithCurrency(anyCurrency)
                                       .WithFirstPaymentDate(anyAsOf.AddDays(1)).WithNumberOfPayments(5)
                                       .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddSchedule(schedule));

        // Assert: nothing from the schedule was added, not even the payments before the clash.
        Assert.Contains("LN-3", refusal.Message);
        Assert.Equal(["LN-3"], portfolio.ContractualFlows.Select(static f => f.FlowId));
    }

    [Fact]
    public void A_payment_of_a_past_schedule_whose_id_is_taken_does_not_clash() {
        // Arrange: LN-1 is history for this portfolio, so a flow named LN-1 is not a duplicate.
        Currency  anyCurrency = Any.Currency().Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, new DateOnly(2025, 3, 15));
        portfolio.AddFlow(Flow("LN-1", new DateOnly(2025, 4, 1), anyCurrency));
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithScheduleId("LN").WithCurrency(anyCurrency)
                                       .WithFirstPaymentDate(new DateOnly(2025, 1, 15)).WithFrequencyMonths(1).WithNumberOfPayments(3)
                                       .WithConvention(BusinessDayConvention.Unadjusted)
                                       .Generate();

        // Act
        portfolio.AddSchedule(schedule);

        // Assert: payments 1 and 2 are history (15 Jan, 15 Feb); payment 3 is 15 Mar.
        Assert.Equal(["LN-1", "LN-3"], portfolio.ContractualFlows.Select(static f => f.FlowId));
    }

    [Fact]
    public void The_same_schedule_cannot_be_added_twice() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithCurrency(anyCurrency).WithFirstPaymentDate(anyAsOf.AddDays(30)).WithNumberOfPayments(4).Generate();
        portfolio.AddSchedule(schedule);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddSchedule(schedule));
        Assert.Equal(4, portfolio.ContractualFlows.Count);
    }

    [Fact]
    public void A_flow_cannot_reuse_the_identifier_of_a_schedule_payment() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        portfolio.AddSchedule(Any.CashFlowSchedule().WithScheduleId("LN").WithCurrency(anyCurrency).WithFirstPaymentDate(anyAsOf.AddDays(30)).Generate());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow("LN-1", anyAsOf.AddDays(1), anyCurrency)));
    }

    [Fact]
    public void A_missing_schedule_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPortfolio.AddSchedule(null!));
    }

    [Fact]
    public void An_added_deposit_is_kept() {
        // Arrange
        Currency           anyCurrency = Any.Currency().Generate();
        Portfolio          portfolio   = Any.Portfolio().WithReportingCurrency(anyCurrency).Generate();
        NonMaturityDeposit deposit     = Any.NonMaturityDeposit().WithCurrency(anyCurrency).Generate();

        // Act
        portfolio.AddDeposit(deposit);

        // Assert
        Assert.Same(deposit, Assert.Single(portfolio.Deposits));
    }

    [Fact]
    public void A_deposit_in_a_foreign_currency_is_refused() {
        // Arrange
        Portfolio          portfolio = Any.Portfolio().WithReportingCurrency(new Currency("EUR")).Generate();
        NonMaturityDeposit deposit   = Any.NonMaturityDeposit().WithCurrency(new Currency("JPY")).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddDeposit(deposit));

        // Assert
        Assert.Contains("JPY", refusal.Message);
        Assert.Empty(portfolio.Deposits);
    }

    [Fact]
    public void A_second_deposit_with_the_same_identifier_is_refused() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        Portfolio portfolio   = Any.Portfolio().WithReportingCurrency(anyCurrency).Generate();
        portfolio.AddDeposit(Any.NonMaturityDeposit().WithDepositId("D-1").WithCurrency(anyCurrency).Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => portfolio.AddDeposit(Any.NonMaturityDeposit().WithDepositId("D-1").WithCurrency(anyCurrency).Generate()));

        // Assert
        Assert.Contains("D-1", refusal.Message);
        Assert.Single(portfolio.Deposits);
    }

    [Fact]
    public void A_deposit_cannot_reuse_the_identifier_of_a_flow() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        portfolio.AddFlow(Flow("X-1", anyAsOf.AddDays(3), anyCurrency));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddDeposit(Any.NonMaturityDeposit().WithDepositId("X-1").WithCurrency(anyCurrency).Generate()));
    }

    [Fact]
    public void A_missing_deposit_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPortfolio.AddDeposit(null!));
    }

    [Fact]
    public void Freezing_closes_the_portfolio() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act
        anyPortfolio.Freeze();

        // Assert
        Assert.True(anyPortfolio.IsFrozen);
    }

    [Fact]
    public void Freezing_twice_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Freeze();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPortfolio.Freeze());

        // Assert
        Assert.Contains(anyPortfolio.PortfolioId, refusal.Message);
        Assert.True(anyPortfolio.IsFrozen);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_flow() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        portfolio.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddFlow(Flow("F-1", anyAsOf.AddDays(1), anyCurrency)));
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_schedule() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        DateOnly  anyAsOf     = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1)).Generate();
        Portfolio portfolio   = OpenPortfolio(anyCurrency, anyAsOf);
        portfolio.Freeze();
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithCurrency(anyCurrency).WithFirstPaymentDate(anyAsOf.AddDays(30)).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddSchedule(schedule));
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_deposit() {
        // Arrange
        Currency  anyCurrency = Any.Currency().Generate();
        Portfolio portfolio   = Any.Portfolio().WithReportingCurrency(anyCurrency).Generate();
        portfolio.Freeze();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(anyCurrency).Generate()));
        Assert.Empty(portfolio.Deposits);
    }

    [Fact]
    public void A_frozen_portfolio_reports_that_it_is_frozen_before_it_complains_about_the_position() {
        // Arrange: a flow that would also be in a foreign currency and in the past
        Portfolio portfolio = Any.Portfolio().WithReportingCurrency(new Currency("EUR")).Generate();
        portfolio.Freeze();
        ContractualFlow flow = Any.ContractualFlow().WithCurrency(new Currency("USD")).WithValueDate(portfolio.AsOfDate.AddDays(-5)).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => portfolio.AddFlow(flow));

        // Assert
        Assert.Contains("frozen", refusal.Message);
    }

}
