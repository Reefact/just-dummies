using System.Globalization;

namespace LiquidityGap.Domain.Tests.Expert.Tests;

/// <summary>Loading positions into a portfolio: what it accepts, what it refuses, and that a refusal leaves it untouched.</summary>
public sealed class PortfolioLoadingTests {

    private static readonly DateOnly AsOf = new DateOnly(2026, 3, 1);

    private static DateOnly Date(string isoDate) {
        return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static Portfolio OpenPortfolioIn(Currency currency) {
        return Any.Portfolio().WithReportingCurrency(currency).WithAsOfDate(AsOf).Generate();
    }

    private static ContractualFlow Flow(Currency currency, string flowId, DateOnly valueDate) {
        return Any.ContractualFlow().WithCurrency(currency).WithFlowId(flowId).WithValueDate(valueDate).Generate();
    }

    private static CashFlowSchedule MonthlySchedule(Currency currency, string scheduleId, string firstPaymentDate, int payments, BusinessDayConvention convention) {
        return Any.CashFlowSchedule()
                  .WithCurrency(currency)
                  .WithScheduleId(scheduleId)
                  .WithFirstPaymentDate(Date(firstPaymentDate))
                  .WithFrequencyMonths(1)
                  .WithNumberOfPayments(payments)
                  .WithConvention(convention)
                  .Generate();
    }

    // ----- construction -----

    [Fact]
    public void A_new_portfolio_is_open_and_holds_nothing() {
        // Act
        Portfolio portfolio = Any.Portfolio().Generate();

        // Assert
        Assert.False(portfolio.IsFrozen);
        Assert.Empty(portfolio.ContractualFlows);
        Assert.Empty(portfolio.Deposits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_identifier_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Portfolio().WithPortfolioId(blank).Generate());

        // Assert
        Assert.Equal("portfolioId", refusal.ParamName);
    }

    [Fact]
    public void A_missing_reporting_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.Portfolio().WithReportingCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("reportingCurrency", refusal.ParamName);
    }

    // ----- AddFlow -----

    [Fact]
    public void A_flow_in_the_reporting_currency_is_added() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        ContractualFlow flow = Flow(anyCurrency, "F1", AsOf.AddDays(10));

        // Act
        portfolio.AddFlow(flow);

        // Assert
        Assert.Same(flow, Assert.Single(portfolio.ContractualFlows));
    }

    [Fact]
    public void A_flow_settling_on_the_as_of_date_itself_is_accepted() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);

        // Act
        portfolio.AddFlow(Flow(anyCurrency, "F1", AsOf));

        // Assert
        Assert.Single(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_settling_the_day_before_the_as_of_date_is_refused_and_not_kept() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow(anyCurrency, "F1", AsOf.AddDays(-1))));

        // Assert
        Assert.Equal("flow", refusal.ParamName);
        Assert.Contains("F1", refusal.Message);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_in_another_currency_is_refused_naming_both_currencies() {
        // Arrange
        Currency reporting = new Currency("EUR");
        Currency foreign   = new Currency("USD");
        Portfolio portfolio = OpenPortfolioIn(reporting);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow(foreign, "F1", AsOf.AddDays(10))));

        // Assert
        Assert.Contains("USD", refusal.Message);
        Assert.Contains("EUR", refusal.Message);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_whose_identifier_is_already_taken_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddFlow(Flow(anyCurrency, "F1", AsOf.AddDays(10)));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow(anyCurrency, "F1", AsOf.AddDays(20))));

        // Assert
        Assert.Contains("F1", refusal.Message);
        Assert.Single(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_cannot_reuse_the_identifier_of_a_deposit() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(anyCurrency).WithDepositId("P1").Generate());

        // Act / Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow(anyCurrency, "P1", AsOf.AddDays(10))));
    }

    [Fact]
    public void A_missing_flow_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPortfolio.AddFlow(null!));

        // Assert
        Assert.Equal("flow", refusal.ParamName);
    }

    // ----- AddSchedule -----

    [Fact]
    public void A_schedule_adds_only_the_payments_settling_on_or_after_the_as_of_date() {
        // Arrange: payments on 15 Jan, 15 Feb, 15 Mar, 15 Apr; the as-of date is 1 Mar, so January and February are history
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        CashFlowSchedule schedule = MonthlySchedule(anyCurrency, "LOAN", "2026-01-15", 4, BusinessDayConvention.Unadjusted);

        // Act
        portfolio.AddSchedule(schedule);

        // Assert
        Assert.Equal(["LOAN-3", "LOAN-4"], portfolio.ContractualFlows.Select(f => f.FlowId));
        Assert.Equal([Date("2026-03-15"), Date("2026-04-15")], portfolio.ContractualFlows.Select(f => f.ValueDate));
    }

    [Fact]
    public void A_schedule_entirely_in_the_past_adds_nothing() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        CashFlowSchedule schedule = MonthlySchedule(anyCurrency, "OLD", "2025-01-15", 3, BusinessDayConvention.Unadjusted);

        // Act
        portfolio.AddSchedule(schedule);

        // Assert
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void The_cut_off_applies_to_the_adjusted_date_of_a_payment() {
        // Arrange: the 15th falls on a Sunday; as of Monday 16 March, Following moves it onto the as-of date and Preceding to Friday 13th
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio following = Any.Portfolio().WithReportingCurrency(anyCurrency).WithAsOfDate(Date("2026-03-16")).Generate();
        Portfolio preceding = Any.Portfolio().WithReportingCurrency(anyCurrency).WithAsOfDate(Date("2026-03-16")).Generate();

        // Act
        following.AddSchedule(MonthlySchedule(anyCurrency, "S", "2026-03-15", 1, BusinessDayConvention.Following));
        preceding.AddSchedule(MonthlySchedule(anyCurrency, "S", "2026-03-15", 1, BusinessDayConvention.Preceding));

        // Assert
        Assert.Single(following.ContractualFlows);
        Assert.Empty(preceding.ContractualFlows);
    }

    [Fact]
    public void A_schedule_one_of_whose_future_payments_clashes_is_refused_as_a_whole() {
        // Arrange: LOAN-4 is already taken
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddFlow(Flow(anyCurrency, "LOAN-4", AsOf.AddDays(60)));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => portfolio.AddSchedule(MonthlySchedule(anyCurrency, "LOAN", "2026-01-15", 4, BusinessDayConvention.Unadjusted)));

        // Assert
        Assert.Contains("LOAN-4", refusal.Message);
        Assert.Equal(["LOAN-4"], portfolio.ContractualFlows.Select(f => f.FlowId));
    }

    [Fact]
    public void A_refused_schedule_reserves_none_of_its_identifiers() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddFlow(Flow(anyCurrency, "LOAN-4", AsOf.AddDays(60)));
        Assert.Throws<ArgumentException>(() => portfolio.AddSchedule(MonthlySchedule(anyCurrency, "LOAN", "2026-01-15", 4, BusinessDayConvention.Unadjusted)));

        // Act: LOAN-3 would have been added by the refused schedule
        portfolio.AddFlow(Flow(anyCurrency, "LOAN-3", AsOf.AddDays(14)));

        // Assert
        Assert.Equal(2, portfolio.ContractualFlows.Count);
    }

    [Fact]
    public void A_payment_already_in_the_past_does_not_clash_with_a_position_of_the_same_identifier() {
        // Arrange: LOAN-1 settled on 15 January, which is history; a flow of that name is allowed
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddFlow(Flow(anyCurrency, "LOAN-1", AsOf.AddDays(5)));

        // Act
        portfolio.AddSchedule(MonthlySchedule(anyCurrency, "LOAN", "2026-01-15", 4, BusinessDayConvention.Unadjusted));

        // Assert
        Assert.Equal(3, portfolio.ContractualFlows.Count);
    }

    [Fact]
    public void A_flow_cannot_take_the_identifier_of_a_scheduled_payment() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddSchedule(MonthlySchedule(anyCurrency, "LOAN", "2026-01-15", 4, BusinessDayConvention.Unadjusted));

        // Act / Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddFlow(Flow(anyCurrency, "LOAN-3", AsOf.AddDays(40))));
    }

    [Fact]
    public void A_schedule_in_another_currency_is_refused() {
        // Arrange
        Portfolio portfolio = OpenPortfolioIn(new Currency("EUR"));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => portfolio.AddSchedule(MonthlySchedule(new Currency("GBP"), "LOAN", "2026-03-15", 2, BusinessDayConvention.Unadjusted)));

        // Assert
        Assert.Contains("GBP", refusal.Message);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_missing_schedule_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPortfolio.AddSchedule(null!));

        // Assert
        Assert.Equal("schedule", refusal.ParamName);
    }

    // ----- AddDeposit -----

    [Fact]
    public void A_deposit_in_the_reporting_currency_is_added() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        NonMaturityDeposit deposit = Any.NonMaturityDeposit().WithCurrency(anyCurrency).Generate();

        // Act
        portfolio.AddDeposit(deposit);

        // Assert
        Assert.Same(deposit, Assert.Single(portfolio.Deposits));
    }

    [Fact]
    public void A_deposit_in_another_currency_is_refused() {
        // Arrange
        Portfolio portfolio = OpenPortfolioIn(new Currency("EUR"));
        NonMaturityDeposit deposit = Any.NonMaturityDeposit().WithCurrency(new Currency("CHF")).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => portfolio.AddDeposit(deposit));

        // Assert
        Assert.Contains("CHF", refusal.Message);
        Assert.Empty(portfolio.Deposits);
    }

    [Fact]
    public void A_deposit_whose_identifier_is_already_taken_is_refused() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(anyCurrency).WithDepositId("D1").Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(anyCurrency).WithDepositId("D1").Generate()));

        // Assert
        Assert.Contains("D1", refusal.Message);
        Assert.Single(portfolio.Deposits);
    }

    [Fact]
    public void A_deposit_cannot_reuse_the_identifier_of_a_flow() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddFlow(Flow(anyCurrency, "P1", AsOf.AddDays(3)));

        // Act / Assert
        Assert.Throws<ArgumentException>(() => portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(anyCurrency).WithDepositId("P1").Generate()));
    }

    [Fact]
    public void A_missing_deposit_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPortfolio.AddDeposit(null!));

        // Assert
        Assert.Equal("deposit", refusal.ParamName);
    }

    // ----- Freeze -----

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

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyPortfolio.Freeze());
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_flow() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.Freeze();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => portfolio.AddFlow(Flow(anyCurrency, "F1", AsOf.AddDays(1))));

        // Assert
        Assert.Contains(portfolio.PortfolioId, refusal.Message);
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_schedule() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.Freeze();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddSchedule(MonthlySchedule(anyCurrency, "LOAN", "2026-03-15", 2, BusinessDayConvention.Unadjusted)));
        Assert.Empty(portfolio.ContractualFlows);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_a_deposit() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.Freeze();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(anyCurrency).Generate()));
        Assert.Empty(portfolio.Deposits);
    }

    [Fact]
    public void A_frozen_portfolio_says_it_is_frozen_before_judging_the_position() {
        // Arrange: the flow is also in a foreign currency and in the past
        Portfolio portfolio = OpenPortfolioIn(new Currency("EUR"));
        portfolio.Freeze();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => portfolio.AddFlow(Flow(new Currency("USD"), "F1", AsOf.AddDays(-5))));
    }

    [Fact]
    public void A_frozen_portfolio_keeps_what_it_held() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio  = OpenPortfolioIn(anyCurrency);
        portfolio.AddFlow(Flow(anyCurrency, "F1", AsOf.AddDays(1)));

        // Act
        portfolio.Freeze();

        // Assert
        Assert.Single(portfolio.ContractualFlows);
    }

}
