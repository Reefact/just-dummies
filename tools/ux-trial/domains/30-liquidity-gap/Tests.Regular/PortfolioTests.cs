using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Regular;

public sealed class PortfolioTests {

    private static AnyContractualFlow AnyFlowFor(Portfolio portfolio) {
        return Arbitrary.ContractualFlow()
                        .WithCurrency(portfolio.ReportingCurrency)
                        .WithValueDate(d => d.Between(portfolio.AsOfDate, portfolio.AsOfDate.AddYears(5)));
    }

    [Fact]
    public void A_flow_in_the_reporting_currency_settling_after_the_as_of_date_is_loaded() {
        // Arrange
        Portfolio       anyPortfolio = Arbitrary.Portfolio().Generate();
        ContractualFlow anyFlow      = AnyFlowFor(anyPortfolio).Generate();

        // Act
        anyPortfolio.AddFlow(anyFlow);

        // Assert
        Assert.Same(anyFlow, Assert.Single(anyPortfolio.ContractualFlows));
    }

    [Fact]
    public void A_flow_in_a_foreign_currency_is_refused() {
        // Arrange
        Portfolio       anyEuroPortfolio = Arbitrary.Portfolio().WithReportingCurrency(new Currency("EUR")).Generate();
        ContractualFlow anyDollarFlow    = AnyFlowFor(anyEuroPortfolio).WithCurrency(new Currency("USD")).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyEuroPortfolio.AddFlow(anyDollarFlow));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Empty(anyEuroPortfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_settling_before_the_as_of_date_is_refused() {
        // Arrange
        Portfolio       anyPortfolio = Arbitrary.Portfolio().Generate();
        ContractualFlow anyPastFlow  = AnyFlowFor(anyPortfolio).WithValueDate(anyPortfolio.AsOfDate.AddDays(-1)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.AddFlow(anyPastFlow));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_second_position_with_the_same_identifier_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(AnyFlowFor(anyPortfolio).WithFlowId("BOND-42").Generate());
        NonMaturityDeposit anyDepositReusingTheId = Arbitrary.NonMaturityDeposit()
                                                             .WithDepositId("BOND-42")
                                                             .WithCurrency(anyPortfolio.ReportingCurrency)
                                                             .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.AddDeposit(anyDepositReusingTheId));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Empty(anyPortfolio.Deposits);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_new_positions() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.Freeze();
        ContractualFlow anyFlow = AnyFlowFor(anyPortfolio).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.AddFlow(anyFlow));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Freezing_twice_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.Freeze();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.Freeze());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_schedule_contributes_only_the_payments_on_or_after_the_as_of_date() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().WithAsOfDate(new DateOnly(2026, 3, 1)).Generate();
        CashFlowSchedule anyMonthlyLoan = Arbitrary.CashFlowSchedule()
                                                   .WithScheduleId("LOAN")
                                                   .WithCurrency(anyPortfolio.ReportingCurrency)
                                                   .WithFirstPaymentDate(new DateOnly(2026, 1, 15))
                                                   .WithFrequencyMonths(1)
                                                   .WithNumberOfPayments(6)
                                                   .WithConvention(BusinessDayConvention.Unadjusted)
                                                   .Generate();

        // Act
        anyPortfolio.AddSchedule(anyMonthlyLoan);

        // Assert
        Assert.Equal(["LOAN-3", "LOAN-4", "LOAN-5", "LOAN-6"], anyPortfolio.ContractualFlows.Select(f => f.FlowId));
    }

    [Fact]
    public void A_schedule_clashing_with_a_loaded_flow_is_refused_as_a_whole() {
        // Arrange
        Portfolio anyPortfolio = Arbitrary.Portfolio().Generate();
        anyPortfolio.AddFlow(AnyFlowFor(anyPortfolio).WithFlowId("LOAN-2").Generate());
        CashFlowSchedule anyLoan = Arbitrary.CashFlowSchedule()
                                            .WithScheduleId("LOAN")
                                            .WithCurrency(anyPortfolio.ReportingCurrency)
                                            .WithFirstPaymentDate(d => d.Between(anyPortfolio.AsOfDate, anyPortfolio.AsOfDate.AddYears(1)))
                                            .WithNumberOfPayments(n => n.Between(2, 60))
                                            .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.AddSchedule(anyLoan));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Single(anyPortfolio.ContractualFlows);
    }

}
