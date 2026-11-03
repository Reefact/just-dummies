using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;
using HealthcareClaims.Tests.Regular.Support;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ClaimFilePaymentTests {

    private static readonly ReimbursementSchedule FullMajorCover = Scenarios.ScheduleOf((ReimbursementCategory.Major, 100m));

    private static (ClaimFile File, SupplementaryPolicy Policy, ReimbursementClaim Claim) ApprovedClaimOf(decimal payable) {
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 10_000m);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOf(policy, (ReimbursementCategory.Major, payable, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);
        file.Approve(claim.Reference);

        return (file, policy, claim);
    }

    [Fact]
    public void Paying_an_approved_claim_issues_a_transfer_from_the_insurer_to_the_insureds_payee_for_the_payable_amount() {
        // Arrange
        (ClaimFile file, SupplementaryPolicy policy, ReimbursementClaim claim) = ApprovedClaimOf(42.30m);
        Party    anyInsurer    = Any.Party().Generate();
        DateOnly executionDate = BusinessCalendar.NextBusinessDayOnOrAfter(claim.DispensedOn);

        // Act
        CreditTransfer transfer = file.Pay(claim.Reference, anyInsurer, executionDate);

        // Assert
        Assert.Same(anyInsurer, transfer.Debtor);
        Assert.Same(policy.Payee, transfer.Creditor);
        Assert.Equal(new Money(42.30m, Currency.EUR), transfer.Amount);
        Assert.Equal(executionDate, transfer.RequestedExecutionDate);
        Assert.Equal(TransferPriority.Normal, transfer.Priority);
    }

    [Fact]
    public void The_transfer_is_identified_by_the_claim_reference_and_explains_what_it_reimburses() {
        // Arrange
        (ClaimFile file, SupplementaryPolicy policy, ReimbursementClaim claim) = ApprovedClaimOf(10m);
        Party    anyInsurer    = Any.Party().Generate();
        DateOnly executionDate = BusinessCalendar.NextBusinessDayOnOrAfter(claim.DispensedOn);

        // Act
        CreditTransfer transfer = file.Pay(claim.Reference, anyInsurer, executionDate);

        // Assert
        Assert.Equal(claim.Reference.Value, transfer.EndToEndId);
        Assert.Equal($"Reimbursement {claim.Reference} policy {policy.Number}", transfer.RemittanceInformation!.Text);
    }

    [Fact]
    public void A_paid_claim_keeps_its_transfer_and_still_holds_on_the_ceiling() {
        // Arrange
        (ClaimFile file, SupplementaryPolicy policy, ReimbursementClaim claim) = ApprovedClaimOf(10m);
        Party    anyInsurer    = Any.Party().Generate();
        DateOnly executionDate = BusinessCalendar.NextBusinessDayOnOrAfter(claim.DispensedOn);

        // Act
        CreditTransfer transfer = file.Pay(claim.Reference, anyInsurer, executionDate);

        // Assert
        Assert.Equal(ClaimStatus.Paid, claim.Status);
        Assert.Same(transfer, claim.Payment);
        Assert.True(claim.ConsumesCeiling);
        Assert.Equal(10m, file.ConsumedCeiling(claim.BenefitYear));
    }

    [Fact]
    public void A_claim_can_be_paid_on_the_very_day_it_was_dispensed() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 10_000m);
        DateOnly            businessDay = BusinessCalendar.NextBusinessDayOnOrAfter(policy.Period.Start);
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOn(policy, businessDay, (ReimbursementCategory.Major, 10m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);
        file.Approve(claim.Reference);

        // Act
        CreditTransfer transfer = file.Pay(claim.Reference, Any.Party().Generate(), businessDay);

        // Assert
        Assert.Equal(businessDay, transfer.RequestedExecutionDate);
    }

    [Fact]
    public void A_claim_cannot_be_paid_before_the_day_it_was_dispensed() {
        // Arrange
        SupplementaryPolicy policy = Scenarios.PolicyWithin2026(FullMajorCover, 0m, 10_000m);
        DateOnly            businessDay = BusinessCalendar.NextBusinessDayOnOrAfter(policy.Period.Start.AddDays(10));
        ClaimFile           file   = new(policy);
        ReimbursementClaim  claim  = Scenarios.ClaimOn(policy, businessDay, (ReimbursementCategory.Major, 10m, 1, null));
        file.Submit(claim);
        file.Assess(claim.Reference);
        file.Approve(claim.Reference);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => file.Pay(claim.Reference, Any.Party().Generate(), businessDay.AddDays(-1)));

        // Assert
        Assert.Equal("executionDate", refusal.ParamName);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    public static TheoryData<int, int, int> NonBusinessDays => new() {
        { 2026, 12, 5 },    // a Saturday
        { 2026, 12, 6 },    // a Sunday
        { 2026, 12, 25 },   // Christmas Day, a Friday
        { 2026, 12, 26 }    // Boxing Day
    };

    [Theory]
    [MemberData(nameof(NonBusinessDays))]
    public void A_transfer_cannot_be_executed_on_a_day_the_banks_are_closed_and_the_claim_stays_approved(int year, int month, int day) {
        // Arrange
        (ClaimFile file, _, ReimbursementClaim claim) = ApprovedClaimOf(10m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => file.Pay(claim.Reference, Any.Party().Generate(), new DateOnly(year, month, day)));

        // Assert
        Assert.Equal("requestedExecutionDate", refusal.ParamName);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Null(claim.Payment);
    }

    [Fact]
    public void The_insurer_cannot_pay_into_its_own_account_and_the_claim_stays_approved() {
        // Arrange
        (ClaimFile file, SupplementaryPolicy policy, ReimbursementClaim claim) = ApprovedClaimOf(10m);
        Party    insurerWithThePayeesAccount = Any.Party().WithAccount(policy.Payee.Account).Generate();
        DateOnly executionDate               = BusinessCalendar.NextBusinessDayOnOrAfter(claim.DispensedOn);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => file.Pay(claim.Reference, insurerWithThePayeesAccount, executionDate));

        // Assert
        Assert.Equal("creditor", refusal.ParamName);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public void A_claim_that_is_not_approved_cannot_be_paid() {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  anyClaim  = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(anyClaim);
        file.Assess(anyClaim.Reference);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Pay(anyClaim.Reference, Any.Party().Generate(), BusinessCalendar.NextBusinessDayOnOrAfter(anyClaim.DispensedOn)));
    }

    [Fact]
    public void A_paid_claim_cannot_be_paid_twice() {
        // Arrange
        (ClaimFile file, _, ReimbursementClaim claim) = ApprovedClaimOf(10m);
        Party    anyInsurer    = Any.Party().Generate();
        DateOnly executionDate = BusinessCalendar.NextBusinessDayOnOrAfter(claim.DispensedOn);
        file.Pay(claim.Reference, anyInsurer, executionDate);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => file.Pay(claim.Reference, anyInsurer, executionDate));
    }

    [Fact]
    public void Paying_needs_an_insurer() {
        // Arrange
        (ClaimFile file, _, ReimbursementClaim claim) = ApprovedClaimOf(10m);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => file.Pay(claim.Reference, null!, BusinessCalendar.NextBusinessDayOnOrAfter(claim.DispensedOn)));
    }

}
