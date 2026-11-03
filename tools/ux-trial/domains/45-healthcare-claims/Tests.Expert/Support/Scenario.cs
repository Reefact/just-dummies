using HealthcareClaims.Domain;
using Insurance.Domain;
using JustDummies;
using Payments.Domain;

namespace HealthcareClaims.Tests.Expert.Support;

/// <summary>Files in which the policy's terms are given by the test and everything else about it is arbitrary.</summary>
internal static class Scenario {

    public static readonly CoveragePeriod Year2026 = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    /// <summary>A Tuesday, and a business day for the days that follow it up to Friday.</summary>
    public static readonly DateOnly Tuesday = new(2026, 3, 10);

    /// <summary>Every category the statutory scheme lists is reimbursed in full: the price paid is what the policy owes.</summary>
    public static readonly ReimbursementSchedule EverythingListed = Schedules.Of((ReimbursementCategory.Irreplaceable, 100m),
                                                                                (ReimbursementCategory.Major, 100m),
                                                                                (ReimbursementCategory.Moderate, 100m),
                                                                                (ReimbursementCategory.Low, 100m));

    public static ClaimFile FileOf(ReimbursementSchedule schedule, decimal deductible = 0m, decimal annualCeiling = 100_000m, CoveragePeriod? period = null) {
        SupplementaryPolicy policy = Any.SupplementaryPolicy()
                                        .WithPeriod(period ?? Year2026)
                                        .WithSchedule(schedule)
                                        .WithDeductible(deductible)
                                        .WithAnnualCeiling(annualCeiling)
                                        .Generate();

        return new ClaimFile(policy);
    }

    public static ReimbursementClaim Submitted(ClaimFile file, DateOnly dispensedOn, params Dispensing[] dispensings) {
        ReimbursementClaim claim = Any.ReimbursementClaim().ForInsured(file.Policy.Insured).DispensedOn(dispensedOn).Dispensing(dispensings).Generate();
        file.Submit(claim);

        return claim;
    }

    public static ReimbursementClaim Assessed(ClaimFile file, DateOnly dispensedOn, params Dispensing[] dispensings) {
        ReimbursementClaim claim = Submitted(file, dispensedOn, dispensings);
        file.Assess(claim.Reference);

        return claim;
    }

    public static ReimbursementClaim Approved(ClaimFile file, DateOnly dispensedOn, params Dispensing[] dispensings) {
        ReimbursementClaim claim = Assessed(file, dispensedOn, dispensings);
        file.Approve(claim.Reference);

        return claim;
    }

    /// <summary>An insurer who is not the insured's bank: a transfer between one account and itself is refused.</summary>
    public static Party InsurerOf(ClaimFile file) {
        Iban anyOtherAccount = Any.Iban().DifferentFrom(file.Policy.Payee.Account).Generate();

        return Any.Party().WithAccount(anyOtherAccount).Generate();
    }

}
