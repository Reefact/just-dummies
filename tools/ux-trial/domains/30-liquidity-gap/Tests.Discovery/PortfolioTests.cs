using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Discovery;

public sealed class PortfolioTests {

    [Fact]
    public void A_flow_in_a_foreign_currency_is_refused() {
        // Arrange
        Portfolio       anyPortfolio = Any.Portfolio().Generate();
        ContractualFlow anyForeignFlow = Any.ContractualFlow()
                                            .WithCurrency(c => c.WithCode(code => code.DifferentFrom(anyPortfolio.ReportingCurrency.Code)))
                                            .WithValueDate(d => d.AfterOrEqualTo(anyPortfolio.AsOfDate))
                                            .Generate();

        // Act
        Exception? error = Record.Exception(() => anyPortfolio.AddFlow(anyForeignFlow));

        // Assert
        Assert.IsType<ArgumentException>(error);
        Assert.Empty(anyPortfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_settling_before_the_as_of_date_is_refused() {
        // Arrange
        Portfolio       anyPortfolio = Any.Portfolio().Generate();
        ContractualFlow anySettledFlow = Any.ContractualFlow()
                                            .WithCurrency(anyPortfolio.ReportingCurrency)
                                            .WithValueDate(anyPortfolio.AsOfDate.AddDays(-1))
                                            .Generate();

        // Act
        Exception? error = Record.Exception(() => anyPortfolio.AddFlow(anySettledFlow));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_deposit_reusing_the_id_of_a_flow_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(Any.ContractualFlow()
                                .WithFlowId("POS-1")
                                .WithCurrency(anyPortfolio.ReportingCurrency)
                                .WithValueDate(anyPortfolio.AsOfDate)
                                .Generate());
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit().WithDepositId("POS-1").WithCurrency(anyPortfolio.ReportingCurrency).Generate();

        // Act
        Exception? error = Record.Exception(() => anyPortfolio.AddDeposit(anyDeposit));

        // Assert
        Assert.IsType<ArgumentException>(error);
        Assert.Empty(anyPortfolio.Deposits);
    }

    [Fact]
    public void A_frozen_portfolio_refuses_new_positions() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit().WithCurrency(anyPortfolio.ReportingCurrency).Generate();
        anyPortfolio.Freeze();

        // Act
        Exception? error = Record.Exception(() => anyPortfolio.AddDeposit(anyDeposit));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Freezing_a_portfolio_twice_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Freeze();

        // Act
        Exception? error = Record.Exception(anyPortfolio.Freeze);

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_schedule_already_running_contributes_only_its_remaining_payments() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().WithAsOfDate(new DateOnly(2026, 6, 30)).Generate();
        CashFlowSchedule anyMonthlyLoan = Any.CashFlowSchedule()
                                             .WithScheduleId("LOAN")
                                             .WithFirstPaymentDate(new DateOnly(2026, 1, 15))
                                             .WithFrequencyMonths(1)
                                             .WithNumberOfPayments(12)
                                             .WithCurrency(anyPortfolio.ReportingCurrency)
                                             .Generate();

        // Act
        anyPortfolio.AddSchedule(anyMonthlyLoan);

        // Assert
        Assert.Equal(["LOAN-7", "LOAN-8", "LOAN-9", "LOAN-10", "LOAN-11", "LOAN-12"], anyPortfolio.ContractualFlows.Select(f => f.FlowId));
    }

    [Fact]
    public void A_schedule_clashing_with_an_existing_position_adds_none_of_its_payments() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(Any.ContractualFlow()
                                .WithFlowId("BOND-3")
                                .WithCurrency(anyPortfolio.ReportingCurrency)
                                .WithValueDate(anyPortfolio.AsOfDate)
                                .Generate());
        CashFlowSchedule anyBond = Any.CashFlowSchedule()
                                      .WithScheduleId("BOND")
                                      .WithFirstPaymentDate(anyPortfolio.AsOfDate.AddDays(1))
                                      .WithNumberOfPayments(5)
                                      .WithCurrency(anyPortfolio.ReportingCurrency)
                                      .Generate();

        // Act
        Exception? error = Record.Exception(() => anyPortfolio.AddSchedule(anyBond));

        // Assert
        Assert.IsType<ArgumentException>(error);
        Assert.Single(anyPortfolio.ContractualFlows);
    }

}
