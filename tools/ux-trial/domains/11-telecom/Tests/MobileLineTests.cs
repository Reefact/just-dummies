using JustDummies;
using JustDummies.Xunit;
using Telecom.Domain;
using Xunit;

namespace Telecom.Tests;

[Reproducible]
public sealed class MobileLineTests {

    private static readonly BillingPeriod October = new BillingPeriod(new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 1));

    private static AnyUsageRecord AnyUsageIn(BillingPeriod period, UsageKind kind) {
        DateTime start = period.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime end   = period.End.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return Arbitrary.UsageRecord().WithKind(kind).WithOccurredAt(at => at.AfterOrEqualTo(start).Before(end));
    }

    [Fact]
    public void A_bill_is_the_fee_plus_the_data_overage_of_the_period() {
        // Arrange: fee, allowance, price and in-period data are what the bill is about.
        // Voice usage, and data metered outside the period, must not count: their amounts are arbitrary.
        MobileLine anyLine = Arbitrary.MobileLine()
                                      .WithPlan(plan => plan.WithTier(PlanTier.Postpaid)
                                                            .WithMonthlyFee(20m)
                                                            .WithDataAllowanceGb(5)
                                                            .WithOveragePerGb(1.50m))
                                      .Generate();

        anyLine.Record(AnyUsageIn(October, UsageKind.Data).WithQuantity(6 * 1024).Generate());
        foreach (UsageRecord anyCall in Any.ListOf(AnyUsageIn(October, UsageKind.Voice)).WithCountBetween(0, 5).Generate()) {
            anyLine.Record(anyCall);
        }
        anyLine.Record(Arbitrary.UsageRecord()
                                .WithKind(UsageKind.Data)
                                .WithOccurredAt(at => at.Before(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)))
                                .Generate());

        // Act
        decimal bill = anyLine.BillFor(October);

        // Assert
        Assert.Equal(21.50m, bill);
    }

    [Fact]
    public void A_suspended_line_resumes_as_active() {
        // Arrange
        MobileLine anyLine = Arbitrary.MobileLine().Generate();
        anyLine.Suspend();

        // Act
        anyLine.Resume();

        // Assert
        Assert.Equal(LineStatus.Active, anyLine.Status);
    }

    [Fact]
    public void An_active_line_cannot_be_resumed() {
        // Arrange
        MobileLine anyLine = Arbitrary.MobileLine().Generate();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(anyLine.Resume);
    }

    [Fact]
    public void A_suspended_line_meters_no_usage() {
        // Arrange
        MobileLine  anyLine  = Arbitrary.MobileLine().Generate();
        UsageRecord anyUsage = Arbitrary.UsageRecord().Generate();
        anyLine.Suspend();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => anyLine.Record(anyUsage));
    }

    [Fact]
    public void A_terminated_line_cannot_be_terminated_again() {
        // Arrange
        MobileLine anyLine = Arbitrary.MobileLine().Generate();
        anyLine.Terminate();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(anyLine.Terminate);
    }

    [Fact]
    public void Swapping_to_another_handset_pairs_the_line_with_it() {
        // Arrange
        List<Imei> anyTwoHandsets = Any.ListOf(Arbitrary.Imei()).WithCount(2).Distinct().Generate();
        MobileLine anyLine        = Arbitrary.MobileLine().WithHandset(anyTwoHandsets[0]).Generate();

        // Act
        anyLine.SwapHandset(anyTwoHandsets[1]);

        // Assert
        Assert.Same(anyTwoHandsets[1], anyLine.Handset);
    }

    [Fact]
    public void Swapping_to_the_paired_handset_is_refused() {
        // Arrange
        MobileLine anyLine = Arbitrary.MobileLine().Generate();
        Imei       sameHandset = new Imei(anyLine.Handset.Value);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => anyLine.SwapHandset(sameHandset));
    }

    [Fact]
    public void A_billing_period_excludes_its_end_day() {
        // Arrange
        BillingPeriod anyPeriod = Arbitrary.BillingPeriod().Generate();

        // Act
        bool containsEnd = anyPeriod.Contains(anyPeriod.End.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        // Assert
        Assert.False(containsEnd);
    }

    [Fact]
    public void An_SMS_record_of_two_messages_is_refused() {
        // Arrange
        DateTime anyInstant = Any.DateTime().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new UsageRecord(anyInstant, UsageKind.Sms, 2));
    }

}
