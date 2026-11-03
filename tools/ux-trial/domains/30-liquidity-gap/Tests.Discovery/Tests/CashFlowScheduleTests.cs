using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class CashFlowScheduleTests {

    private static AnyCashFlowSchedule Unadjusted() {
        return Any.CashFlowSchedule().WithConvention(BusinessDayConvention.Unadjusted);
    }

    [Fact]
    public void A_schedule_expands_into_one_flow_per_payment_numbered_from_one() {
        // Arrange
        CashFlowSchedule schedule = Unadjusted().WithScheduleId("LOAN").WithNumberOfPayments(3).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.Equal(new[] { "LOAN-1", "LOAN-2", "LOAN-3" }, flows.Select(f => f.FlowId).ToArray());
    }

    [Fact]
    public void Every_flow_carries_the_amount_currency_and_direction_of_the_schedule() {
        // Arrange
        CashFlowSchedule schedule = Unadjusted().WithNumberOfPayments(4)
                                                .WithAmountPerPayment(2500.25m)
                                                .WithCurrency(Ladders.Usd)
                                                .WithDirection(FlowDirection.Inflow)
                                                .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.All(flows, flow => {
            Assert.Equal(2500.25m, flow.Amount);
            Assert.Equal(Ladders.Usd, flow.Currency);
            Assert.Equal(FlowDirection.Inflow, flow.Direction);
        });
    }

    [Fact]
    public void Quarterly_payments_fall_three_months_apart() {
        // Arrange: 2026-01-15, 04-15, 07-15 and 10-15 are all weekdays
        CashFlowSchedule schedule = Unadjusted().WithFirstPaymentDate(new DateOnly(2026, 1, 15))
                                                .WithFrequencyMonths(3)
                                                .WithNumberOfPayments(4)
                                                .Generate();

        // Act
        DateOnly[] dates = schedule.Flows().Select(f => f.ValueDate).ToArray();

        // Assert
        Assert.Equal(new[] { new DateOnly(2026, 1, 15), new DateOnly(2026, 4, 15), new DateOnly(2026, 7, 15), new DateOnly(2026, 10, 15) }, dates);
    }

    [Fact]
    public void Month_end_payments_are_counted_from_the_first_date_not_from_the_previous_one() {
        // Arrange: 2026-01-31 + 1 month = 2026-02-28, but + 2 months = 2026-03-31, not 03-28
        CashFlowSchedule schedule = Unadjusted().WithFirstPaymentDate(new DateOnly(2026, 1, 31))
                                                .WithFrequencyMonths(1)
                                                .WithNumberOfPayments(3)
                                                .Generate();

        // Act
        DateOnly[] dates = schedule.Flows().Select(f => f.ValueDate).ToArray();

        // Assert
        Assert.Equal(new[] { new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31) }, dates);
    }

    [Theory]
    [InlineData(BusinessDayConvention.Unadjusted, 2026, 1, 31)]
    [InlineData(BusinessDayConvention.Following, 2026, 2, 2)]
    [InlineData(BusinessDayConvention.ModifiedFollowing, 2026, 1, 30)]
    [InlineData(BusinessDayConvention.Preceding, 2026, 1, 30)]
    public void A_schedule_moves_a_weekend_payment_under_its_convention(BusinessDayConvention convention, int year, int month, int day) {
        // Arrange: 2026-01-31 is a Saturday
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithFirstPaymentDate(new DateOnly(2026, 1, 31))
                                                          .WithNumberOfPayments(1)
                                                          .WithConvention(convention)
                                                          .Generate();

        // Act
        ContractualFlow flow = Assert.Single(schedule.Flows());

        // Assert
        Assert.Equal(new DateOnly(year, month, day), flow.ValueDate);
    }

    [Fact]
    public void The_last_payment_date_is_the_unadjusted_date_of_the_last_payment() {
        // Arrange: 2026-01-31 is a Saturday, and the last payment is not adjusted
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithFirstPaymentDate(new DateOnly(2026, 1, 31))
                                                          .WithFrequencyMonths(6)
                                                          .WithNumberOfPayments(3)
                                                          .WithConvention(BusinessDayConvention.Following)
                                                          .Generate();

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 31), schedule.LastPaymentDate);
    }

    [Theory]
    [InlineData(BusinessDayConvention.Unadjusted)]
    [InlineData(BusinessDayConvention.Following)]
    [InlineData(BusinessDayConvention.ModifiedFollowing)]
    [InlineData(BusinessDayConvention.Preceding)]
    public void A_weekday_is_never_moved(BusinessDayConvention convention) {
        // Arrange: 2026-03-12 is a Thursday
        DateOnly thursday = new DateOnly(2026, 3, 12);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(thursday, convention);

        // Assert
        Assert.Equal(thursday, adjusted);
    }

    [Theory]
    [InlineData(2026, 3, 14, BusinessDayConvention.Following, 2026, 3, 16)]
    [InlineData(2026, 3, 15, BusinessDayConvention.Following, 2026, 3, 16)]
    [InlineData(2026, 3, 14, BusinessDayConvention.Preceding, 2026, 3, 13)]
    [InlineData(2026, 3, 15, BusinessDayConvention.Preceding, 2026, 3, 13)]
    [InlineData(2026, 3, 14, BusinessDayConvention.ModifiedFollowing, 2026, 3, 16)]
    [InlineData(2026, 3, 14, BusinessDayConvention.Unadjusted, 2026, 3, 14)]
    [InlineData(2026, 5, 31, BusinessDayConvention.Following, 2026, 6, 1)]
    [InlineData(2026, 5, 31, BusinessDayConvention.ModifiedFollowing, 2026, 5, 29)]
    [InlineData(2026, 1, 31, BusinessDayConvention.ModifiedFollowing, 2026, 1, 30)]
    public void A_weekend_date_is_rolled_by_the_convention(int y, int m, int d, BusinessDayConvention convention, int ey, int em, int ed) {
        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(new DateOnly(y, m, d), convention);

        // Assert
        Assert.Equal(new DateOnly(ey, em, ed), adjusted);
    }

    [Fact]
    public void Adjusting_with_an_unknown_convention_is_refused_on_a_weekend() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => CashFlowSchedule.Adjust(new DateOnly(2026, 3, 14), (BusinessDayConvention)99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(24)]
    [InlineData(-3)]
    public void A_frequency_other_than_1_3_6_or_12_months_is_refused(int frequency) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CashFlowSchedule().WithFrequencyMonths(frequency).Generate());

        // Assert
        Assert.Equal("frequencyMonths", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    public void The_four_frequencies_are_accepted(int frequency) {
        // Act
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithFrequencyMonths(frequency).Generate();

        // Assert
        Assert.Equal(frequency, schedule.FrequencyMonths);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(601)]
    public void A_number_of_payments_outside_1_to_600_is_refused(int count) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CashFlowSchedule().WithNumberOfPayments(count).Generate());

        // Assert
        Assert.Equal("numberOfPayments", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(600)]
    public void The_limits_of_the_number_of_payments_are_accepted(int count) {
        // Act
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithNumberOfPayments(count).Generate();

        // Assert
        Assert.Equal(count, schedule.Flows().Count);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void A_payment_amount_that_is_not_strictly_positive_is_refused(string amount) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CashFlowSchedule().WithAmountPerPayment(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).Generate());

        // Assert
        Assert.Equal("amountPerPayment", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_schedule_id_is_refused(string scheduleId) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.CashFlowSchedule().WithScheduleId(scheduleId).Generate());

        // Assert
        Assert.Equal("scheduleId", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_direction_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CashFlowSchedule().WithDirection((FlowDirection)7).Generate());

        // Assert
        Assert.Equal("direction", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_convention_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CashFlowSchedule().WithConvention((BusinessDayConvention)7).Generate());

        // Assert
        Assert.Equal("convention", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => Any.CashFlowSchedule().WithCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

}
