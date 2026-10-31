using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Expert;

public sealed class PortfolioLoadingTests {

    [Fact]
    public void A_frozen_portfolio_refuses_new_flows() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        ContractualFlow anyFlow = Any.ContractualFlow().BookableIn(anyPortfolio).Generate();
        anyPortfolio.Freeze();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.AddFlow(anyFlow));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Empty(anyPortfolio.ContractualFlows);
    }

    [Fact]
    public void Freezing_a_portfolio_twice_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.Freeze();

        // Act
        Exception? refusal = Record.Exception(anyPortfolio.Freeze);

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_flow_in_a_foreign_currency_is_refused() {
        // Arrange
        Portfolio anyEuroPortfolio = Any.Portfolio().WithReportingCurrency(new Currency("EUR")).Generate();
        ContractualFlow anyForeignFlow = Any.ContractualFlow()
                                            .BookableIn(anyEuroPortfolio)
                                            .WithCurrency(c => c.WithCode(code => code.DifferentFrom("EUR")))
                                            .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyEuroPortfolio.AddFlow(anyForeignFlow));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Empty(anyEuroPortfolio.ContractualFlows);
    }

    [Fact]
    public void A_flow_that_settled_before_the_as_of_date_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().WithAsOfDate(new DateOnly(2026, 6, 30)).Generate();
        ContractualFlow anyPastFlow = Any.ContractualFlow()
                                         .BookableIn(anyPortfolio)
                                         .WithValueDate(new DateOnly(2026, 6, 29))
                                         .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.AddFlow(anyPastFlow));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_flow_settling_on_the_as_of_date_is_accepted() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().WithAsOfDate(new DateOnly(2026, 6, 30)).Generate();
        ContractualFlow anyFlowToday = Any.ContractualFlow()
                                          .BookableIn(anyPortfolio)
                                          .WithValueDate(new DateOnly(2026, 6, 30))
                                          .Generate();

        // Act
        anyPortfolio.AddFlow(anyFlowToday);

        // Assert
        Assert.Single(anyPortfolio.ContractualFlows);
    }

    [Fact]
    public void A_deposit_reusing_the_id_of_a_flow_is_refused() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(Any.ContractualFlow().BookableIn(anyPortfolio).WithFlowId("POS-1").Generate());
        NonMaturityDeposit anyDeposit = Any.NonMaturityDeposit().BookableIn(anyPortfolio).WithDepositId("POS-1").Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.AddDeposit(anyDeposit));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Empty(anyPortfolio.Deposits);
    }

    [Fact]
    public void Only_the_payments_of_a_schedule_due_on_or_after_the_as_of_date_are_loaded() {
        // Arrange
        // Monthly on the 15th, January to December 2026: a weekend roll moves a payment by two days at
        // most, so none crosses the 30 June cut-off and the convention stays a dummy.
        Portfolio anyPortfolio = Any.Portfolio().WithAsOfDate(new DateOnly(2026, 6, 30)).Generate();
        CashFlowSchedule anyMonthlyLoan = Any.CashFlowSchedule()
                                             .BookableIn(anyPortfolio)
                                             .WithScheduleId("LOAN-42")
                                             .WithFirstPaymentDate(new DateOnly(2026, 1, 15))
                                             .WithFrequencyMonths(1)
                                             .WithNumberOfPayments(12)
                                             .Generate();

        // Act
        anyPortfolio.AddSchedule(anyMonthlyLoan);

        // Assert
        Assert.Equal(["LOAN-42-7", "LOAN-42-8", "LOAN-42-9", "LOAN-42-10", "LOAN-42-11", "LOAN-42-12"],
                     anyPortfolio.ContractualFlows.Select(f => f.FlowId));
    }

    [Fact]
    public void A_schedule_clashing_with_a_loaded_position_is_refused_as_a_whole() {
        // Arrange
        Portfolio anyPortfolio = Any.Portfolio().Generate();
        anyPortfolio.AddFlow(Any.ContractualFlow().BookableIn(anyPortfolio).WithFlowId("BOND-9-2").Generate());
        CashFlowSchedule anyCoupons = Any.CashFlowSchedule()
                                         .BookableIn(anyPortfolio)
                                         .WithScheduleId("BOND-9")
                                         .WithFirstPaymentDate(anyPortfolio.AsOfDate)
                                         .WithNumberOfPayments(n => n.Between(2, 40))
                                         .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPortfolio.AddSchedule(anyCoupons));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
        Assert.Single(anyPortfolio.ContractualFlows);
    }

}
