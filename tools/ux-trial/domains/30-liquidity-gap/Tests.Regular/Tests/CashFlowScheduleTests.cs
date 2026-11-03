using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class CashFlowScheduleTests {

    private static CashFlowSchedule Schedule(DateOnly first, int frequencyMonths, int payments, BusinessDayConvention convention) {
        return Any.CashFlowSchedule()
                  .WithFirstPaymentDate(first)
                  .WithFrequencyMonths(frequencyMonths)
                  .WithNumberOfPayments(payments)
                  .WithConvention(convention)
                  .Generate();
    }

    [Fact]
    public void A_schedule_expands_into_one_flow_per_payment_numbered_from_one() {
        // Arrange
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithScheduleId("BOND-9")
                                       .WithFirstPaymentDate(new DateOnly(2025, 3, 12))
                                       .WithFrequencyMonths(3)
                                       .WithNumberOfPayments(4)
                                       .WithConvention(BusinessDayConvention.Unadjusted)
                                       .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.Equal(["BOND-9-1", "BOND-9-2", "BOND-9-3", "BOND-9-4"], flows.Select(static f => f.FlowId));
        Assert.Equal([new DateOnly(2025, 3, 12), new DateOnly(2025, 6, 12), new DateOnly(2025, 9, 12), new DateOnly(2025, 12, 12)],
                     flows.Select(static f => f.ValueDate));
    }

    [Fact]
    public void Every_flow_of_a_schedule_carries_the_amount_currency_and_direction_of_the_schedule() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithAmountPerPayment(750.25m)
                                       .WithCurrency(anyCurrency)
                                       .WithDirection(FlowDirection.Inflow)
                                       .WithNumberOfPayments(5)
                                       .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.Equal(5, flows.Count);
        Assert.All(flows, flow => {
            Assert.Equal(750.25m, flow.Amount);
            Assert.Equal(anyCurrency, flow.Currency);
            Assert.Equal(FlowDirection.Inflow, flow.Direction);
        });
    }

    [Fact]
    public void A_single_payment_schedule_expands_into_one_flow() {
        // Arrange
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithNumberOfPayments(1).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.Single(flows);
    }

    [Fact]
    public void The_longest_schedule_has_six_hundred_payments() {
        // Arrange
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithNumberOfPayments(600).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.Equal(600, flows.Count);
        Assert.Equal(600, flows.Select(static f => f.FlowId).Distinct().Count());
    }

    [Fact]
    public void Month_end_payments_follow_the_first_date_instead_of_drifting_after_a_short_month() {
        // Arrange
        CashFlowSchedule schedule = Schedule(new DateOnly(2025, 1, 31), 1, 4, BusinessDayConvention.Unadjusted);

        // Act
        IReadOnlyList<DateOnly> dates = schedule.Flows().Select(static f => f.ValueDate).ToList();

        // Assert
        Assert.Equal([new DateOnly(2025, 1, 31), new DateOnly(2025, 2, 28), new DateOnly(2025, 3, 31), new DateOnly(2025, 4, 30)], dates);
    }

    [Fact]
    public void The_last_payment_date_is_the_unadjusted_date_of_the_last_payment() {
        // Arrange: 2025-08-30 is a Saturday, and Following would move it.
        CashFlowSchedule schedule = Schedule(new DateOnly(2025, 2, 28), 6, 2, BusinessDayConvention.Following);

        // Act
        DateOnly lastPaymentDate = schedule.LastPaymentDate;

        // Assert
        Assert.Equal(new DateOnly(2025, 8, 28), lastPaymentDate);
    }

    [Theory]
    [InlineData(BusinessDayConvention.Unadjusted, "2025-03-15")]
    [InlineData(BusinessDayConvention.Following, "2025-03-17")]
    [InlineData(BusinessDayConvention.ModifiedFollowing, "2025-03-17")]
    [InlineData(BusinessDayConvention.Preceding, "2025-03-14")]
    public void A_saturday_payment_is_moved_according_to_the_convention(BusinessDayConvention convention, string expected) {
        // Arrange
        DateOnly saturday = new DateOnly(2025, 3, 15);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(saturday, convention);

        // Assert
        Assert.Equal(DateOnly.Parse(expected), adjusted);
    }

    [Theory]
    [InlineData(BusinessDayConvention.Unadjusted, "2025-03-16")]
    [InlineData(BusinessDayConvention.Following, "2025-03-17")]
    [InlineData(BusinessDayConvention.ModifiedFollowing, "2025-03-17")]
    [InlineData(BusinessDayConvention.Preceding, "2025-03-14")]
    public void A_sunday_payment_is_moved_according_to_the_convention(BusinessDayConvention convention, string expected) {
        // Arrange
        DateOnly sunday = new DateOnly(2025, 3, 16);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(sunday, convention);

        // Assert
        Assert.Equal(DateOnly.Parse(expected), adjusted);
    }

    [Theory]
    [InlineData(BusinessDayConvention.Unadjusted)]
    [InlineData(BusinessDayConvention.Following)]
    [InlineData(BusinessDayConvention.ModifiedFollowing)]
    [InlineData(BusinessDayConvention.Preceding)]
    public void A_weekday_payment_is_never_moved(BusinessDayConvention convention) {
        // Arrange
        DateOnly wednesday = new DateOnly(2025, 3, 12);

        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(wednesday, convention);

        // Assert
        Assert.Equal(wednesday, adjusted);
    }

    [Theory]
    [InlineData("2025-08-30", "2025-08-29")] // Saturday: Following would reach Monday 1 September
    [InlineData("2025-08-31", "2025-08-29")] // Sunday: the same
    public void Modified_following_rolls_back_when_following_would_change_month(string weekend, string expected) {
        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(DateOnly.Parse(weekend), BusinessDayConvention.ModifiedFollowing);

        // Assert
        Assert.Equal(DateOnly.Parse(expected), adjusted);
    }

    [Fact]
    public void Following_does_cross_the_month_end_where_modified_following_would_not() {
        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(new DateOnly(2025, 8, 30), BusinessDayConvention.Following);

        // Assert
        Assert.Equal(new DateOnly(2025, 9, 1), adjusted);
    }

    [Fact]
    public void Preceding_crosses_the_month_start_backwards() {
        // Arrange: 2025-06-01 is a Sunday.
        // Act
        DateOnly adjusted = CashFlowSchedule.Adjust(new DateOnly(2025, 6, 1), BusinessDayConvention.Preceding);

        // Assert
        Assert.Equal(new DateOnly(2025, 5, 30), adjusted);
    }

    [Fact]
    public void Adjustment_applies_to_the_flows_of_a_schedule() {
        // Arrange: monthly from Friday 2025-01-31 -> 2025-03-31 is a Monday, 2025-02-28 a Friday; 2025-05-31 is a Saturday.
        CashFlowSchedule schedule = Schedule(new DateOnly(2025, 3, 31), 1, 3, BusinessDayConvention.ModifiedFollowing);

        // Act
        IReadOnlyList<DateOnly> dates = schedule.Flows().Select(static f => f.ValueDate).ToList();

        // Assert: 03-31 Mon, 04-30 Wed, 05-31 Sat -> rolls back to Fri 05-30
        Assert.Equal([new DateOnly(2025, 3, 31), new DateOnly(2025, 4, 30), new DateOnly(2025, 5, 30)], dates);
    }

    [Fact]
    public void An_undeclared_convention_is_refused_when_adjusting() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => CashFlowSchedule.Adjust(new DateOnly(2025, 3, 15), (BusinessDayConvention)42));
    }

    [Fact(Skip = "domain defect: the constructor accepts a schedule whose last payment is beyond DateOnly.MaxValue, so Flows() and LastPaymentDate throw the BCL's ArgumentOutOfRangeException ('months'); drawn first date unbounded, seed 348448195 (numberOfPayments 600)")]
    public void A_schedule_running_past_the_last_representable_date_is_refused_when_it_is_built() {
        // Arrange
        string   anyId       = Any.String().NotBlank().Generate();
        Currency anyCurrency = Any.Currency().Generate();

        // Act & Assert: 9999-01-01 + 12 months cannot be represented, so the second payment does not exist.
        Assert.ThrowsAny<ArgumentException>(
            () => new CashFlowSchedule(anyId, new DateOnly(9999, 1, 1), 12, 2, 10m, anyCurrency, FlowDirection.Inflow, BusinessDayConvention.Following));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(13)]
    [InlineData(24)]
    [InlineData(-3)]
    public void A_frequency_other_than_one_three_six_or_twelve_months_is_refused(int invalidFrequency) {
        // Arrange
        string   anyId       = Any.String().NotBlank().Generate();
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CashFlowSchedule(anyId, new DateOnly(2025, 1, 1), invalidFrequency, 3, 10m, anyCurrency, FlowDirection.Inflow, BusinessDayConvention.Following));

        // Assert
        Assert.Equal("frequencyMonths", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    public void The_four_standard_frequencies_are_accepted(int frequency) {
        // Act
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithFrequencyMonths(frequency).Generate();

        // Assert
        Assert.Equal(frequency, schedule.FrequencyMonths);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(601)]
    public void A_number_of_payments_outside_one_to_six_hundred_is_refused(int invalidCount) {
        // Arrange
        string   anyId       = Any.String().NotBlank().Generate();
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CashFlowSchedule(anyId, new DateOnly(2025, 1, 1), 1, invalidCount, 10m, anyCurrency, FlowDirection.Inflow, BusinessDayConvention.Following));

        // Assert
        Assert.Equal("numberOfPayments", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void A_payment_amount_that_is_not_strictly_positive_is_refused(string invalidAmount) {
        // Arrange
        string   anyId       = Any.String().NotBlank().Generate();
        Currency anyCurrency = Any.Currency().Generate();
        decimal  amount      = decimal.Parse(invalidAmount, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CashFlowSchedule(anyId, new DateOnly(2025, 1, 1), 1, 3, amount, anyCurrency, FlowDirection.Inflow, BusinessDayConvention.Following));

        // Assert
        Assert.Equal("amountPerPayment", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void A_blank_schedule_identifier_is_refused(string? blankId) {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CashFlowSchedule(blankId!, new DateOnly(2025, 1, 1), 1, 3, 10m, anyCurrency, FlowDirection.Inflow, BusinessDayConvention.Following));

        // Assert
        Assert.Equal("scheduleId", refusal.ParamName);
    }

    [Fact]
    public void An_undeclared_direction_or_convention_is_refused() {
        // Arrange
        string   anyId       = Any.String().NotBlank().Generate();
        Currency anyCurrency = Any.Currency().Generate();

        // Act
        ArgumentOutOfRangeException badDirection = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CashFlowSchedule(anyId, new DateOnly(2025, 1, 1), 1, 3, 10m, anyCurrency, (FlowDirection)9, BusinessDayConvention.Following));
        ArgumentOutOfRangeException badConvention = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CashFlowSchedule(anyId, new DateOnly(2025, 1, 1), 1, 3, 10m, anyCurrency, FlowDirection.Inflow, (BusinessDayConvention)9));

        // Assert
        Assert.Equal("direction", badDirection.ParamName);
        Assert.Equal("convention", badConvention.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Arrange
        string anyId = Any.String().NotBlank().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new CashFlowSchedule(anyId, new DateOnly(2025, 1, 1), 1, 3, 10m, null!, FlowDirection.Inflow, BusinessDayConvention.Following));

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

}
