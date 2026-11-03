using Clinical.Domain;
using HealthcareClaims.Domain;
using Insurance.Domain;
using HealthcareClaims.Tests.Expert.Support;
using JustDummies;
using Payments.Domain;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ClaimFileSubmissionTests {

    private static readonly CoveragePeriod Year2026 = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
    private static readonly DateOnly       Tuesday  = new(2026, 3, 10);

    // Half of a major medicine is reimbursed: enough for a claim to have something payable, whatever its status needs.
    private static readonly ReimbursementSchedule HalfOfMajor = Schedules.Of((ReimbursementCategory.Major, 50m));
    private static readonly Dispensing            OneMajorBox = new(ReimbursementCategory.Major, UnitPrice: 100m);

    public static TheoryData<ClaimStatus> StatusesOfAClaimThatStillOccupiesItsPrescription => new() {
        ClaimStatus.Submitted, ClaimStatus.Assessed, ClaimStatus.Approved, ClaimStatus.Paid
    };

    private static ClaimFile NewFile() {
        // No deductible and a ceiling nothing here approaches: an approved or a paid claim must have something payable.
        SupplementaryPolicy policy = Any.SupplementaryPolicy().WithPeriod(Year2026).WithSchedule(HalfOfMajor).WithDeductible(0m).WithAnnualCeiling(10_000m).Generate();

        return new ClaimFile(policy);
    }

    private static ReimbursementClaim ClaimOf(ClaimFile file, DateOnly dispensedOn) {
        return Any.ReimbursementClaim().ForInsured(file.Policy.Insured).DispensedOn(dispensedOn).Dispensing(OneMajorBox).Generate();
    }

    private static Party InsurerOf(ClaimFile file) {
        Iban anyOtherAccount = Any.Iban().DifferentFrom(file.Policy.Payee.Account).Generate();

        return Any.Party().WithAccount(anyOtherAccount).Generate();
    }

    private static void TakeToStatus(ClaimFile file, ReimbursementClaim claim, ClaimStatus status) {
        if (status == ClaimStatus.Submitted) { return; }
        file.Assess(claim.Reference);
        if (status == ClaimStatus.Assessed) { return; }
        file.Approve(claim.Reference);
        if (status == ClaimStatus.Approved) { return; }
        file.Pay(claim.Reference, InsurerOf(file), claim.DispensedOn.AddDays(2));
    }

    [Fact]
    public void A_file_belongs_to_the_policy_it_was_opened_for() {
        SupplementaryPolicy anyPolicy = Any.SupplementaryPolicy().Generate();

        ClaimFile file = new(anyPolicy);

        Assert.Same(anyPolicy, file.Policy);
        Assert.Empty(file.Claims);
    }

    [Fact]
    public void A_file_cannot_be_opened_without_a_policy() {
        Assert.Throws<ArgumentNullException>(() => new ClaimFile(null!));
    }

    [Fact]
    public void A_submitted_claim_is_found_by_its_reference() {
        ClaimFile          file  = NewFile();
        ReimbursementClaim claim = ClaimOf(file, Tuesday);

        file.Submit(claim);

        Assert.Same(claim, file.Find(claim.Reference));
        Assert.Same(claim, Assert.Single(file.Claims));
        Assert.Equal(ClaimStatus.Submitted, claim.Status);
    }

    [Fact]
    public void A_reference_that_was_never_submitted_is_not_found() {
        ClaimFile file = NewFile();
        ClaimReference anyReference = Any.ClaimReference().Generate();

        Assert.Null(file.Find(anyReference));
    }

    [Fact]
    public void A_claim_cannot_be_looked_up_without_its_reference() {
        ClaimFile file = NewFile();

        Assert.Throws<ArgumentNullException>(() => file.Find(null!));
    }

    [Fact]
    public void A_missing_claim_cannot_be_submitted() {
        ClaimFile file = NewFile();

        Assert.Throws<ArgumentNullException>(() => file.Submit(null!));
    }

    [Fact]
    public void The_same_claim_cannot_be_submitted_twice() {
        ClaimFile          file  = NewFile();
        ReimbursementClaim claim = ClaimOf(file, Tuesday);
        file.Submit(claim);

        // Not a refusal for a business reason: the file is simply being misused.
        Assert.Throws<InvalidOperationException>(() => file.Submit(claim));
        Assert.Single(file.Claims);
    }

    [Fact]
    public void Another_claim_under_a_reference_already_in_the_file_is_refused() {
        ClaimFile          file  = NewFile();
        ReimbursementClaim first = ClaimOf(file, Tuesday);
        file.Submit(first);
        ReimbursementClaim sameReference = Any.ReimbursementClaim()
                                              .WithReference(first.Reference)
                                              .ForInsured(file.Policy.Insured)
                                              .DispensedOn(Tuesday)
                                              .Generate();

        Assert.Throws<InvalidOperationException>(() => file.Submit(sameReference));
        Assert.Single(file.Claims);
    }

    [Fact]
    public void A_claim_that_already_went_through_assessment_cannot_be_submitted_again() {
        ClaimFile          file  = NewFile();
        ReimbursementClaim claim = ClaimOf(file, Tuesday);
        file.Submit(claim);
        file.Assess(claim.Reference);
        ClaimFile anotherFileOfTheSamePolicy = new(file.Policy);

        Assert.Throws<InvalidOperationException>(() => anotherFileOfTheSamePolicy.Submit(claim));
    }

    [Fact]
    public void A_claim_for_the_prescription_of_someone_else_is_refused() {
        ClaimFile file          = NewFile();
        Patient   anyStranger   = Any.Patient().WithNir(Any.Nir().DifferentFrom(file.Policy.Insured.Nir).Generate()).Generate();
        ReimbursementClaim claim = Any.ReimbursementClaim().ForInsured(anyStranger).DispensedOn(Tuesday).Generate();

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        Assert.Equal(ClaimRefusalReason.PatientIsNotInsured, refusal.Reason);
        Assert.Empty(file.Claims);
    }

    [Theory]
    [InlineData(2025, 12, 31)]
    [InlineData(2027, 1, 1)]
    public void A_claim_dispensed_outside_the_coverage_period_is_refused(int year, int month, int day) {
        ClaimFile          file  = NewFile();
        ReimbursementClaim claim = ClaimOf(file, new DateOnly(year, month, day));

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        Assert.Equal(ClaimRefusalReason.OutsideCoveragePeriod, refusal.Reason);
        Assert.Empty(file.Claims);
    }

    [Theory]
    [InlineData(2026, 1, 1)]
    [InlineData(2026, 12, 31)]
    public void A_claim_dispensed_on_the_first_or_the_last_day_of_the_coverage_period_is_accepted(int year, int month, int day) {
        ClaimFile          file  = NewFile();
        ReimbursementClaim claim = ClaimOf(file, new DateOnly(year, month, day));

        file.Submit(claim);

        Assert.Same(claim, Assert.Single(file.Claims));
    }

    [Theory]
    [MemberData(nameof(StatusesOfAClaimThatStillOccupiesItsPrescription))]
    public void A_prescription_already_claimed_cannot_be_claimed_again_while_its_claim_stands(ClaimStatus status) {
        ClaimFile          file  = NewFile();
        ReimbursementClaim first = ClaimOf(file, Tuesday);
        file.Submit(first);
        TakeToStatus(file, first, status);
        ReimbursementClaim again = Any.ReimbursementClaim().ForPrescription(first.Prescription).DispensedOn(Tuesday).Dispensing(OneMajorBox).Generate();

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(again));

        Assert.Equal(ClaimRefusalReason.PrescriptionAlreadyClaimed, refusal.Reason);
        Assert.Single(file.Claims);
    }

    [Fact]
    public void A_prescription_whose_claim_was_rejected_can_be_claimed_again() {
        ClaimFile          file  = NewFile();
        ReimbursementClaim first = ClaimOf(file, Tuesday);
        file.Submit(first);
        file.Assess(first.Reference);
        file.Reject(first.Reference, ClaimRefusalReason.MissingDocuments);
        ReimbursementClaim again = Any.ReimbursementClaim().ForPrescription(first.Prescription).DispensedOn(Tuesday).Dispensing(OneMajorBox).Generate();

        file.Submit(again);

        Assert.Equal(2, file.Claims.Count);
    }

    [Fact]
    public void A_claim_of_a_stranger_outside_the_coverage_period_is_refused_for_the_stranger_first() {
        ClaimFile file        = NewFile();
        Patient   anyStranger = Any.Patient().WithNir(Any.Nir().DifferentFrom(file.Policy.Insured.Nir).Generate()).Generate();
        ReimbursementClaim claim = Any.ReimbursementClaim().ForInsured(anyStranger).DispensedOn(new DateOnly(2027, 1, 1)).Generate();

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(claim));

        Assert.Equal(ClaimRefusalReason.PatientIsNotInsured, refusal.Reason);
    }

    [Fact]
    public void A_claim_on_an_already_claimed_prescription_outside_the_coverage_period_is_refused_for_the_period_first() {
        ClaimFile    file         = NewFile();
        Prescription prescription = Any.Prescription()
                                       .WithPatient(file.Policy.Insured)
                                       .WithIssuedOn(new DateOnly(2026, 12, 20))
                                       .Dispensed()
                                       .Generate();
        ReimbursementClaim first = Any.ReimbursementClaim().ForPrescription(prescription).DispensedOn(new DateOnly(2026, 12, 30)).Dispensing(OneMajorBox).Generate();
        file.Submit(first);
        ReimbursementClaim afterTheYearEnd = Any.ReimbursementClaim().ForPrescription(prescription).DispensedOn(new DateOnly(2027, 1, 5)).Dispensing(OneMajorBox).Generate();

        ClaimRefusedException refusal = Assert.Throws<ClaimRefusedException>(() => file.Submit(afterTheYearEnd));

        Assert.Equal(ClaimRefusalReason.OutsideCoveragePeriod, refusal.Reason);
    }

    [Fact]
    public void Two_different_prescriptions_can_be_claimed_in_the_same_file() {
        ClaimFile file = NewFile();

        file.Submit(ClaimOf(file, Tuesday));
        file.Submit(ClaimOf(file, Tuesday));

        Assert.Equal(2, file.Claims.Count);
    }

}
