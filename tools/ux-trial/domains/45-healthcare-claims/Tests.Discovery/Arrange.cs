using Clinical.Domain;

using Insurance.Domain;

using JustDummies;

using Payments.Domain;

namespace HealthcareClaims.Domain;

/// <summary>Builds the few consistent scenarios the claim-file tests share. Only what a test is about is passed in; the rest is drawn.</summary>
internal static class Arrange {

    internal static ReimbursementSchedule Schedule(params (ReimbursementCategory Category, decimal Percent)[] entries) {
        Dictionary<ReimbursementCategory, ReimbursementRate> rates = new();
        foreach ((ReimbursementCategory category, decimal percent) in entries) {
            rates[category] = new ReimbursementRate(percent);
        }

        return new ReimbursementSchedule(rates);
    }

    /// <summary>A policy covering [start, start + days] for the given insured, with the figures the test is about.</summary>
    internal static SupplementaryPolicy Policy(Patient               insured,
                                               ReimbursementSchedule schedule,
                                               decimal               deductible,
                                               decimal               annualCeiling,
                                               DateOnly              start,
                                               int                   days) {
        return Any.SupplementaryPolicy()
                  .WithInsured(insured)
                  .WithSchedule(schedule)
                  .WithDeductible(new Deductible(deductible))
                  .WithAnnualCeiling(new Money(annualCeiling, Currency.EUR))
                  .WithPeriod(period => period.WithStart(start).WithDays(days))
                  .Generate();
    }

    /// <summary>A dispensed prescription of the insured, with the given number of lines.</summary>
    internal static Prescription DispensedPrescription(Patient insured, DateOnly issuedOn, int lineCount) {
        Prescription prescription = Any.Prescription()
                                       .WithPatient(insured)
                                       .WithIssuedOn(issuedOn)
                                       .WithLines(lines => lines.WithCount(lineCount))
                                       .Generate();
        prescription.Sign();
        prescription.Dispense(issuedOn);

        return prescription;
    }

    internal static DispensedLine Line(PrescriptionLine prescriptionLine,
                                       ReimbursementCategory category,
                                       int packs,
                                       decimal unitPrice,
                                       decimal? statutory = null) {
        return new DispensedLine(prescriptionLine, category, packs, new Money(unitPrice, Currency.EUR),
                                 statutory is null ? null : new Money(statutory.Value, Currency.EUR));
    }

    /// <summary>A claim on the first line of a fresh dispensed prescription of the insured: <paramref name="price"/> EUR for one pack.</summary>
    internal static ReimbursementClaim ClaimOfOneLine(Patient insured, DateOnly dispensedOn, ReimbursementCategory category, decimal price, decimal? statutory = null) {
        Prescription prescription = DispensedPrescription(insured, dispensedOn, lineCount: 1);

        return new ReimbursementClaim(Any.ClaimReference().Generate(),
                                      prescription,
                                      dispensedOn,
                                      [Line(prescription.Lines[0], category, packs: 1, price, statutory)]);
    }

}
