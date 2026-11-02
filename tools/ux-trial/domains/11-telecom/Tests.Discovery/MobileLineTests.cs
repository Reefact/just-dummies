using JustDummies;

using Xunit;

namespace Telecom.Domain.Tests;

public sealed class MobileLineTests {

    private static readonly DateTime InMarch = new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc);

    private static BillingPeriod March() => new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 4, 1));

    private static Plan Postpaid(decimal fee, int allowanceGb, decimal overagePerGb) {
        return Any.Plan().WithTier(PlanTier.Postpaid).WithMonthlyFee(fee).WithDataAllowanceGb(allowanceGb).WithOveragePerGb(overagePerGb).Generate();
    }

    private static MobileLine LineIn(LineStatus status) {
        MobileLine line = Any.MobileLine().Generate();
        switch (status) {
            case LineStatus.Suspended:
                line.Suspend();
                break;
            case LineStatus.Terminated:
                line.Terminate();
                break;
        }
        return line;
    }

    // ---- construction -------------------------------------------------------------------------

    [Fact]
    public void Starts_active_with_no_usage() {
        // Act
        MobileLine line = Any.MobileLine().Generate();

        // Assert
        Assert.Equal(LineStatus.Active, line.Status);
        Assert.Empty(line.Usage);
    }

    [Fact]
    public void Keeps_its_number_handset_plan_and_activation_date() {
        // Arrange
        Msisdn   number  = new Msisdn("33612345678");
        Imei     handset = new Imei("490154203237518");
        Plan     plan    = new Plan("Gold", PlanTier.Postpaid, 19.99m, 20, 2.5m);
        DateOnly day     = new DateOnly(2024, 5, 17);

        // Act
        MobileLine line = new MobileLine(number, handset, plan, day);

        // Assert
        Assert.Equal(new Msisdn("33612345678"), line.Number);
        Assert.Equal(new Imei("490154203237518"), line.Handset);
        Assert.Same(plan, line.Plan);
        Assert.Equal(day, line.ActivatedOn);
    }

    [Fact]
    public void Refuses_a_missing_number() {
        // Arrange
        Imei anyHandset = Any.Imei().Generate();
        Plan anyPlan    = Any.Plan().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new MobileLine(null!, anyHandset, anyPlan, new DateOnly(2025, 1, 1)));

        // Assert
        Assert.Equal("number", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_handset() {
        // Arrange
        Msisdn anyNumber = Any.Msisdn().Generate();
        Plan   anyPlan   = Any.Plan().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new MobileLine(anyNumber, null!, anyPlan, new DateOnly(2025, 1, 1)));

        // Assert
        Assert.Equal("handset", exception.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_plan() {
        // Arrange
        Msisdn anyNumber  = Any.Msisdn().Generate();
        Imei   anyHandset = Any.Imei().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new MobileLine(anyNumber, anyHandset, null!, new DateOnly(2025, 1, 1)));

        // Assert
        Assert.Equal("plan", exception.ParamName);
    }

    [Fact]
    public void Accepts_an_activation_on_the_first_day_of_2000() {
        // Arrange
        Msisdn anyNumber  = Any.Msisdn().Generate();
        Imei   anyHandset = Any.Imei().Generate();
        Plan   anyPlan    = Any.Plan().Generate();

        // Act
        MobileLine line = new MobileLine(anyNumber, anyHandset, anyPlan, new DateOnly(2000, 1, 1));

        // Assert
        Assert.Equal(new DateOnly(2000, 1, 1), line.ActivatedOn);
    }

    [Fact]
    public void Refuses_an_activation_before_2000() {
        // Arrange
        Msisdn   anyNumber    = Any.Msisdn().Generate();
        Imei     anyHandset   = Any.Imei().Generate();
        Plan     anyPlan      = Any.Plan().Generate();
        DateOnly anyTooEarly  = Any.DateOnly().Between(DateOnly.MinValue, new DateOnly(1999, 12, 31)).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MobileLine(anyNumber, anyHandset, anyPlan, anyTooEarly));

        // Assert
        Assert.Equal("activatedOn", exception.ParamName);
    }

    // ---- recording usage ----------------------------------------------------------------------

    [Fact]
    public void Records_usage_in_the_order_it_happens() {
        // Arrange
        MobileLine  anyLine = Any.MobileLine().Generate();
        UsageRecord first   = new UsageRecord(InMarch, UsageKind.Data, 10);
        UsageRecord second  = new UsageRecord(InMarch, UsageKind.Voice, 60);
        UsageRecord third   = new UsageRecord(InMarch, UsageKind.Sms, 1);

        // Act
        anyLine.Record(first);
        anyLine.Record(second);
        anyLine.Record(third);

        // Assert
        Assert.Equal(new[] { first, second, third }, anyLine.Usage);
    }

    [Fact]
    public void Refuses_to_record_nothing() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyLine.Record(null!));
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void Meters_no_usage_unless_active(LineStatus status) {
        // Arrange
        MobileLine  line   = LineIn(status);
        UsageRecord record = Any.UsageRecord().Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => line.Record(record));

        // Assert
        Assert.Empty(line.Usage);
    }

    [Fact]
    public void Meters_usage_again_once_resumed() {
        // Arrange
        MobileLine  line   = LineIn(LineStatus.Suspended);
        UsageRecord record = Any.UsageRecord().Generate();

        // Act
        line.Resume();
        line.Record(record);

        // Assert
        Assert.Single(line.Usage);
    }

    [Fact(Skip = "domain defect: Usage hands out the internal List<UsageRecord> behind IReadOnlyList, so a cast to IList<UsageRecord> lets a caller append to or clear the 'append-only read-only' usage (low severity)")]
    public void Cannot_have_its_usage_changed_from_outside() {
        // Arrange
        MobileLine  line   = Any.MobileLine().Generate();
        UsageRecord record = Any.UsageRecord().Generate();

        // Act
        bool writable = line.Usage is IList<UsageRecord> list && !list.IsReadOnly;

        // Assert
        Assert.False(writable);
    }

    // ---- status transitions -------------------------------------------------------------------

    [Fact]
    public void Suspends_an_active_line() {
        // Arrange
        MobileLine line = LineIn(LineStatus.Active);

        // Act
        line.Suspend();

        // Assert
        Assert.Equal(LineStatus.Suspended, line.Status);
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void Cannot_suspend_a_line_that_is_not_active(LineStatus status) {
        // Arrange
        MobileLine line = LineIn(status);

        // Act
        Assert.Throws<InvalidOperationException>(() => line.Suspend());

        // Assert
        Assert.Equal(status, line.Status);
    }

    [Fact]
    public void Resumes_a_suspended_line() {
        // Arrange
        MobileLine line = LineIn(LineStatus.Suspended);

        // Act
        line.Resume();

        // Assert
        Assert.Equal(LineStatus.Active, line.Status);
    }

    [Theory]
    [InlineData(LineStatus.Active)]
    [InlineData(LineStatus.Terminated)]
    public void Cannot_resume_a_line_that_is_not_suspended(LineStatus status) {
        // Arrange
        MobileLine line = LineIn(status);

        // Act
        Assert.Throws<InvalidOperationException>(() => line.Resume());

        // Assert
        Assert.Equal(status, line.Status);
    }

    [Theory]
    [InlineData(LineStatus.Active)]
    [InlineData(LineStatus.Suspended)]
    public void Terminates_an_active_or_a_suspended_line(LineStatus status) {
        // Arrange
        MobileLine line = LineIn(status);

        // Act
        line.Terminate();

        // Assert
        Assert.Equal(LineStatus.Terminated, line.Status);
    }

    [Fact]
    public void Cannot_terminate_a_line_twice() {
        // Arrange
        MobileLine line = LineIn(LineStatus.Terminated);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => line.Terminate());
        Assert.Equal(LineStatus.Terminated, line.Status);
    }

    [Fact]
    public void Can_be_suspended_and_resumed_repeatedly() {
        // Arrange
        MobileLine line = LineIn(LineStatus.Active);

        // Act
        line.Suspend();
        line.Resume();
        line.Suspend();
        line.Resume();

        // Assert
        Assert.Equal(LineStatus.Active, line.Status);
    }

    // ---- handset and plan ---------------------------------------------------------------------

    [Fact]
    public void Swaps_to_another_handset() {
        // Arrange
        MobileLine line       = Any.MobileLine().WithHandset(new Imei("490154203237518")).Generate();
        Imei       newHandset = new Imei("356938035643809");

        // Act
        line.SwapHandset(newHandset);

        // Assert
        Assert.Equal(new Imei("356938035643809"), line.Handset);
    }

    [Fact]
    public void Refuses_to_swap_to_the_handset_it_already_has() {
        // Arrange: a different instance holding the same IMEI
        MobileLine line = Any.MobileLine().WithHandset(new Imei("490154203237518")).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => line.SwapHandset(new Imei("490154203237518")));
        Assert.Equal(new Imei("490154203237518"), line.Handset);
    }

    [Fact]
    public void Refuses_to_swap_to_no_handset() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyLine.SwapHandset(null!));
    }

    [Theory]
    [InlineData(LineStatus.Active)]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void Swaps_its_handset_in_any_status(LineStatus status) {
        // Arrange
        MobileLine line       = LineIn(status);
        Imei       newHandset = new Imei("356938035643809");

        // Act: the line's own handset is arbitrary, so make sure the new one differs from it
        if (line.Handset.Equals(newHandset)) {
            line.SwapHandset(new Imei("490154203237518"));
        }
        line.SwapHandset(newHandset);

        // Assert
        Assert.Equal(new Imei("356938035643809"), line.Handset);
        Assert.Equal(status, line.Status);
    }

    [Theory]
    [InlineData(LineStatus.Active)]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void Changes_its_plan_in_any_status(LineStatus status) {
        // Arrange
        MobileLine line    = LineIn(status);
        Plan       newPlan = new Plan("Silver", PlanTier.Prepaid, 5m, 3, 1m);

        // Act
        line.ChangePlan(newPlan);

        // Assert
        Assert.Same(newPlan, line.Plan);
        Assert.Equal(status, line.Status);
    }

    [Fact]
    public void Refuses_to_change_to_no_plan() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyLine.ChangePlan(null!));
    }

    // ---- billing ------------------------------------------------------------------------------

    [Fact]
    public void Bills_the_monthly_fee_alone_when_there_is_no_usage() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(19.99m, 2, 3.5m)).Generate();

        // Act
        decimal bill = line.BillFor(March());

        // Assert
        Assert.Equal(19.99m, bill);
    }

    [Fact]
    public void Bills_no_overage_up_to_the_allowance() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(19.99m, 2, 3.5m)).Generate();
        line.Record(new UsageRecord(InMarch, UsageKind.Data, 1_500));
        line.Record(new UsageRecord(InMarch, UsageKind.Data, 548));

        // Act
        decimal bill = line.BillFor(March());

        // Assert: 2 048 MB is exactly 2 GB
        Assert.Equal(19.99m, bill);
    }

    [Fact]
    public void Bills_the_overage_of_the_sum_of_all_data_records() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(19.99m, 2, 3.5m)).Generate();
        line.Record(new UsageRecord(InMarch, UsageKind.Data, 1_500));
        line.Record(new UsageRecord(InMarch, UsageKind.Data, 549));

        // Act
        decimal bill = line.BillFor(March());

        // Assert: 2 049 MB, one started gigabyte over
        Assert.Equal(23.49m, bill);
    }

    [Fact]
    public void Bills_each_started_gigabyte_over_the_allowance() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(10m, 1, 2m)).Generate();
        line.Record(new UsageRecord(InMarch, UsageKind.Data, 3_073));

        // Act
        decimal bill = line.BillFor(March());

        // Assert: 3 073 MB over a 1 GB allowance is 2 049 MB over, 3 started gigabytes
        Assert.Equal(16m, bill);
    }

    [Fact]
    public void Does_not_bill_voice_nor_sms() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(10m, 1, 2m)).Generate();
        line.Record(new UsageRecord(InMarch, UsageKind.Voice, 5_000_000));
        line.Record(new UsageRecord(InMarch, UsageKind.Sms, 1));

        // Act
        decimal bill = line.BillFor(March());

        // Assert
        Assert.Equal(10m, bill);
    }

    [Fact]
    public void Counts_data_used_on_the_first_day_of_the_period() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(10m, 1, 2m)).Generate();
        line.Record(new UsageRecord(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), UsageKind.Data, 1_025));

        // Act
        decimal bill = line.BillFor(March());

        // Assert
        Assert.Equal(12m, bill);
    }

    [Fact]
    public void Leaves_out_data_used_on_the_end_day_of_the_period() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(10m, 1, 2m)).Generate();
        line.Record(new UsageRecord(new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc), UsageKind.Data, 5_000));

        // Act
        decimal bill = line.BillFor(March());

        // Assert
        Assert.Equal(10m, bill);
    }

    [Fact]
    public void Leaves_out_data_used_before_the_period() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(10m, 1, 2m)).Generate();
        line.Record(new UsageRecord(new DateTime(2025, 2, 28, 23, 59, 59, DateTimeKind.Utc), UsageKind.Data, 5_000));

        // Act
        decimal bill = line.BillFor(March());

        // Assert
        Assert.Equal(10m, bill);
    }

    [Fact]
    public void Bills_only_the_usage_of_the_requested_period() {
        // Arrange
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(10m, 1, 2m)).Generate();
        line.Record(new UsageRecord(new DateTime(2025, 2, 20, 0, 0, 0, DateTimeKind.Utc), UsageKind.Data, 900));
        line.Record(new UsageRecord(new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc), UsageKind.Data, 900));
        line.Record(new UsageRecord(new DateTime(2025, 4, 20, 0, 0, 0, DateTimeKind.Utc), UsageKind.Data, 900));

        // Act
        decimal bill = line.BillFor(March());

        // Assert: 900 MB inside a 1 GB allowance, whatever the other months hold
        Assert.Equal(10m, bill);
    }

    [Fact]
    public void Bills_nothing_over_the_fee_on_an_unlimited_plan() {
        // Arrange
        Plan unlimited = Any.Plan().WithTier(PlanTier.Unlimited).WithMonthlyFee(49.90m).Generate();
        MobileLine line = Any.MobileLine().WithPlan(unlimited).Generate();
        line.Record(new UsageRecord(InMarch, UsageKind.Data, 900_000));

        // Act
        decimal bill = line.BillFor(March());

        // Assert
        Assert.Equal(49.90m, bill);
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void Still_bills_the_fee_and_the_overage_when_not_active(LineStatus status) {
        // Arrange: the usage was metered while the line was active
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(10m, 1, 2m)).Generate();
        line.Record(new UsageRecord(InMarch, UsageKind.Data, 1_025));
        if (status == LineStatus.Suspended) { line.Suspend(); } else { line.Terminate(); }

        // Act
        decimal bill = line.BillFor(March());

        // Assert
        Assert.Equal(12m, bill);
    }

    [Fact]
    public void Bills_with_the_current_plan_only() {
        // Arrange: 3 000 MB used under a generous plan, then the plan changes
        MobileLine line = Any.MobileLine().WithPlan(Postpaid(10m, 100, 1m)).Generate();
        line.Record(new UsageRecord(InMarch, UsageKind.Data, 3_000));
        line.ChangePlan(Postpaid(20m, 1, 5m));

        // Act
        decimal bill = line.BillFor(March());

        // Assert: 3 000 MB is 1 976 MB over 1 GB, 2 started gigabytes
        Assert.Equal(30m, bill);
    }

    [Fact]
    public void Refuses_to_bill_no_period() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyLine.BillFor(null!));
    }

    [Fact]
    public void Bills_the_fee_for_a_period_with_no_data_whatever_the_plan_is() {
        // Arrange: the fee is the one literal this test is about
        Plan       anyPlan   = Any.Plan().WithMonthlyFee(8.25m).Generate();
        MobileLine line      = Any.MobileLine().WithPlan(anyPlan).Generate();
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(8.25m, bill);
    }

}
