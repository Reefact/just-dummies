using Clinical.Domain;

using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;

using Insurance.Domain;

using JustDummies;

using Payments.Domain;

namespace HealthcareClaims.Tests.Regular.Support;

/// <summary>The arrangements the computation tests share: a policy whose figures are the point, and a claim whose lines are.</summary>
internal static class Scenarios {

    public static ReimbursementSchedule ScheduleOf(params (ReimbursementCategory Category, decimal Percent)[] rates) {
        Dictionary<ReimbursementCategory, ReimbursementRate> table = new();
        foreach ((ReimbursementCategory category, decimal percent) in rates) {
            table[category] = new ReimbursementRate(percent);
        }

        return new ReimbursementSchedule(table);
    }

    /// <summary>A policy whose whole coverage period falls inside calendar year 2026, so every claim dispensed under it consumes the same ceiling.</summary>
    public static SupplementaryPolicy PolicyWithin2026(ReimbursementSchedule schedule, decimal deductible, decimal annualCeiling) {
        return Any.SupplementaryPolicy()
                  .WithPeriod(period => period.StartingBetween(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).LastingDays(300))
                  .WithSchedule(schedule)
                  .WithDeductible(new Deductible(deductible))
                  .WithAnnualCeiling(new Money(annualCeiling, Currency.EUR))
                  .Generate();
    }

    /// <summary>A claim by the policy's insured whose lines are exactly the ones given; the day defaults to an arbitrary one of the policy period.</summary>
    public static ReimbursementClaim ClaimOf(SupplementaryPolicy policy,
                                             params (ReimbursementCategory Category, decimal UnitPrice, int Packs, decimal? Statutory)[] lines) {
        return ClaimOn(policy, null, lines);
    }

    public static ReimbursementClaim ClaimOn(SupplementaryPolicy policy,
                                             DateOnly?           day,
                                             params (ReimbursementCategory Category, decimal UnitPrice, int Packs, decimal? Statutory)[] lines) {
        DateOnly dispensedOn = day ?? Any.DateOnly().Between(policy.Period.Start, policy.Period.End).Generate();
        Prescription prescription = Any.Prescription()
                                       .Dispensed()
                                       .WithPatient(policy.Insured)
                                       .WithIssuedOn(issuedOn => issuedOn.Between(dispensedOn.AddDays(-60), dispensedOn))
                                       .WithLines(prescriptionLines => prescriptionLines.WithMinCount(lines.Length))
                                       .Generate();
        List<DispensedLine> dispensed = new();
        for (int i = 0; i < lines.Length; i++) {
            AnyDispensedLine anyLine = Any.DispensedLine()
                                          .WithPrescriptionLine(prescription.Lines[i])
                                          .WithCategory(lines[i].Category)
                                          .WithPacks(lines[i].Packs)
                                          .WithUnitPrice(new Money(lines[i].UnitPrice, Currency.EUR));
            anyLine = lines[i].Statutory is { } statutory
                          ? anyLine.WithStatutoryReimbursement(new Money(statutory, Currency.EUR))
                          : anyLine.WithoutStatutoryReimbursement();
            dispensed.Add(anyLine.Generate());
        }

        return new ReimbursementClaim(Any.ClaimReference().Generate(), prescription, dispensedOn, dispensed);
    }

}
