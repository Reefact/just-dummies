using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Expert.Support;
using Insurance.Domain;
using JustDummies;
using Payments.Domain;
using Xunit;

using static HealthcareClaims.Tests.Expert.Support.Scenario;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ClaimFilePaymentTests {

    private static readonly DateOnly Thursday = Tuesday.AddDays(2);

    public static TheoryData<int, int, int> DaysThatAreNotBusinessDays => new() {
        { 2026, 3, 14 },    // a Saturday
        { 2026, 3, 15 },    // a Sunday
        { 2026, 5, 1 },     // Labour Day, a Friday
        { 2026, 12, 25 }    // Christmas, a Friday
    };

    private static Dispensing OneMajorBox(decimal unitPrice) => new(ReimbursementCategory.Major, unitPrice);

    [Fact]
    public void Paying_issues_a_transfer_from_the_insurer_to_the_payee_of_the_policy_for_the_payable_amount() {
        PolicyNumber       number = new("POL-2025-FR123456");
        ClaimReference     reference = new("CLM-2026-000042");
        SupplementaryPolicy policy = Any.SupplementaryPolicy()
                                        .WithNumber(number)
                                        .WithPeriod(Year2026)
                                        .WithSchedule(EverythingListed)
                                        .WithDeductible(10.00m)
                                        .WithAnnualCeiling(1_000m)
                                        .Generate();
        ClaimFile file = new(policy);
        ReimbursementClaim claim = Any.ReimbursementClaim()
                                      .WithReference(reference)
                                      .ForInsured(policy.Insured)
                                      .DispensedOn(Tuesday)
                                      .Dispensing(OneMajorBox(70.00m))
                                      .Generate();
        file.Submit(claim);
        file.Assess(reference);
        file.Approve(reference);
        Party insurer = InsurerOf(file);

        CreditTransfer transfer = file.Pay(reference, insurer, Thursday);

        Assert.Equal("CLM-2026-000042", transfer.EndToEndId);
        Assert.Same(insurer, transfer.Debtor);
        Assert.Same(policy.Payee, transfer.Creditor);
        Assert.Equal(new Money(60.00m, Currency.EUR), transfer.Amount);
        Assert.Equal(Thursday, transfer.RequestedExecutionDate);
        Assert.Equal(TransferPriority.Normal, transfer.Priority);
        Assert.Equal("Reimbursement CLM-2026-000042 policy POL-2025-FR123456", transfer.RemittanceInformation?.Text);
    }

    [Fact]
    public void Paying_moves_the_claim_on_and_keeps_the_transfer() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, OneMajorBox(70.00m));

        CreditTransfer transfer = file.Pay(claim.Reference, InsurerOf(file), Thursday);

        Assert.Equal(ClaimStatus.Paid, claim.Status);
        Assert.Same(transfer, claim.Payment);
        Assert.True(claim.ConsumesCeiling);
    }

    [Fact]
    public void A_claim_may_be_paid_on_the_day_it_was_dispensed() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, OneMajorBox(70.00m));

        CreditTransfer transfer = file.Pay(claim.Reference, InsurerOf(file), Tuesday);

        Assert.Equal(Tuesday, transfer.RequestedExecutionDate);
    }

    [Fact]
    public void A_claim_cannot_be_paid_before_the_day_it_was_dispensed() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, OneMajorBox(70.00m));
        DateOnly           theMondayBefore = Tuesday.AddDays(-1);

        Assert.Throws<ArgumentOutOfRangeException>(() => file.Pay(claim.Reference, InsurerOf(file), theMondayBefore));
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Theory]
    [MemberData(nameof(DaysThatAreNotBusinessDays))]
    public void A_claim_cannot_be_paid_on_a_day_banks_are_closed_and_stays_approved(int year, int month, int day) {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, OneMajorBox(70.00m));

        Assert.Throws<ArgumentException>(() => file.Pay(claim.Reference, InsurerOf(file), new DateOnly(year, month, day)));

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Null(claim.Payment);
    }

    [Fact]
    public void A_claim_cannot_be_paid_by_an_insurer_banking_where_the_insured_does_and_stays_approved() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, OneMajorBox(70.00m));
        Party              insurerOfTheSameAccount = Any.Party().WithAccount(file.Policy.Payee.Account).Generate();

        Assert.Throws<ArgumentException>(() => file.Pay(claim.Reference, insurerOfTheSameAccount, Thursday));

        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public void A_refused_payment_can_be_made_again_with_a_good_date() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, OneMajorBox(70.00m));
        Party              insurer = InsurerOf(file);
        Assert.Throws<ArgumentException>(() => file.Pay(claim.Reference, insurer, new DateOnly(2026, 3, 14)));

        file.Pay(claim.Reference, insurer, Thursday);

        Assert.Equal(ClaimStatus.Paid, claim.Status);
    }

    [Fact]
    public void A_claim_must_be_approved_to_be_paid() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Assessed(file, Tuesday, OneMajorBox(70.00m));

        Assert.Throws<InvalidOperationException>(() => file.Pay(claim.Reference, InsurerOf(file), Thursday));
        Assert.Equal(ClaimStatus.Assessed, claim.Status);
    }

    [Fact]
    public void A_claim_cannot_be_paid_twice() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, OneMajorBox(70.00m));
        Party              insurer = InsurerOf(file);
        CreditTransfer     first   = file.Pay(claim.Reference, insurer, Thursday);

        Assert.Throws<InvalidOperationException>(() => file.Pay(claim.Reference, insurer, Thursday));
        Assert.Same(first, claim.Payment);
    }

    [Fact]
    public void A_rejected_claim_cannot_be_paid() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Assessed(file, Tuesday, OneMajorBox(70.00m));
        file.Reject(claim.Reference, ClaimRefusalReason.MissingDocuments);

        Assert.Throws<InvalidOperationException>(() => file.Pay(claim.Reference, InsurerOf(file), Thursday));
    }

    [Fact]
    public void An_unknown_claim_cannot_be_paid() {
        ClaimFile      file                = FileOf(EverythingListed);
        ClaimReference anyUnknownReference = Any.ClaimReference().Generate();

        Assert.Throws<KeyNotFoundException>(() => file.Pay(anyUnknownReference, InsurerOf(file), Thursday));
    }

    [Fact]
    public void A_claim_cannot_be_paid_by_nobody() {
        ClaimFile          file  = FileOf(EverythingListed);
        ReimbursementClaim claim = Approved(file, Tuesday, OneMajorBox(70.00m));

        Assert.Throws<ArgumentNullException>(() => file.Pay(claim.Reference, null!, Thursday));
    }

}
