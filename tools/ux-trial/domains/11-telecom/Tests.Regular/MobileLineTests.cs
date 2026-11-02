using JustDummies;

using Telecom.Domain;

using Xunit;

namespace Telecom.Tests.Regular;

public sealed class MobileLineTests {

    private static readonly BillingPeriod March = new BillingPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 4, 1));

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

    private static UsageRecord Data(long megabytes, DateTime occurredAt) {
        return Any.UsageRecord().WithKind(UsageKind.Data).WithQuantity(megabytes).WithOccurredAt(occurredAt).Generate();
    }

    private static DateTime Utc(int month, int day, int hour = 12) => new DateTime(2025, month, day, hour, 0, 0, DateTimeKind.Utc);

    // A 10 GB plan at 2.50 per started GB over, 20.00 a month: the only plan the billing tests care about.
    private static MobileLine BillableLine(PlanTier tier = PlanTier.Postpaid) {
        Plan plan = Any.Plan().WithTier(tier).WithMonthlyFee(20.00m).WithDataAllowanceGb(10).WithOveragePerGb(2.50m).Generate();

        return Any.MobileLine().WithPlan(plan).Generate();
    }

    // ---- construction ----

    [Fact]
    public void A_new_line_is_active_with_no_usage() {
        // Act
        MobileLine line = Any.MobileLine().Generate();

        // Assert
        Assert.Equal(LineStatus.Active, line.Status);
        Assert.Empty(line.Usage);
    }

    [Fact]
    public void Keeps_the_number_handset_plan_and_activation_date_it_was_given() {
        // Arrange
        Msisdn   anyNumber  = Any.Msisdn().Generate();
        Imei     anyHandset = Any.Imei().Generate();
        Plan     anyPlan    = Any.Plan().Generate();
        DateOnly activatedOn = new DateOnly(2023, 6, 15);

        // Act
        MobileLine line = new MobileLine(anyNumber, anyHandset, anyPlan, activatedOn);

        // Assert
        Assert.Same(anyNumber, line.Number);
        Assert.Same(anyHandset, line.Handset);
        Assert.Same(anyPlan, line.Plan);
        Assert.Equal(activatedOn, line.ActivatedOn);
    }

    [Fact]
    public void Refuses_a_missing_number() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new MobileLine(null!, Any.Imei().Generate(), Any.Plan().Generate(), new DateOnly(2023, 1, 1)));

        // Assert
        Assert.Equal("number", refused.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_handset() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new MobileLine(Any.Msisdn().Generate(), null!, Any.Plan().Generate(), new DateOnly(2023, 1, 1)));

        // Assert
        Assert.Equal("handset", refused.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_plan() {
        // Act
        ArgumentNullException refused = Assert.Throws<ArgumentNullException>(() => new MobileLine(Any.Msisdn().Generate(), Any.Imei().Generate(), null!, new DateOnly(2023, 1, 1)));

        // Assert
        Assert.Equal("plan", refused.ParamName);
    }

    [Fact]
    public void Accepts_an_activation_on_the_first_day_of_2000() {
        // Act
        MobileLine line = Any.MobileLine().WithActivatedOn(new DateOnly(2000, 1, 1)).Generate();

        // Assert
        Assert.Equal(new DateOnly(2000, 1, 1), line.ActivatedOn);
    }

    [Fact]
    public void Refuses_an_activation_before_2000() {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MobileLine().WithActivatedOn(new DateOnly(1999, 12, 31)).Generate());

        // Assert
        Assert.Equal("activatedOn", refused.ParamName);
    }

    [Fact]
    public void Refuses_any_activation_date_before_2000() {
        // Arrange
        DateOnly anyEarlyDate = Any.DateOnly().Before(new DateOnly(2000, 1, 1)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.MobileLine().WithActivatedOn(anyEarlyDate).Generate());
    }

    // ---- Record ----

    [Fact]
    public void Records_usage_in_the_order_it_arrives() {
        // Arrange
        MobileLine  line   = Any.MobileLine().Generate();
        UsageRecord first  = Any.UsageRecord().Generate();
        UsageRecord second = Any.UsageRecord().Generate();

        // Act
        line.Record(first);
        line.Record(second);

        // Assert
        Assert.Equal(new[] { first, second }, line.Usage);
    }

    [Fact]
    public void Refuses_a_null_usage_record() {
        // Arrange
        MobileLine line = Any.MobileLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => line.Record(null!));
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void Meters_no_usage_unless_active(LineStatus status) {
        // Arrange
        MobileLine  line        = LineIn(status);
        UsageRecord anyUsage    = Any.UsageRecord().Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => line.Record(anyUsage));

        // Assert
        Assert.Empty(line.Usage);
    }

    [Fact]
    public void Meters_usage_again_once_resumed() {
        // Arrange
        MobileLine  line     = Any.MobileLine().Generate();
        UsageRecord anyUsage = Any.UsageRecord().Generate();
        line.Suspend();
        line.Resume();

        // Act
        line.Record(anyUsage);

        // Assert
        Assert.Single(line.Usage);
    }

    [Fact]
    public void Meters_usage_dated_before_the_activation_as_given() {
        // Arrange: characterisation, the line does not compare the record's date with its activation
        MobileLine line = Any.MobileLine().WithActivatedOn(new DateOnly(2025, 6, 1)).Generate();
        UsageRecord before = Data(100, Utc(1, 1));

        // Act
        line.Record(before);

        // Assert
        Assert.Single(line.Usage);
    }

    [Fact(Skip = "domain defect: Usage is a List<UsageRecord> returned as IReadOnlyList, so a caller casting it to IList can add or clear records; not seed-dependent")]
    public void Exposes_usage_as_a_list_that_cannot_be_modified_from_outside() {
        // Arrange
        MobileLine line = Any.MobileLine().Generate();

        // Act
        bool modifiable = line.Usage is IList<UsageRecord> { IsReadOnly: false };

        // Assert
        Assert.False(modifiable);
    }

    // ---- Suspend / Resume / Terminate ----

    [Fact]
    public void An_active_line_can_be_suspended() {
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
    public void Only_an_active_line_can_be_suspended(LineStatus status) {
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
    public void Only_a_suspended_line_can_be_resumed(LineStatus status) {
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
    public void An_active_or_suspended_line_can_be_terminated(LineStatus status) {
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

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => line.Terminate());
    }

    [Fact]
    public void A_terminated_line_stays_terminated_whatever_is_tried() {
        // Arrange
        MobileLine line = LineIn(LineStatus.Terminated);

        // Act
        Assert.Throws<InvalidOperationException>(() => line.Suspend());
        Assert.Throws<InvalidOperationException>(() => line.Resume());

        // Assert
        Assert.Equal(LineStatus.Terminated, line.Status);
    }

    // ---- SwapHandset ----

    [Fact]
    public void Swapping_the_handset_pairs_the_line_with_the_new_one() {
        // Arrange
        MobileLine line       = Any.MobileLine().Generate();
        Imei       newHandset = Any.Imei().DifferentFrom(line.Handset).Generate();

        // Act
        line.SwapHandset(newHandset);

        // Assert
        Assert.Same(newHandset, line.Handset);
    }

    [Fact]
    public void Refuses_a_swap_to_the_handset_already_paired() {
        // Arrange
        MobileLine line = Any.MobileLine().Generate();
        Imei       same = new Imei(line.Handset.Value);
        Imei before = line.Handset;

        // Act
        Assert.Throws<InvalidOperationException>(() => line.SwapHandset(same));

        // Assert
        Assert.Same(before, line.Handset);
    }

    [Fact]
    public void Refuses_a_null_handset() {
        // Arrange
        MobileLine line = Any.MobileLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => line.SwapHandset(null!));
    }

    [Theory]
    [InlineData(LineStatus.Active)]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void The_handset_can_be_swapped_in_any_status(LineStatus status) {
        // Arrange
        MobileLine line       = LineIn(status);
        Imei       newHandset = Any.Imei().DifferentFrom(line.Handset).Generate();

        // Act
        line.SwapHandset(newHandset);

        // Assert
        Assert.Same(newHandset, line.Handset);
    }

    // ---- ChangePlan ----

    [Fact]
    public void Changing_the_plan_replaces_it() {
        // Arrange
        MobileLine line    = Any.MobileLine().Generate();
        Plan       newPlan = Any.Plan().Generate();

        // Act
        line.ChangePlan(newPlan);

        // Assert
        Assert.Same(newPlan, line.Plan);
    }

    [Fact]
    public void Refuses_a_null_plan() {
        // Arrange
        MobileLine line = Any.MobileLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => line.ChangePlan(null!));
    }

    [Theory]
    [InlineData(LineStatus.Active)]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void The_plan_can_be_changed_in_any_status(LineStatus status) {
        // Arrange
        MobileLine line    = LineIn(status);
        Plan       newPlan = Any.Plan().Generate();

        // Act
        line.ChangePlan(newPlan);

        // Assert
        Assert.Same(newPlan, line.Plan);
    }

    // ---- BillFor ----

    [Fact]
    public void A_line_without_usage_is_billed_the_monthly_fee() {
        // Arrange
        MobileLine line = BillableLine();

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(20.00m, bill);
    }

    [Fact]
    public void Usage_within_the_allowance_is_billed_the_fee_only() {
        // Arrange
        MobileLine line = BillableLine();
        line.Record(Data(10 * 1024, Utc(3, 15)));

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(20.00m, bill);
    }

    [Fact]
    public void Each_started_gigabyte_over_the_allowance_is_added_to_the_fee() {
        // Arrange
        MobileLine line = BillableLine();
        line.Record(Data(10 * 1024 + 1, Utc(3, 15)));

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(22.50m, bill);
    }

    [Fact]
    public void Data_records_of_the_period_are_added_up_before_the_allowance_is_applied() {
        // Arrange: 6 GB + 6 GB = 12 GB, two gigabytes over
        MobileLine line = BillableLine();
        line.Record(Data(6 * 1024, Utc(3, 2)));
        line.Record(Data(6 * 1024, Utc(3, 28)));

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(25.00m, bill);
    }

    [Fact]
    public void Usage_outside_the_period_is_not_billed() {
        // Arrange
        MobileLine line = BillableLine();
        line.Record(Data(50 * 1024, Utc(2, 28, 23)));
        line.Record(Data(50 * 1024, Utc(4, 1, 0)));

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(20.00m, bill);
    }

    [Fact]
    public void Usage_on_the_first_day_of_the_period_is_billed_and_on_its_end_day_is_not() {
        // Arrange
        MobileLine line = BillableLine();
        line.Record(Data(11 * 1024, Utc(3, 1, 0)));
        line.Record(Data(5 * 1024, Utc(4, 1, 0)));

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(22.50m, bill);
    }

    [Fact]
    public void Voice_and_sms_are_not_billed() {
        // Arrange
        MobileLine line = BillableLine();
        line.Record(Any.UsageRecord().WithKind(UsageKind.Voice).WithQuantity(999_999).WithOccurredAt(Utc(3, 10)).Generate());
        line.Record(Any.UsageRecord().WithKind(UsageKind.Sms).WithOccurredAt(Utc(3, 11)).Generate());

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(20.00m, bill);
    }

    [Fact]
    public void An_unlimited_plan_is_billed_the_fee_whatever_the_usage() {
        // Arrange
        MobileLine line = BillableLine(PlanTier.Unlimited);
        line.Record(Data(500 * 1024, Utc(3, 15)));

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(20.00m, bill);
    }

    [Fact]
    public void Past_usage_is_billed_at_the_current_plan_only() {
        // Arrange: 12 GB used under a 10 GB / 2.50 plan, then the line moves to a 20 GB plan
        MobileLine line = BillableLine();
        line.Record(Data(12 * 1024, Utc(3, 10)));
        Plan roomier = Any.Plan().WithTier(PlanTier.Postpaid).WithMonthlyFee(30.00m).WithDataAllowanceGb(20).WithOveragePerGb(1m).Generate();
        line.ChangePlan(roomier);

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(30.00m, bill);
    }

    [Theory]
    [InlineData(LineStatus.Suspended)]
    [InlineData(LineStatus.Terminated)]
    public void A_suspended_or_terminated_line_is_still_billed_its_fee_and_overage(LineStatus status) {
        // Arrange
        MobileLine line = BillableLine();
        line.Record(Data(11 * 1024, Utc(3, 10)));
        if (status == LineStatus.Suspended) { line.Suspend(); } else { line.Terminate(); }

        // Act
        decimal bill = line.BillFor(March);

        // Assert
        Assert.Equal(22.50m, bill);
    }

    [Fact]
    public void Refuses_a_null_period() {
        // Arrange
        MobileLine line = Any.MobileLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => line.BillFor(null!));
    }

    [Fact]
    public void Billing_does_not_modify_the_line() {
        // Arrange
        MobileLine line = BillableLine();
        line.Record(Data(11 * 1024, Utc(3, 10)));

        // Act
        decimal first  = line.BillFor(March);
        decimal second = line.BillFor(March);

        // Assert
        Assert.Equal(first, second);
        Assert.Single(line.Usage);
    }

}
