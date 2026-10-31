using JustDummies;

using LiquidityGap.Domain;

using Xunit;

namespace LiquidityGap.Tests.Regular;

public sealed class CashFlowScheduleTests {

    [Fact]
    public void A_schedule_expands_into_one_flow_per_payment_numbered_from_one() {
        // Arrange
        CashFlowSchedule anySchedule = Arbitrary.CashFlowSchedule()
                                                .WithScheduleId("LOAN-7")
                                                .WithNumberOfPayments(3)
                                                .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySchedule.Flows();

        // Assert
        Assert.Equal(["LOAN-7-1", "LOAN-7-2", "LOAN-7-3"], flows.Select(f => f.FlowId));
    }

    [Fact]
    public void Payments_are_spaced_by_the_frequency_from_the_first_payment_date() {
        // Arrange
        CashFlowSchedule anyQuarterlySchedule = Arbitrary.CashFlowSchedule()
                                                         .WithFirstPaymentDate(new DateOnly(2026, 1, 15))
                                                         .WithFrequencyMonths(3)
                                                         .WithNumberOfPayments(3)
                                                         .WithConvention(BusinessDayConvention.Unadjusted)
                                                         .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyQuarterlySchedule.Flows();

        // Assert
        Assert.Equal([new DateOnly(2026, 1, 15), new DateOnly(2026, 4, 15), new DateOnly(2026, 7, 15)], flows.Select(f => f.ValueDate));
    }

    [Fact]
    public void Every_flow_carries_the_schedule_amount_currency_and_direction() {
        // Arrange
        CashFlowSchedule anySchedule = Arbitrary.CashFlowSchedule().WithNumberOfPayments(n => n.Between(1, 24)).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySchedule.Flows();

        // Assert
        Assert.All(flows, f => {
            Assert.Equal(anySchedule.AmountPerPayment, f.Amount);
            Assert.Equal(anySchedule.Currency, f.Currency);
            Assert.Equal(anySchedule.Direction, f.Direction);
        });
    }

    [Fact]
    public void A_business_day_is_never_moved_whatever_the_convention() {
        // Arrange
        DateOnly              wednesday     = new DateOnly(2026, 6, 17);
        BusinessDayConvention anyConvention = Any.Enum<BusinessDayConvention>().Generate();

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(wednesday, anyConvention);

        // Assert
        Assert.Equal(wednesday, adjusted);
    }

    [Theory]
    [InlineData(BusinessDayConvention.Unadjusted, 2026, 6, 20)]
    [InlineData(BusinessDayConvention.Following, 2026, 6, 22)]
    [InlineData(BusinessDayConvention.Preceding, 2026, 6, 19)]
    [InlineData(BusinessDayConvention.ModifiedFollowing, 2026, 6, 22)]
    public void A_saturday_in_mid_month_moves_according_to_the_convention(BusinessDayConvention convention, int year, int month, int day) {
        // Arrange
        DateOnly saturday = new DateOnly(2026, 6, 20);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(saturday, convention);

        // Assert
        Assert.Equal(new DateOnly(year, month, day), adjusted);
    }

    [Fact]
    public void Modified_following_rolls_back_when_following_would_change_the_month() {
        // Arrange
        DateOnly lastSaturdayOfJanuary = new DateOnly(2026, 1, 31);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(lastSaturdayOfJanuary, BusinessDayConvention.ModifiedFollowing);

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 30), adjusted);
    }

}
