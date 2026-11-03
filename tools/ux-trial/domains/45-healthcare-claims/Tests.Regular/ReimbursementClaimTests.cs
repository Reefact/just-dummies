using Clinical.Domain;

using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ReimbursementClaimTests {

    private static Money Euros(decimal amount) => new(amount, Currency.EUR);

    [Fact]
    public void A_claim_is_born_submitted_with_nothing_decided_yet() {
        // Arrange
        Prescription        anyPrescription = Any.Prescription().Dispensed().Generate();
        ClaimReference      anyReference    = Any.ClaimReference().Generate();
        DispensedLine       anyLine         = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();

        // Act
        ReimbursementClaim claim = new(anyReference, anyPrescription, anyPrescription.IssuedOn, [anyLine]);

        // Assert
        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Null(claim.Assessment);
        Assert.Null(claim.RejectionReason);
        Assert.Null(claim.Payment);
        Assert.False(claim.ConsumesCeiling);
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.Assessed)]
    [InlineData(ClaimStatus.Rejected)]
    public void A_claim_that_is_not_approved_or_paid_holds_nothing_on_the_ceiling(ClaimStatus status) {
        // Arrange
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();
        ClaimFile           file      = new(anyPolicy);
        ReimbursementClaim  claim     = Any.ReimbursementClaim().ForPolicy(anyPolicy).Generate();
        file.Submit(claim);
        if (status >= ClaimStatus.Assessed) { file.Assess(claim.Reference); }
        if (status == ClaimStatus.Rejected) { file.Reject(claim.Reference, ClaimRefusalReason.MissingDocuments); }

        // Assert
        Assert.Equal(status, claim.Status);
        Assert.False(claim.ConsumesCeiling);
    }

    [Theory]
    [InlineData(PrescriptionStatus.Draft)]
    [InlineData(PrescriptionStatus.Signed)]
    [InlineData(PrescriptionStatus.Cancelled)]
    public void Only_a_dispensed_prescription_can_be_claimed(PrescriptionStatus status) {
        // Arrange
        AnyPrescription anyPrescriptionInStatus = status switch {
            PrescriptionStatus.Signed    => Any.Prescription().Signed(),
            PrescriptionStatus.Cancelled => Any.Prescription().Cancelled(),
            _                            => Any.Prescription()
        };
        Prescription anyPrescription = anyPrescriptionInStatus.Generate();
        DispensedLine anyLine        = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, [anyLine]));

        // Assert
        Assert.Equal("prescription", refusal.ParamName);
    }

    [Fact]
    public void A_prescription_can_be_dispensed_on_the_day_it_was_issued() {
        // Arrange
        Prescription  anyPrescription = Any.Prescription().Dispensed().Generate();
        DispensedLine anyLine         = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, [anyLine]);

        // Assert
        Assert.Equal(anyPrescription.IssuedOn, claim.DispensedOn);
    }

    [Fact]
    public void A_prescription_can_be_dispensed_on_its_last_dispensing_day() {
        // Arrange
        Prescription  anyPrescription = Any.Prescription().Dispensed().Generate();
        DispensedLine anyLine         = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.LastDispensingDay, [anyLine]);

        // Assert
        Assert.Equal(anyPrescription.LastDispensingDay, claim.DispensedOn);
    }

    [Fact]
    public void A_prescription_cannot_be_dispensed_before_it_was_issued() {
        // Arrange
        Prescription  anyPrescription = Any.Prescription().Dispensed().Generate();
        DispensedLine anyLine         = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn.AddDays(-1), [anyLine]));

        // Assert
        Assert.Equal("dispensedOn", refusal.ParamName);
    }

    [Fact]
    public void A_prescription_cannot_be_dispensed_after_its_last_dispensing_day() {
        // Arrange
        Prescription  anyPrescription = Any.Prescription().Dispensed().Generate();
        DispensedLine anyLine         = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.LastDispensingDay.AddDays(1), [anyLine]));

        // Assert
        Assert.Equal("dispensedOn", refusal.ParamName);
    }

    [Fact]
    public void A_claim_needs_at_least_one_line() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Dispensed().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, []));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_claim_can_dispense_every_line_of_its_prescription() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Dispensed().WithLines(lines => lines.WithCount(3)).Generate();
        List<DispensedLine> everyLine = anyPrescription.Lines.Select(line => Any.DispensedLine().WithPrescriptionLine(line).Generate()).ToList();

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, everyLine);

        // Assert
        Assert.Equal(3, claim.Lines.Count);
    }

    [Fact]
    public void A_claim_cannot_have_more_lines_than_its_prescription() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Dispensed().WithLines(lines => lines.WithCount(2)).Generate();
        List<DispensedLine> threeLines = Any.ListOf(Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0])).WithCount(3).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, threeLines));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_claim_cannot_dispense_a_line_of_another_prescription() {
        // Arrange
        Prescription  anyPrescription      = Any.Prescription().Dispensed().Generate();
        Prescription  anyOtherPrescription = Any.Prescription().Dispensed().Generate();
        DispensedLine foreignLine          = Any.DispensedLine().WithPrescriptionLine(anyOtherPrescription.Lines[0]).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, [foreignLine]));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_claim_cannot_dispense_the_same_line_twice() {
        // Arrange
        Prescription  anyPrescription = Any.Prescription().Dispensed().WithLines(lines => lines.WithMinCount(2)).Generate();
        DispensedLine first           = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();
        DispensedLine second          = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, [first, second]));

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_missing_line_in_the_list_is_refused() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Dispensed().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new ReimbursementClaim(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, [null!]));
    }

    [Fact]
    public void The_benefit_year_is_the_year_of_the_dispensing_date() {
        // Arrange
        Prescription  anyPrescription = Any.Prescription().Dispensed().WithIssuedOn(issuedOn => issuedOn.Between(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 20))).Generate();
        DispensedLine anyLine         = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate();

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), anyPrescription, new DateOnly(2027, 1, 5), [anyLine]);

        // Assert
        Assert.Equal(2027, claim.BenefitYear);
    }

    [Fact]
    public void The_totals_add_up_what_each_line_cost_and_what_the_statutory_scheme_left_at_the_insureds_charge() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Dispensed().WithLines(lines => lines.WithCount(2)).Generate();
        DispensedLine first = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0])
                                 .WithCategory(ReimbursementCategory.Major).WithPacks(2).WithUnitPrice(Euros(10m))
                                 .WithStatutoryReimbursement(Euros(13m)).Generate();
        DispensedLine second = Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[1])
                                  .WithCategory(ReimbursementCategory.Unlisted).WithPacks(1).WithUnitPrice(Euros(7.50m))
                                  .WithoutStatutoryReimbursement().Generate();

        // Act
        ReimbursementClaim claim = new(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, [first, second]);

        // Assert
        Assert.Equal(27.50m, claim.TotalPaid);
        Assert.Equal(14.50m, claim.TotalRemainingCharge);
    }

    [Fact]
    public void A_claim_does_not_follow_later_changes_to_the_list_of_lines_it_was_built_from() {
        // Arrange
        Prescription        anyPrescription = Any.Prescription().Dispensed().Generate();
        List<DispensedLine> lines           = [Any.DispensedLine().WithPrescriptionLine(anyPrescription.Lines[0]).Generate()];
        ReimbursementClaim  claim           = new(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, lines);

        // Act
        lines.Clear();

        // Assert
        Assert.Single(claim.Lines);
    }

}
