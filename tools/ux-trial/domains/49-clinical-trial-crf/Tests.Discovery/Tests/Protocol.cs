using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

/// <summary>
///     The protocol the casebook tests run under. Its dates and windows are literals: the tests are about them.
///     Day 1 is the randomisation date; D8 is due on days 6 to 10, D29 on days 26 to 32, D85 on days 78 to 92.
/// </summary>
internal static class Protocol {

    public static readonly DateOnly Consent         = new DateOnly(2024, 3, 1);
    public static readonly DateOnly RandomisationDay = new DateOnly(2024, 3, 11);

    public static DateOnly DateOfDay(int studyDay) => RandomisationDay.AddDays(studyDay - 1);

    public static VisitSchedule Schedule() {
        return new VisitSchedule(new[] {
            new VisitDefinition("D8", 8, 2, 2, new[] { "ALT" }),
            new VisitDefinition("D29", 29, 3, 3, new[] { "ALT", "HGB" }),
            new VisitDefinition("D85", 85, 7, 7, Array.Empty<string>())
        });
    }

    /// <summary>A casebook consented on <see cref="Consent" />, not randomised yet. Number and initials are dummies.</summary>
    public static SubjectCasebook Casebook() {
        return Any.SubjectCasebook().WithConsentDate(Consent).WithSchedule(Schedule()).Generate();
    }

    public static SubjectCasebook RandomisedCasebook() {
        SubjectCasebook casebook = Casebook();

        casebook.Randomise(Any.Randomisation().WithDate(RandomisationDay).Generate());

        return casebook;
    }

    /// <summary>What a site enters for a visit that took place on <paramref name="date" />: the content is arbitrary.</summary>
    public static VisitEntry Entry(DateOnly date) {
        return Any.VisitEntry().WithVisitDate(Observed<DateOnly>.Of(date)).Generate();
    }

    /// <summary>A visit that took place on <paramref name="date" /> with every field resolved and the given analytes reported.</summary>
    public static VisitEntry CompleteEntry(DateOnly date, params string[] analytes) {
        List<LabResult> labs = analytes.Select(analyte => Any.LabResult().WithAnalyteCode(analyte).Resolved().Generate()).ToList();

        return Any.VisitEntry()
                  .WithVisitDate(Observed<DateOnly>.Of(date))
                  .WithVitals(Any.VitalSigns().FullyResolved().Generate())
                  .WithLabs(labs)
                  .Generate();
    }

    public static VisitEntry DidNotTakePlace(MissingReason reason) {
        return new VisitEntry(Observed<DateOnly>.Missing(reason), null, Array.Empty<LabResult>());
    }

    /// <summary>A non-fatal event starting on or after consent, within 300 days: its content is arbitrary.</summary>
    public static AdverseEvent AnyEvent() {
        return Any.AdverseEvent().NonFatal().WithOnsetDate(Any.DateOnly().Between(Consent, Consent.AddDays(300)).Generate()).Generate();
    }

    public static AdverseEvent Fatal(DateOnly onset, DateOnly death) {
        return new AdverseEvent("Cardiac arrest", onset, null, death, 5, true, Causality.Probable, AdverseEventOutcome.Fatal, null);
    }

    public static AdverseEvent NonFatalOn(DateOnly onset) {
        return Any.AdverseEvent().NonFatal().WithOnsetDate(onset).Generate();
    }

    public static void AssertRefused(CasebookRefusal expected, Action action) {
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(action);

        Assert.Equal(expected, refusal.Reason);
    }

    /// <summary>A casebook with every visit recorded, submitted and signed (the visits' content is arbitrary but complete).</summary>
    public static SubjectCasebook SignedCasebook() {
        SubjectCasebook casebook = RandomisedCasebook();

        casebook.RecordVisit("D8", CompleteEntry(DateOfDay(8), "ALT"));
        casebook.RecordVisit("D29", CompleteEntry(DateOfDay(29), "ALT", "HGB"));
        casebook.RecordVisit("D85", CompleteEntry(DateOfDay(85)));

        foreach (string code in new[] { "D8", "D29", "D85" }) {
            casebook.SubmitVisit(code);
            casebook.SignVisit(code);
        }

        return casebook;
    }

}
