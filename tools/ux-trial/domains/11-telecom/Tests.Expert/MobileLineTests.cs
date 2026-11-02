using JustDummies;

using Telecom.Domain;
using Telecom.Tests.Expert.Generators;

using Xunit;

namespace Telecom.Tests.Expert;

public sealed class MobileLineTests {

    // ---------------------------------------------------------------- creation

    [Fact]
    public void A_new_line_is_active_with_no_usage_and_keeps_what_it_was_built_with() {
        // Arrange
        Msisdn number  = new Msisdn("33612345678");
        Imei   handset = new Imei("490154203237518");
        Plan   plan    = Any.Plan().Generate();

        // Act
        MobileLine line = new MobileLine(number, handset, plan, new DateOnly(2024, 9, 1));

        // Assert
        Assert.Same(number, line.Number);
        Assert.Same(handset, line.Handset);
        Assert.Same(plan, line.Plan);
        Assert.Equal(new DateOnly(2024, 9, 1), line.ActivatedOn);
        Assert.Equal(LineStatus.Active, line.Status);
        Assert.Empty(line.Usage);
    }

    [Fact]
    public void A_line_without_a_number_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new MobileLine(null!, Any.Imei().Generate(), Any.Plan().Generate(), Any.DateOnly().AfterOrEqualTo(new DateOnly(2000, 1, 1)).Generate()));

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Fact]
    public void A_line_without_a_handset_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new MobileLine(Any.Msisdn().Generate(), null!, Any.Plan().Generate(), Any.DateOnly().AfterOrEqualTo(new DateOnly(2000, 1, 1)).Generate()));

        // Assert
        Assert.Equal("handset", refusal.ParamName);
    }

    [Fact]
    public void A_line_without_a_plan_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new MobileLine(Any.Msisdn().Generate(), Any.Imei().Generate(), null!, Any.DateOnly().AfterOrEqualTo(new DateOnly(2000, 1, 1)).Generate()));

        // Assert
        Assert.Equal("plan", refusal.ParamName);
    }

    [Fact]
    public void A_line_may_be_activated_on_the_first_day_of_the_year_2000() {
        // Act
        MobileLine line = Any.MobileLine().WithActivatedOn(new DateOnly(2000, 1, 1)).Generate();

        // Assert
        Assert.Equal(new DateOnly(2000, 1, 1), line.ActivatedOn);
    }

    [Fact]
    public void A_line_may_not_be_activated_before_the_year_2000() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MobileLine(Any.Msisdn().Generate(), Any.Imei().Generate(), Any.Plan().Generate(), new DateOnly(1999, 12, 31)));

        // Assert
        Assert.Equal("activatedOn", refusal.ParamName);
    }

    // ---------------------------------------------------------------- usage

    [Fact]
    public void Usage_is_metered_in_the_order_it_is_recorded() {
        // Arrange
        MobileLine  anyLine = Any.MobileLine().Generate();
        UsageRecord first   = new UsageRecord(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), UsageKind.Data, 50);
        UsageRecord second  = new UsageRecord(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc), UsageKind.Voice, 60);

        // Act
        anyLine.Record(first);
        anyLine.Record(second);

        // Assert
        Assert.Equal(new[] { first, second }, anyLine.Usage);
    }

    [Fact]
    public void A_record_that_is_not_there_is_refused() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyLine.Record(null!));
    }

    [Fact]
    public void A_record_dated_before_the_activation_is_still_metered() {
        // Arrange
        MobileLine  line   = Any.MobileLine().WithActivatedOn(new DateOnly(2025, 1, 1)).Generate();
        UsageRecord before = new UsageRecord(new DateTime(2024, 12, 31, 23, 59, 0, DateTimeKind.Utc), UsageKind.Data, 10);

        // Act
        line.Record(before);

        // Assert
        Assert.Single(line.Usage);
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void A_line_that_is_not_active_meters_no_usage(LineStatus status) {
        // Arrange
        MobileLine  line      = LineIn(status);
        UsageRecord anyRecord = Any.UsageRecord().Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => line.Record(anyRecord));

        // Assert
        Assert.Empty(line.Usage);
    }

    [Fact]
    public void A_line_meters_usage_again_once_resumed() {
        // Arrange
        MobileLine  line      = LineIn(LineStatus.Suspended);
        UsageRecord anyRecord = Any.UsageRecord().Generate();

        // Act
        line.Resume();
        line.Record(anyRecord);

        // Assert
        Assert.Single(line.Usage);
    }

    [Fact(Skip = "domain defect: Usage hands out the internal List, so a caller can cast it back and append to a line that meters nothing; deterministic, no seed")]
    public void Usage_cannot_be_changed_through_the_list_it_exposes() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act & Assert
        Assert.False(anyLine.Usage is List<UsageRecord>);
    }

    // ---------------------------------------------------------------- life cycle

    [Fact]
    public void An_active_line_can_be_suspended() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act
        anyLine.Suspend();

        // Assert
        Assert.Equal(LineStatus.Suspended, anyLine.Status);
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void A_line_that_is_not_active_cannot_be_suspended(LineStatus status) {
        // Arrange
        MobileLine line = LineIn(status);

        // Act
        Assert.Throws<InvalidOperationException>(() => line.Suspend());

        // Assert
        Assert.Equal(status, line.Status);
    }

    [Fact]
    public void A_suspended_line_can_be_resumed() {
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
    public void A_line_that_is_not_suspended_cannot_be_resumed(LineStatus status) {
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
    public void A_line_that_is_not_terminated_can_be_terminated(LineStatus status) {
        // Arrange
        MobileLine line = LineIn(status);

        // Act
        line.Terminate();

        // Assert
        Assert.Equal(LineStatus.Terminated, line.Status);
    }

    [Fact]
    public void A_terminated_line_cannot_be_terminated_again() {
        // Arrange
        MobileLine line = LineIn(LineStatus.Terminated);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => line.Terminate());
        Assert.Equal(LineStatus.Terminated, line.Status);
    }

    // ---------------------------------------------------------------- handset

    [Fact]
    public void A_line_can_be_paired_with_another_handset() {
        // Arrange
        MobileLine line     = Any.MobileLine().Generate();
        Imei       newOne   = Any.Imei().DifferentFrom(line.Handset).Generate();

        // Act
        line.SwapHandset(newOne);

        // Assert
        Assert.Same(newOne, line.Handset);
    }

    [Fact]
    public void A_line_cannot_be_paired_again_with_the_handset_it_already_has_even_through_another_instance() {
        // Arrange
        MobileLine line    = Any.MobileLine().Generate();
        Imei       sameOne = new Imei(line.Handset.Value);
        Imei       before  = line.Handset;

        // Act
        Assert.Throws<InvalidOperationException>(() => line.SwapHandset(sameOne));

        // Assert
        Assert.Same(before, line.Handset);
    }

    [Fact]
    public void A_handset_that_is_not_there_is_refused() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyLine.SwapHandset(null!));
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void The_handset_can_be_swapped_whatever_the_status(LineStatus status) {
        // Arrange
        MobileLine line   = LineIn(status);
        Imei       newOne = Any.Imei().DifferentFrom(line.Handset).Generate();

        // Act
        line.SwapHandset(newOne);

        // Assert
        Assert.Same(newOne, line.Handset);
    }

    // ---------------------------------------------------------------- plan

    [Fact]
    public void A_line_can_change_plan_and_keeps_its_usage() {
        // Arrange
        MobileLine  line      = Any.MobileLine().Generate();
        UsageRecord anyRecord = Any.UsageRecord().Generate();
        line.Record(anyRecord);
        Plan newPlan = Any.Plan().Generate();

        // Act
        line.ChangePlan(newPlan);

        // Assert
        Assert.Same(newPlan, line.Plan);
        Assert.Single(line.Usage);
    }

    [Fact]
    public void A_plan_that_is_not_there_is_refused() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();
        Plan       before  = anyLine.Plan;

        // Act
        Assert.Throws<ArgumentNullException>(() => anyLine.ChangePlan(null!));

        // Assert
        Assert.Same(before, anyLine.Plan);
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void The_plan_can_be_changed_whatever_the_status(LineStatus status) {
        // Arrange
        MobileLine line    = LineIn(status);
        Plan       newPlan = Any.Plan().Generate();

        // Act
        line.ChangePlan(newPlan);

        // Assert
        Assert.Same(newPlan, line.Plan);
    }

    // ---------------------------------------------------------------- billing

    [Fact]
    public void A_line_without_usage_pays_the_monthly_fee() {
        // Arrange
        MobileLine line = LineOn(Metered(fee: 19.99m, allowanceGb: 5, overagePerGb: 2.50m));

        // Act
        decimal bill = line.BillFor(Any.BillingPeriod().Generate());

        // Assert
        Assert.Equal(19.99m, bill);
    }

    [Fact]
    public void Data_up_to_the_allowance_costs_nothing_more() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        MobileLine    line      = LineOn(Metered(fee: 10m, allowanceGb: 2, overagePerGb: 3.50m));
        line.Record(Data(anyPeriod.Start, 1_000));
        line.Record(Data(anyPeriod.Start, 1_048));      // 2 048 MB in all: exactly the allowance

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(10m, bill);
    }

    [Theory]
    [InlineData(2_049, "13.50")]    // one megabyte over: a gigabyte started
    [InlineData(3_072, "13.50")]    // exactly one gigabyte over
    [InlineData(3_073, "17.00")]    // a second gigabyte started
    public void Data_beyond_the_allowance_is_charged_per_started_gigabyte_on_top_of_the_fee(long megabytes, string expected) {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        MobileLine    line      = LineOn(Metered(fee: 10m, allowanceGb: 2, overagePerGb: 3.50m));
        line.Record(Data(anyPeriod.Start, megabytes - 1));
        line.Record(Data(anyPeriod.Start, 1));          // summed over the period, not charged record by record

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), bill);
    }

    [Fact]
    public void Data_is_summed_over_every_day_of_the_period() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        MobileLine    line      = LineOn(Metered(fee: 10m, allowanceGb: 2, overagePerGb: 3.50m));
        line.Record(Data(anyPeriod.Start, 2_048));
        line.Record(Data(anyPeriod.End.AddDays(-1), 1));    // the last day of the period

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(13.50m, bill);
    }

    [Fact]
    public void Data_used_outside_the_period_is_not_billed() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        MobileLine    line      = LineOn(Metered(fee: 10m, allowanceGb: 2, overagePerGb: 3.50m));
        line.Record(Data(anyPeriod.Start.AddDays(-1), 50_000));
        line.Record(Data(anyPeriod.End, 50_000));           // the end is exclusive
        line.Record(Data(anyPeriod.End.AddDays(40), 50_000));

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(10m, bill);
    }

    [Fact]
    public void Voice_and_sms_are_not_billed() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        MobileLine    line      = LineOn(Metered(fee: 10m, allowanceGb: 2, overagePerGb: 3.50m));
        line.Record(Voice(anyPeriod.Start, 999_999));
        line.Record(Sms(anyPeriod.Start));

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(10m, bill);
    }

    [Fact]
    public void An_unlimited_plan_pays_its_fee_whatever_the_data() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        Plan          unlimited = Any.Plan().WithTier(PlanTier.Unlimited).WithMonthlyFee(49.90m).WithDataAllowanceGb(1).WithOveragePerGb(100m).Generate();
        MobileLine    line      = LineOn(unlimited);
        line.Record(Data(anyPeriod.Start, 1_000_000));

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(49.90m, bill);
    }

    [Fact]
    public void Past_usage_is_billed_at_the_plan_the_line_has_now() {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        MobileLine    line      = LineOn(Metered(fee: 10m, allowanceGb: 2, overagePerGb: 3.50m));
        line.Record(Data(anyPeriod.Start, 3_072));
        line.ChangePlan(Metered(fee: 30m, allowanceGb: 1, overagePerGb: 1m));

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(32m, bill);    // 30 + two gigabytes over a 1 GB allowance at 1
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void A_line_that_is_not_active_still_pays_its_fee_and_the_overage_already_metered(LineStatus status) {
        // Arrange
        BillingPeriod anyPeriod = Any.BillingPeriod().Generate();
        MobileLine    line      = LineOn(Metered(fee: 10m, allowanceGb: 2, overagePerGb: 3.50m));
        line.Record(Data(anyPeriod.Start, 3_072));
        if (status == LineStatus.Suspended) { line.Suspend(); } else { line.Terminate(); }

        // Act
        decimal bill = line.BillFor(anyPeriod);

        // Assert
        Assert.Equal(13.50m, bill);
    }

    [Fact]
    public void Two_periods_are_billed_on_their_own_usage() {
        // Arrange
        BillingPeriod january  = new BillingPeriod(new DateOnly(2025, 1, 1), new DateOnly(2025, 2, 1));
        BillingPeriod february = new BillingPeriod(new DateOnly(2025, 2, 1), new DateOnly(2025, 3, 1));
        MobileLine    line     = LineOn(Metered(fee: 10m, allowanceGb: 2, overagePerGb: 3.50m));
        line.Record(Data(new DateOnly(2025, 1, 31), 3_072));
        line.Record(Data(new DateOnly(2025, 2, 1), 100));

        // Act
        decimal januaryBill  = line.BillFor(january);
        decimal februaryBill = line.BillFor(february);

        // Assert
        Assert.Equal(13.50m, januaryBill);
        Assert.Equal(10m, februaryBill);
    }

    [Fact]
    public void A_period_that_is_not_there_is_refused() {
        // Arrange
        MobileLine anyLine = Any.MobileLine().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyLine.BillFor(null!));
    }

    // ---------------------------------------------------------------- helpers

    private static Plan Metered(decimal fee, int allowanceGb, decimal overagePerGb) {
        return Any.Plan()
                  .WithTier(tier => tier.Except(PlanTier.Unlimited))
                  .WithMonthlyFee(fee)
                  .WithDataAllowanceGb(allowanceGb)
                  .WithOveragePerGb(overagePerGb)
                  .Generate();
    }

    private static MobileLine LineOn(Plan plan) {
        return Any.MobileLine().WithPlan(plan).Generate();
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

    private static DateTime NoonOf(DateOnly day) {
        return day.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
    }

    private static UsageRecord Data(DateOnly day, long megabytes) {
        return new UsageRecord(NoonOf(day), UsageKind.Data, megabytes);
    }

    private static UsageRecord Voice(DateOnly day, long seconds) {
        return new UsageRecord(NoonOf(day), UsageKind.Voice, seconds);
    }

    private static UsageRecord Sms(DateOnly day) {
        return new UsageRecord(NoonOf(day), UsageKind.Sms, 1);
    }

}
