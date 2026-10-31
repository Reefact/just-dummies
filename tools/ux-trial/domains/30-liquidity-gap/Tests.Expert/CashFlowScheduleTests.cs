using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Expert;

public sealed class CashFlowScheduleTests {

    [Fact]
    public void Payments_are_identified_by_the_schedule_id_and_their_rank_from_one() {
        // Arrange
        CashFlowSchedule anySchedule = Any.CashFlowSchedule()
                                          .WithScheduleId("LOAN-42")
                                          .WithNumberOfPayments(3)
                                          .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySchedule.Flows();

        // Assert
        Assert.Equal(["LOAN-42-1", "LOAN-42-2", "LOAN-42-3"], flows.Select(f => f.FlowId));
    }

    [Fact]
    public void A_quarterly_schedule_pays_every_three_months_from_its_first_payment() {
        // Arrange
        // Every date below is a business day, so the convention has nothing to move: it is a dummy.
        CashFlowSchedule anyQuarterlySchedule = Any.CashFlowSchedule()
                                                   .WithFirstPaymentDate(new DateOnly(2026, 1, 15))
                                                   .WithFrequencyMonths(3)
                                                   .WithNumberOfPayments(4)
                                                   .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyQuarterlySchedule.Flows();

        // Assert
        Assert.Equal([new DateOnly(2026, 1, 15), new DateOnly(2026, 4, 15), new DateOnly(2026, 7, 15), new DateOnly(2026, 10, 15)],
                     flows.Select(f => f.ValueDate));
        Assert.Equal(new DateOnly(2026, 10, 15), anyQuarterlySchedule.LastPaymentDate);
    }

    [Fact]
    public void Each_payment_carries_the_schedule_amount_currency_and_direction() {
        // Arrange
        Currency euro = new("EUR");
        CashFlowSchedule anyRepayment = Any.CashFlowSchedule()
                                           .WithAmountPerPayment(1_250.00m)
                                           .WithCurrency(euro)
                                           .WithDirection(FlowDirection.Inflow)
                                           .WithNumberOfPayments(n => n.Between(1, 24))
                                           .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyRepayment.Flows();

        // Assert
        Assert.All(flows, flow => {
            Assert.Equal(1_250.00m, flow.Amount);
            Assert.Equal(euro, flow.Currency);
            Assert.Equal(FlowDirection.Inflow, flow.Direction);
        });
    }

    [Fact]
    public void A_business_day_is_never_moved_whatever_the_convention() {
        // Arrange
        DateOnly              thursday      = new(2026, 3, 12);
        BusinessDayConvention anyConvention = Any.Enum<BusinessDayConvention>().Generate();

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(thursday, anyConvention);

        // Assert
        Assert.Equal(thursday, adjusted);
    }

    [Theory]
    [InlineData(BusinessDayConvention.Unadjusted, "2026-05-16")]        // the Saturday stays
    [InlineData(BusinessDayConvention.Following, "2026-05-18")]         // to Monday
    [InlineData(BusinessDayConvention.Preceding, "2026-05-15")]         // to Friday
    [InlineData(BusinessDayConvention.ModifiedFollowing, "2026-05-18")] // to Monday, still in May
    public void A_mid_month_saturday_payment_is_moved_by_the_convention(BusinessDayConvention convention, string expected) {
        // Arrange
        DateOnly saturday = new(2026, 5, 16);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(saturday, convention);

        // Assert
        Assert.Equal(DateOnly.Parse(expected), adjusted);
    }

    [Fact]
    public void Modified_following_rolls_back_when_following_would_leave_the_month() {
        // Arrange
        DateOnly saturdayAtMonthEnd = new(2026, 1, 31);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(saturdayAtMonthEnd, BusinessDayConvention.ModifiedFollowing);

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 30), adjusted);
    }

}
