using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Discovery;

public sealed class CashFlowTests {

    [Fact]
    public void An_outflow_is_signed_negative() {
        // Arrange
        ContractualFlow anyOutflow = Any.ContractualFlow().WithAmount(250m).WithDirection(FlowDirection.Outflow).Generate();

        // Act
        decimal signed = anyOutflow.SignedAmount;

        // Assert
        Assert.Equal(-250m, signed);
    }

    [Fact]
    public void An_inflow_keeps_its_amount_as_signed_amount() {
        // Arrange
        ContractualFlow anyInflow = Any.ContractualFlow().WithAmount(250m).WithDirection(FlowDirection.Inflow).Generate();

        // Act
        decimal signed = anyInflow.SignedAmount;

        // Assert
        Assert.Equal(250m, signed);
    }

    [Fact]
    public void A_currency_code_in_lower_case_is_refused() {
        // Arrange
        string lowerCaseCode = "eur";

        // Act
        Exception? error = Record.Exception(() => new Currency(lowerCaseCode));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_schedule_names_its_flows_after_itself_and_their_rank() {
        // Arrange
        CashFlowSchedule anySchedule = Any.CashFlowSchedule().WithScheduleId("LOAN-42").WithNumberOfPayments(3).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySchedule.Flows();

        // Assert
        Assert.Equal(["LOAN-42-1", "LOAN-42-2", "LOAN-42-3"], flows.Select(f => f.FlowId));
    }

    [Fact]
    public void An_unadjusted_quarterly_schedule_pays_every_three_months() {
        // Arrange
        CashFlowSchedule anySchedule = Any.CashFlowSchedule()
                                          .WithFirstPaymentDate(new DateOnly(2026, 1, 31))
                                          .WithFrequencyMonths(3)
                                          .WithNumberOfPayments(3)
                                          .WithConvention(BusinessDayConvention.Unadjusted)
                                          .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySchedule.Flows();

        // Assert
        Assert.Equal([new DateOnly(2026, 1, 31), new DateOnly(2026, 4, 30), new DateOnly(2026, 7, 31)], flows.Select(f => f.ValueDate));
    }

    [Fact]
    public void Every_payment_of_a_schedule_carries_the_schedule_amount_currency_and_direction() {
        // Arrange
        Currency         euro        = new Currency("EUR");
        CashFlowSchedule anySchedule = Any.CashFlowSchedule()
                                          .WithAmountPerPayment(1_250.50m)
                                          .WithCurrency(euro)
                                          .WithDirection(FlowDirection.Inflow)
                                          .WithNumberOfPayments(n => n.Between(1, 24))
                                          .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySchedule.Flows();

        // Assert
        Assert.All(flows, f => {
            Assert.Equal(1_250.50m, f.Amount);
            Assert.Equal(euro, f.Currency);
            Assert.Equal(FlowDirection.Inflow, f.Direction);
        });
    }

    [Theory]
    [InlineData(BusinessDayConvention.Unadjusted, 2026, 1, 31)]        // a Saturday stays a Saturday
    [InlineData(BusinessDayConvention.Following, 2026, 2, 2)]          // rolls forward into February
    [InlineData(BusinessDayConvention.ModifiedFollowing, 2026, 1, 30)] // Following would change month: rolls back
    [InlineData(BusinessDayConvention.Preceding, 2026, 1, 30)]
    public void A_month_end_saturday_is_moved_by_the_business_day_convention(BusinessDayConvention convention, int year, int month, int day) {
        // Arrange
        DateOnly saturday = new DateOnly(2026, 1, 31);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(saturday, convention);

        // Assert
        Assert.Equal(new DateOnly(year, month, day), adjusted);
    }

    [Fact]
    public void A_mid_month_sunday_under_modified_following_rolls_to_monday() {
        // Arrange
        DateOnly sunday = new DateOnly(2026, 3, 15);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(sunday, BusinessDayConvention.ModifiedFollowing);

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 16), adjusted);
    }

    [Fact]
    public void A_business_day_is_never_moved() {
        // Arrange
        DateOnly                wednesday      = new DateOnly(2026, 3, 18);
        BusinessDayConvention anyConvention = Any.Enum<BusinessDayConvention>().Generate();

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(wednesday, anyConvention);

        // Assert
        Assert.Equal(wednesday, adjusted);
    }

}
