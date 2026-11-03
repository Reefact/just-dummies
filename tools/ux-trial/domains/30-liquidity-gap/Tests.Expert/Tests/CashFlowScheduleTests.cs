using System.Globalization;

namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class CashFlowScheduleTests {

    private static DateOnly Date(string isoDate) {
        return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    // ----- Flows() -----

    [Fact]
    public void A_quarterly_schedule_expands_into_one_numbered_flow_per_payment() {
        // Arrange
        Currency euro = new Currency("EUR");
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithScheduleId("LOAN")
                                       .WithFirstPaymentDate(Date("2026-01-15"))
                                       .WithFrequencyMonths(3)
                                       .WithNumberOfPayments(3)
                                       .WithAmountPerPayment(250.00m)
                                       .WithCurrency(euro)
                                       .WithDirection(FlowDirection.Inflow)
                                       .WithConvention(BusinessDayConvention.Unadjusted)
                                       .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.Equal(["LOAN-1", "LOAN-2", "LOAN-3"], flows.Select(f => f.FlowId));
        Assert.Equal([Date("2026-01-15"), Date("2026-04-15"), Date("2026-07-15")], flows.Select(f => f.ValueDate));
        Assert.All(flows, flow => {
            Assert.Equal(250.00m, flow.Amount);
            Assert.Equal(euro, flow.Currency);
            Assert.Equal(FlowDirection.Inflow, flow.Direction);
        });
    }

    [Fact]
    public void Every_flow_of_an_outgoing_schedule_is_an_outflow() {
        // Arrange
        CashFlowSchedule anyBondSchedule = Any.CashFlowSchedule().WithNumberOfPayments(4).WithDirection(FlowDirection.Outflow).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anyBondSchedule.Flows();

        // Assert
        Assert.All(flows, flow => Assert.Equal(FlowDirection.Outflow, flow.Direction));
    }

    [Fact]
    public void The_longest_schedule_expands_into_600_flows() {
        // Arrange
        CashFlowSchedule anySchedule = Any.CashFlowSchedule().WithScheduleId("SWAP").WithNumberOfPayments(600).WithFrequencyMonths(1).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySchedule.Flows();

        // Assert
        Assert.Equal(600, flows.Count);
        Assert.Equal("SWAP-600", flows[^1].FlowId);
    }

    [Fact]
    public void A_single_payment_schedule_expands_into_one_flow() {
        // Arrange
        CashFlowSchedule anySchedule = Any.CashFlowSchedule().WithNumberOfPayments(1).Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = anySchedule.Flows();

        // Assert
        Assert.Single(flows);
    }

    [Fact]
    public void A_month_end_payment_date_returns_to_the_month_end_after_a_shorter_month() {
        // Arrange: the dates are counted from the first payment, so February's 28th does not drag March down
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithFirstPaymentDate(Date("2026-01-31"))
                                       .WithFrequencyMonths(1)
                                       .WithNumberOfPayments(3)
                                       .WithConvention(BusinessDayConvention.Unadjusted)
                                       .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.Equal([Date("2026-01-31"), Date("2026-02-28"), Date("2026-03-31")], flows.Select(f => f.ValueDate));
    }

    [Fact]
    public void Payments_falling_on_a_weekend_are_moved_by_the_schedules_convention() {
        // Arrange: 2026-02-15 and 2026-03-15 are Sundays
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithFirstPaymentDate(Date("2026-01-15"))
                                       .WithFrequencyMonths(1)
                                       .WithNumberOfPayments(3)
                                       .WithConvention(BusinessDayConvention.Following)
                                       .Generate();

        // Act
        IReadOnlyList<ContractualFlow> flows = schedule.Flows();

        // Assert
        Assert.Equal([Date("2026-01-15"), Date("2026-02-16"), Date("2026-03-16")], flows.Select(f => f.ValueDate));
    }

    [Fact]
    public void The_last_payment_date_is_the_unadjusted_date_of_the_last_payment() {
        // Arrange: 2026-01-15 plus three quarters is 2026-10-15, a Thursday; the schedule has four payments
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithFirstPaymentDate(Date("2026-01-15"))
                                       .WithFrequencyMonths(3)
                                       .WithNumberOfPayments(4)
                                       .WithConvention(BusinessDayConvention.Following)
                                       .Generate();

        // Assert
        Assert.Equal(Date("2026-10-15"), schedule.LastPaymentDate);
    }

    [Fact]
    public void The_last_payment_date_ignores_the_convention() {
        // Arrange: 2026-03-14 is a Saturday
        CashFlowSchedule schedule = Any.CashFlowSchedule()
                                       .WithFirstPaymentDate(Date("2026-03-14"))
                                       .WithNumberOfPayments(1)
                                       .WithConvention(BusinessDayConvention.Following)
                                       .Generate();

        // Assert
        Assert.Equal(Date("2026-03-14"), schedule.LastPaymentDate);
        Assert.Equal(Date("2026-03-16"), schedule.Flows()[0].ValueDate);
    }

    // ----- Adjust -----

    [Theory]
    [InlineData("2026-03-18", BusinessDayConvention.Unadjusted)]       // a Wednesday
    [InlineData("2026-03-18", BusinessDayConvention.Following)]
    [InlineData("2026-03-18", BusinessDayConvention.ModifiedFollowing)]
    [InlineData("2026-03-18", BusinessDayConvention.Preceding)]
    [InlineData("2026-03-13", BusinessDayConvention.Following)]        // a Friday
    [InlineData("2026-03-16", BusinessDayConvention.Preceding)]        // a Monday
    public void A_business_day_is_never_moved(string businessDay, BusinessDayConvention convention) {
        // Assert
        Assert.Equal(Date(businessDay), CashFlowSchedule.Adjust(Date(businessDay), convention));
    }

    [Theory]
    [InlineData("2026-03-14", BusinessDayConvention.Unadjusted, "2026-03-14")]         // Saturday
    [InlineData("2026-03-15", BusinessDayConvention.Unadjusted, "2026-03-15")]         // Sunday
    [InlineData("2026-03-14", BusinessDayConvention.Following, "2026-03-16")]
    [InlineData("2026-03-15", BusinessDayConvention.Following, "2026-03-16")]
    [InlineData("2026-03-14", BusinessDayConvention.Preceding, "2026-03-13")]
    [InlineData("2026-03-15", BusinessDayConvention.Preceding, "2026-03-13")]
    [InlineData("2026-03-14", BusinessDayConvention.ModifiedFollowing, "2026-03-16")]  // mid-month: like Following
    [InlineData("2026-03-15", BusinessDayConvention.ModifiedFollowing, "2026-03-16")]
    public void A_weekend_day_is_moved_to_the_business_day_the_convention_names(string weekend, BusinessDayConvention convention, string expected) {
        // Assert
        Assert.Equal(Date(expected), CashFlowSchedule.Adjust(Date(weekend), convention));
    }

    [Theory]
    [InlineData("2026-01-31", "2026-01-30")]   // Saturday: Following would reach Monday 2 February
    [InlineData("2026-05-31", "2026-05-29")]   // Sunday: Following would reach Monday 1 June
    public void Modified_following_rolls_back_rather_than_into_the_next_month(string monthEndWeekend, string expected) {
        // Assert
        Assert.Equal(Date(expected), CashFlowSchedule.Adjust(Date(monthEndWeekend), BusinessDayConvention.ModifiedFollowing));
    }

    [Fact]
    public void An_unknown_convention_is_refused_when_a_weekend_needs_it() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => CashFlowSchedule.Adjust(Date("2026-03-14"), (BusinessDayConvention)99));

        // Assert
        Assert.Equal("convention", refusal.ParamName);
    }

    // ----- construction -----

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    public void A_frequency_of_one_three_six_or_twelve_months_is_accepted(int months) {
        // Act
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithFrequencyMonths(months).Generate();

        // Assert
        Assert.Equal(months, schedule.FrequencyMonths);
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(13)]
    [InlineData(24)]
    public void Any_other_frequency_is_refused(int months) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CashFlowSchedule().WithFrequencyMonths(months).Generate());

        // Assert
        Assert.Equal("frequencyMonths", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(600)]
    public void From_one_to_600_payments_are_accepted(int payments) {
        // Act
        CashFlowSchedule schedule = Any.CashFlowSchedule().WithNumberOfPayments(payments).Generate();

        // Assert
        Assert.Equal(payments, schedule.NumberOfPayments);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(601)]
    public void Fewer_than_one_or_more_than_600_payments_are_refused(int payments) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CashFlowSchedule().WithNumberOfPayments(payments).Generate());

        // Assert
        Assert.Equal("numberOfPayments", refusal.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void A_payment_that_is_not_strictly_positive_is_refused(string amount) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.CashFlowSchedule().WithAmountPerPayment(decimal.Parse(amount, CultureInfo.InvariantCulture)).Generate());

        // Assert
        Assert.Equal("amountPerPayment", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_identifier_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.CashFlowSchedule().WithScheduleId(blank).Generate());

        // Assert
        Assert.Equal("scheduleId", refusal.ParamName);
    }

    [Fact]
    public void A_missing_currency_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.CashFlowSchedule().WithCurrency((Currency)null!).Generate());

        // Assert
        Assert.Equal("currency", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_direction_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CashFlowSchedule().WithDirection((FlowDirection)7).Generate());

        // Assert
        Assert.Equal("direction", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_convention_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CashFlowSchedule().WithConvention((BusinessDayConvention)7).Generate());

        // Assert
        Assert.Equal("convention", refusal.ParamName);
    }

}
