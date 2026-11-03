using Clinical.Domain;
using HealthcareClaims.Domain;
using JustDummies;
using Payments.Domain;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ReimbursementClaimTests {

    private static Money Euros(decimal amount) => new(amount, Currency.EUR);

    private static DispensedLine Dispensing(PrescriptionLine line) => Any.DispensedLine().WithPrescriptionLine(line).Generate();

    /// <summary>A prescription of <paramref name="lineCount" /> lines that has been through signing and dispensing.</summary>
    private static Prescription DispensedPrescriptionOf(int lineCount) => Any.Prescription().WithLines(lines => lines.WithCount(lineCount)).Dispensed().Generate();

    private static ReimbursementClaim ClaimOf(Prescription prescription, DateOnly dispensedOn, params DispensedLine[] lines) {
        return new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, dispensedOn, lines);
    }

    [Fact]
    public void A_claim_is_born_submitted_with_nothing_filled_in_yet() {
        Prescription anyPrescription = DispensedPrescriptionOf(1);

        ReimbursementClaim claim = ClaimOf(anyPrescription, anyPrescription.IssuedOn, Dispensing(anyPrescription.Lines[0]));

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Null(claim.Assessment);
        Assert.Null(claim.RejectionReason);
        Assert.Null(claim.Payment);
        Assert.False(claim.ConsumesCeiling);
    }

    [Fact]
    public void A_claim_keeps_what_it_was_built_from() {
        ClaimReference   anyReference    = Any.ClaimReference().Generate();
        Prescription     anyPrescription = DispensedPrescriptionOf(1);
        DispensedLine    anyLine         = Dispensing(anyPrescription.Lines[0]);

        ReimbursementClaim claim = new(anyReference, anyPrescription, anyPrescription.IssuedOn, [anyLine]);

        Assert.Same(anyReference, claim.Reference);
        Assert.Same(anyPrescription, claim.Prescription);
        Assert.Equal(anyPrescription.IssuedOn, claim.DispensedOn);
        Assert.Same(anyLine, Assert.Single(claim.Lines));
    }

    [Fact]
    public void A_prescription_that_was_never_signed_cannot_be_claimed() {
        Prescription draft = Any.Prescription().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => ClaimOf(draft, draft.IssuedOn, Dispensing(draft.Lines[0])));

        Assert.Equal("prescription", refusal.ParamName);
    }

    [Fact]
    public void A_prescription_that_was_signed_but_not_dispensed_cannot_be_claimed() {
        Prescription signed = Any.Prescription().Generate();
        signed.Sign();

        Assert.Throws<ArgumentException>(() => ClaimOf(signed, signed.IssuedOn, Dispensing(signed.Lines[0])));
    }

    [Fact]
    public void A_cancelled_prescription_cannot_be_claimed() {
        Prescription cancelled = Any.Prescription().Generate();
        cancelled.Cancel();

        Assert.Throws<ArgumentException>(() => ClaimOf(cancelled, cancelled.IssuedOn, Dispensing(cancelled.Lines[0])));
    }

    [Fact]
    public void A_claim_may_be_dispensed_on_the_day_the_prescription_was_issued() {
        Prescription anyPrescription = DispensedPrescriptionOf(1);

        ReimbursementClaim claim = ClaimOf(anyPrescription, anyPrescription.IssuedOn, Dispensing(anyPrescription.Lines[0]));

        Assert.Equal(anyPrescription.IssuedOn, claim.DispensedOn);
    }

    [Fact]
    public void A_claim_may_be_dispensed_on_the_last_dispensing_day() {
        Prescription anyPrescription = DispensedPrescriptionOf(1);

        ReimbursementClaim claim = ClaimOf(anyPrescription, anyPrescription.LastDispensingDay, Dispensing(anyPrescription.Lines[0]));

        Assert.Equal(anyPrescription.LastDispensingDay, claim.DispensedOn);
    }

    [Fact]
    public void A_claim_cannot_be_dispensed_before_the_prescription_was_issued() {
        Prescription anyPrescription = DispensedPrescriptionOf(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => ClaimOf(anyPrescription, anyPrescription.IssuedOn.AddDays(-1), Dispensing(anyPrescription.Lines[0])));
    }

    [Fact]
    public void A_claim_cannot_be_dispensed_after_the_last_dispensing_day() {
        Prescription anyPrescription = DispensedPrescriptionOf(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => ClaimOf(anyPrescription, anyPrescription.LastDispensingDay.AddDays(1), Dispensing(anyPrescription.Lines[0])));
    }

    [Fact]
    public void A_claim_needs_at_least_one_line() {
        Prescription anyPrescription = DispensedPrescriptionOf(1);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => ClaimOf(anyPrescription, anyPrescription.IssuedOn));

        Assert.Equal("lines", refusal.ParamName);
    }

    [Fact]
    public void A_claim_may_leave_some_prescription_lines_undispensed() {
        Prescription anyPrescription = DispensedPrescriptionOf(3);

        ReimbursementClaim claim = ClaimOf(anyPrescription, anyPrescription.IssuedOn, Dispensing(anyPrescription.Lines[1]));

        Assert.Single(claim.Lines);
    }

    [Fact]
    public void A_claim_may_dispense_every_prescription_line() {
        Prescription anyPrescription = DispensedPrescriptionOf(3);

        ReimbursementClaim claim = ClaimOf(anyPrescription, anyPrescription.IssuedOn, anyPrescription.Lines.Select(Dispensing).ToArray());

        Assert.Equal(3, claim.Lines.Count);
    }

    [Fact]
    public void A_claim_cannot_have_more_lines_than_the_prescription() {
        Prescription anyPrescription = DispensedPrescriptionOf(2);
        DispensedLine first          = Dispensing(anyPrescription.Lines[0]);
        DispensedLine second         = Dispensing(anyPrescription.Lines[1]);
        DispensedLine again          = Dispensing(anyPrescription.Lines[0]);

        Assert.Throws<ArgumentException>(() => ClaimOf(anyPrescription, anyPrescription.IssuedOn, first, second, again));
    }

    [Fact]
    public void A_claim_cannot_dispense_a_line_of_another_prescription() {
        Prescription anyPrescription      = DispensedPrescriptionOf(1);
        Prescription anyOtherPrescription = DispensedPrescriptionOf(1);

        Assert.Throws<ArgumentException>(() => ClaimOf(anyPrescription, anyPrescription.IssuedOn, Dispensing(anyOtherPrescription.Lines[0])));
    }

    [Fact]
    public void A_claim_cannot_dispense_a_look_alike_of_a_prescription_line() {
        Prescription     anyPrescription = DispensedPrescriptionOf(1);
        PrescriptionLine original        = anyPrescription.Lines[0];
        PrescriptionLine lookAlike       = new(original.Molecule, original.Dosage, original.Route, original.Duration);

        Assert.Throws<ArgumentException>(() => ClaimOf(anyPrescription, anyPrescription.IssuedOn, Dispensing(lookAlike)));
    }

    [Fact]
    public void A_claim_cannot_dispense_the_same_prescription_line_twice() {
        Prescription anyPrescription = DispensedPrescriptionOf(2);
        DispensedLine once           = Dispensing(anyPrescription.Lines[0]);
        DispensedLine twice          = Dispensing(anyPrescription.Lines[0]);

        Assert.Throws<ArgumentException>(() => ClaimOf(anyPrescription, anyPrescription.IssuedOn, once, twice));
    }

    [Fact]
    public void A_claim_cannot_hold_an_empty_line() {
        Prescription anyPrescription = DispensedPrescriptionOf(2);
        DispensedLine anyLine        = Dispensing(anyPrescription.Lines[0]);

        Assert.Throws<ArgumentNullException>(() => ClaimOf(anyPrescription, anyPrescription.IssuedOn, anyLine, null!));
    }

    [Fact]
    public void A_claim_cannot_be_built_without_its_reference_prescription_or_lines() {
        ClaimReference anyReference    = Any.ClaimReference().Generate();
        Prescription   anyPrescription = DispensedPrescriptionOf(1);
        DispensedLine  anyLine         = Dispensing(anyPrescription.Lines[0]);

        Assert.Throws<ArgumentNullException>(() => new ReimbursementClaim(null!, anyPrescription, anyPrescription.IssuedOn, [anyLine]));
        Assert.Throws<ArgumentNullException>(() => new ReimbursementClaim(anyReference, null!, anyPrescription.IssuedOn, [anyLine]));
        Assert.Throws<ArgumentNullException>(() => new ReimbursementClaim(anyReference, anyPrescription, anyPrescription.IssuedOn, null!));
    }

    [Fact]
    public void A_claim_does_not_follow_the_list_of_lines_it_was_built_from() {
        Prescription anyPrescription = DispensedPrescriptionOf(2);
        List<DispensedLine> lines    = [Dispensing(anyPrescription.Lines[0])];
        ReimbursementClaim claim     = new(Any.ClaimReference().Generate(), anyPrescription, anyPrescription.IssuedOn, lines);

        lines.Add(Dispensing(anyPrescription.Lines[1]));

        Assert.Single(claim.Lines);
    }

    [Fact]
    public void The_benefit_year_is_the_year_of_the_dispensing_not_the_year_of_the_prescription() {
        Prescription issuedInDecember = Any.Prescription().WithIssuedOn(new DateOnly(2025, 12, 20)).Dispensed().Generate();

        ReimbursementClaim claim = ClaimOf(issuedInDecember, new DateOnly(2026, 1, 2), Dispensing(issuedInDecember.Lines[0]));

        Assert.Equal(2026, claim.BenefitYear);
    }

    [Fact]
    public void The_totals_add_up_the_price_paid_and_the_remaining_charge_of_every_line() {
        Prescription anyPrescription = DispensedPrescriptionOf(2);
        DispensedLine reimbursedLine = Any.DispensedLine()
                                          .WithPrescriptionLine(anyPrescription.Lines[0])
                                          .WithCategory(ReimbursementCategory.Major)
                                          .WithUnitPrice(Euros(12.50m)).WithPacks(3)
                                          .WithStatutoryReimbursement(Euros(22.50m))
                                          .Generate();
        DispensedLine unreimbursedLine = Any.DispensedLine()
                                            .WithPrescriptionLine(anyPrescription.Lines[1])
                                            .WithUnitPrice(Euros(8.00m)).WithPacks(1)
                                            .WithoutStatutoryReimbursement()
                                            .Generate();

        ReimbursementClaim claim = ClaimOf(anyPrescription, anyPrescription.IssuedOn, reimbursedLine, unreimbursedLine);

        Assert.Equal(45.50m, claim.TotalPaid);
        Assert.Equal(23.00m, claim.TotalRemainingCharge);
    }

}
