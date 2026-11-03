using Clinical.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class ClaimFileDecisionTests {

    private static readonly DateOnly Monday  = new(2026, 3, 2);
    private static readonly DateOnly Tuesday = new(2026, 3, 3);

    private static ClaimFile FileWith(decimal deductible, decimal annualCeiling, params (ReimbursementCategory Category, decimal Percent)[] schedule) {
        SupplementaryPolicy policy = Arrange.Policy(Any.Patient().Generate(),
                                                    Arrange.Schedule(schedule),
                                                    deductible,
                                                    annualCeiling,
                                                    new DateOnly(2026, 1, 1),
                                                    days: 364);

        return new ClaimFile(policy);
    }

    private static ReimbursementClaim Assessed(ClaimFile file, ReimbursementCategory category, decimal price) {
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(file.Policy.Insured, Monday, category, price);
        file.Submit(claim);
        file.Assess(claim.Reference);

        return claim;
    }

    private static ReimbursementClaim Approved(ClaimFile file, decimal price) {
        ReimbursementClaim claim = Assessed(file, ReimbursementCategory.Major, price);
        file.Approve(claim.Reference);

        return claim;
    }

    // ---- Approve ----------------------------------------------------------------------------------------------

    [Fact]
    public void Approving_an_assessed_claim_makes_it_hold_on_the_ceiling() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Assessed(file, ReimbursementCategory.Major, price: 40m);

        // Act
        file.Approve(claim.Reference);

        // Assert
        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.True(claim.ConsumesCeiling);
        Assert.Equal(20m, file.ConsumedCeiling(2026));
        Assert.Equal(80m, file.RemainingCeiling(2026));
    }

    [Fact]
    public void A_claim_whose_category_is_not_covered_leaves_nothing_to_reimburse() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Assessed(file, ReimbursementCategory.Low, price: 40m);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(claim.Reference));

        // Assert
        Assert.Equal(ClaimRefusalReason.NothingToReimburse, refusal.Reason);
        Assert.Equal(ClaimStatus.Assessed, claim.Status);
    }

    [Fact]
    public void A_deductible_that_swallows_the_covered_amount_leaves_nothing_to_reimburse() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 50m, annualCeiling: 100m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Assessed(file, ReimbursementCategory.Major, price: 40m);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(claim.Reference));

        // Assert
        Assert.Equal(ClaimRefusalReason.NothingToReimburse, refusal.Reason);
    }

    [Fact]
    public void A_claim_cut_to_zero_by_the_ceiling_is_refused_for_the_exhausted_ceiling() {
        // Arrange
        ClaimFile          file   = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        Approved(file, price: 100m);
        ReimbursementClaim second = Assessed(file, ReimbursementCategory.Major, price: 40m);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(second.Reference));

        // Assert
        Assert.Equal(ClaimRefusalReason.AnnualCeilingExhausted, refusal.Reason);
        Assert.Equal(ClaimStatus.Assessed, second.Status);
    }

    [Fact]
    public void Claims_approved_since_the_assessment_can_leave_too_little_ceiling() {
        // Arrange: both claims are assessed against the same 100.00 ceiling, each payable 80.00.
        ClaimFile          file   = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim first  = Assessed(file, ReimbursementCategory.Major, price: 80m);
        ReimbursementClaim second = Assessed(file, ReimbursementCategory.Major, price: 80m);
        file.Approve(first.Reference);

        // Act
        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Approve(second.Reference));

        // Assert
        Assert.Equal(ClaimRefusalReason.AnnualCeilingExhausted, refusal.Reason);
        Assert.Equal(20m, file.RemainingCeiling(2026));
    }

    [Fact]
    public void A_claim_that_exactly_uses_the_remaining_ceiling_is_approved() {
        // Arrange
        ClaimFile          file   = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim first  = Assessed(file, ReimbursementCategory.Major, price: 60m);
        ReimbursementClaim second = Assessed(file, ReimbursementCategory.Major, price: 40m);
        file.Approve(first.Reference);

        // Act
        file.Approve(second.Reference);

        // Assert
        Assert.Equal(0m, file.RemainingCeiling(2026));
    }

    [Fact]
    public void A_claim_not_yet_assessed_cannot_be_approved() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(file.Policy.Insured, Monday, ReimbursementCategory.Major, 10m);
        file.Submit(claim);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => file.Approve(claim.Reference));

        // Assert
        Assert.IsNotType<ClaimRefusedException>(refusal);
    }

    [Fact]
    public void An_approved_claim_cannot_be_approved_twice() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim claim = Approved(file, price: 10m);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => file.Approve(claim.Reference));
        Assert.Equal(10m, file.ConsumedCeiling(2026));
    }

    [Fact]
    public void Approving_an_unknown_claim_is_refused() {
        // Arrange
        ClaimFile      file         = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ClaimReference anyReference = Any.ClaimReference().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => file.Approve(anyReference));
    }

    // ---- Reject -----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(ClaimRefusalReason.MissingDocuments)]
    [InlineData(ClaimRefusalReason.SuspectedFraud)]
    [InlineData(ClaimRefusalReason.NothingToReimburse)]
    public void Rejecting_an_assessed_claim_records_the_reason(ClaimRefusalReason reason) {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim claim = Assessed(file, ReimbursementCategory.Major, price: 10m);

        // Act
        file.Reject(claim.Reference, reason);

        // Assert
        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal(reason, claim.RejectionReason);
        Assert.False(claim.ConsumesCeiling);
    }

    [Fact]
    public void An_undefined_rejection_reason_is_refused_and_the_claim_stays_assessed() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim claim = Assessed(file, ReimbursementCategory.Major, price: 10m);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => file.Reject(claim.Reference, (ClaimRefusalReason)99));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
        Assert.Equal(ClaimStatus.Assessed, claim.Status);
    }

    [Fact]
    public void A_submitted_claim_cannot_be_rejected_before_it_is_assessed() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim claim = Arrange.ClaimOfOneLine(file.Policy.Insured, Monday, ReimbursementCategory.Major, 10m);
        file.Submit(claim);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => file.Reject(claim.Reference, ClaimRefusalReason.MissingDocuments));
    }

    [Fact]
    public void An_approved_claim_cannot_be_rejected() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim claim = Approved(file, price: 10m);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => file.Reject(claim.Reference, ClaimRefusalReason.MissingDocuments));
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public void A_rejected_claim_cannot_be_approved_afterwards() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim claim = Assessed(file, ReimbursementCategory.Major, price: 10m);
        file.Reject(claim.Reference, ClaimRefusalReason.SuspectedFraud);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => file.Approve(claim.Reference));
    }

    [Fact]
    public void Rejecting_an_unknown_claim_is_refused() {
        // Arrange
        ClaimFile      file         = FileWith(deductible: 0m, annualCeiling: 100m, (ReimbursementCategory.Major, 100m));
        ClaimReference anyReference = Any.ClaimReference().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => file.Reject(anyReference, ClaimRefusalReason.MissingDocuments));
    }

    // ---- Pay --------------------------------------------------------------------------------------------------

    [Fact]
    public void Paying_an_approved_claim_issues_a_transfer_from_the_insurer_to_the_insured() {
        // Arrange
        ClaimFile          file    = FileWith(deductible: 5m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim   = Assessed(file, ReimbursementCategory.Major, price: 40m);
        file.Approve(claim.Reference);
        Party              anyInsurer = Any.Party().Generate();

        // Act
        CreditTransfer transfer = file.Pay(claim.Reference, anyInsurer, Tuesday);

        // Assert
        Assert.Equal(ClaimStatus.Paid, claim.Status);
        Assert.Same(transfer, claim.Payment);
        Assert.Equal(claim.Reference.Value, transfer.EndToEndId);
        Assert.Same(anyInsurer, transfer.Debtor);
        Assert.Same(file.Policy.Payee, transfer.Creditor);
        Assert.Equal(new Money(15m, Currency.EUR), transfer.Amount);
        Assert.Equal(Tuesday, transfer.RequestedExecutionDate);
        Assert.Equal(TransferPriority.Normal, transfer.Priority);
    }

    [Fact]
    public void The_remittance_names_the_claim_and_the_policy() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Approved(file, price: 40m);

        // Act
        CreditTransfer transfer = file.Pay(claim.Reference, Any.Party().Generate(), Tuesday);

        // Assert
        Assert.Equal($"Reimbursement {claim.Reference} policy {file.Policy.Number}", transfer.RemittanceInformation!.Text);
    }

    [Fact]
    public void A_claim_may_be_paid_on_the_very_day_it_was_dispensed() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Approved(file, price: 40m);

        // Act
        CreditTransfer transfer = file.Pay(claim.Reference, Any.Party().Generate(), Monday);

        // Assert
        Assert.Equal(Monday, transfer.RequestedExecutionDate);
    }

    [Fact]
    public void A_claim_cannot_be_paid_before_the_day_it_was_dispensed() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Approved(file, price: 40m);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => file.Pay(claim.Reference, Any.Party().Generate(), new DateOnly(2026, 2, 27)));

        // Assert
        Assert.Equal("executionDate", refusal.ParamName);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Theory]
    [InlineData(2026, 3, 7)]   // a Saturday
    [InlineData(2026, 3, 8)]   // a Sunday
    [InlineData(2026, 5, 1)]   // 1 May, a Friday
    [InlineData(2026, 12, 25)] // Christmas
    public void A_non_business_day_is_refused_by_the_transfer_and_the_claim_stays_approved(int year, int month, int day) {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Approved(file, price: 40m);

        // Act
        Assert.Throws<ArgumentException>(() => file.Pay(claim.Reference, Any.Party().Generate(), new DateOnly(year, month, day)));

        // Assert
        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Null(claim.Payment);
        Assert.Equal(20m, file.ConsumedCeiling(2026));
    }

    [Fact]
    public void After_a_refused_payment_the_claim_can_still_be_paid_on_a_business_day() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Approved(file, price: 40m);
        Party              anyInsurer = Any.Party().Generate();
        Assert.ThrowsAny<ArgumentException>(() => file.Pay(claim.Reference, anyInsurer, new DateOnly(2026, 3, 7)));

        // Act
        file.Pay(claim.Reference, anyInsurer, Tuesday);

        // Assert
        Assert.Equal(ClaimStatus.Paid, claim.Status);
    }

    [Fact]
    public void An_insurer_paying_itself_is_refused_and_the_claim_stays_approved() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Approved(file, price: 40m);

        // Act
        Assert.Throws<ArgumentException>(() => file.Pay(claim.Reference, file.Policy.Payee, Tuesday));

        // Assert
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public void A_claim_not_approved_cannot_be_paid() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Assessed(file, ReimbursementCategory.Major, price: 40m);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => file.Pay(claim.Reference, Any.Party().Generate(), Tuesday));
    }

    [Fact]
    public void A_paid_claim_cannot_be_paid_twice() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Approved(file, price: 40m);
        Party              anyInsurer = Any.Party().Generate();
        file.Pay(claim.Reference, anyInsurer, Tuesday);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => file.Pay(claim.Reference, anyInsurer, Tuesday));
    }

    [Fact]
    public void Paying_without_an_insurer_or_a_reference_is_refused() {
        // Arrange
        ClaimFile          file  = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ReimbursementClaim claim = Approved(file, price: 40m);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => file.Pay(claim.Reference, null!, Tuesday));
        Assert.Throws<ArgumentNullException>(() => file.Pay(null!, Any.Party().Generate(), Tuesday));
    }

    [Fact]
    public void Paying_an_unknown_claim_is_refused() {
        // Arrange
        ClaimFile      file         = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 50m));
        ClaimReference anyReference = Any.ClaimReference().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => file.Pay(anyReference, Any.Party().Generate(), Tuesday));
    }

    // ---- Ceiling bookkeeping ----------------------------------------------------------------------------------

    [Fact]
    public void With_no_claim_the_whole_ceiling_remains_in_any_year() {
        // Arrange
        ClaimFile file = FileWith(deductible: 0m, annualCeiling: 250m, (ReimbursementCategory.Major, 50m));

        // Act / Assert
        Assert.Equal(0m, file.ConsumedCeiling(2026));
        Assert.Equal(250m, file.RemainingCeiling(2026));
        Assert.Equal(250m, file.RemainingCeiling(1999));
    }

    [Fact]
    public void Only_approved_and_paid_claims_of_the_year_are_consumed() {
        // Arrange
        ClaimFile          file       = FileWith(deductible: 0m, annualCeiling: 1_000m, (ReimbursementCategory.Major, 100m));
        ReimbursementClaim approved   = Approved(file, price: 10m);
        ReimbursementClaim paid       = Approved(file, price: 20m);
        file.Pay(paid.Reference, Any.Party().Generate(), Tuesday);
        Assessed(file, ReimbursementCategory.Major, price: 40m);
        ReimbursementClaim rejected   = Assessed(file, ReimbursementCategory.Major, price: 80m);
        file.Reject(rejected.Reference, ClaimRefusalReason.MissingDocuments);
        ReimbursementClaim submitted  = Arrange.ClaimOfOneLine(file.Policy.Insured, Monday, ReimbursementCategory.Major, 160m);
        file.Submit(submitted);

        // Act
        decimal consumed = file.ConsumedCeiling(2026);

        // Assert
        Assert.Equal(30m, consumed);
        Assert.Equal(970m, file.RemainingCeiling(2026));
        Assert.Equal(ClaimStatus.Approved, approved.Status);
    }

}
