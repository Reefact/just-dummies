using System;
using System.Collections.Generic;
using System.Linq;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Support;

/// <summary>
///     The protocol the casebook tests run under, and the casebook of one randomised subject. The schedule is
///     significant to those tests (they are about windows, orders and required analytes), so its days are literals;
///     everything the schedule does not constrain (consent date, screening number, randomisation number and arm) is drawn.
/// </summary>
public sealed class Trial {

    public const string V1 = "V1";   // day 8  ± 2  -> window days  6..10, requires ALT
    public const string V2 = "V2";   // day 15 ± 3  -> window days 12..18, requires nothing
    public const string V3 = "V3";   // day 29 ± 3  -> window days 26..32, requires ALT and HGB

    public static VisitSchedule Protocol() {
        return new VisitSchedule(new[] {
            new VisitDefinition(V1, 8, 2, 2, new[] { "ALT" }),
            new VisitDefinition(V2, 15, 3, 3, Array.Empty<string>()),
            new VisitDefinition(V3, 29, 3, 3, new[] { "ALT", "HGB" })
        });
    }

    private Trial(SubjectCasebook casebook, DateOnly consentDate) {
        Casebook    = casebook;
        ConsentDate = consentDate;
    }

    public SubjectCasebook Casebook    { get; }
    public DateOnly        ConsentDate { get; }

    public DateOnly RandomisationDate => Casebook.Randomisation!.Date;

    /// <summary>The calendar date of a study day (day 1 is the randomisation date; there is no day 0).</summary>
    public DateOnly DateOfDay(int studyDay) {
        return RandomisationDate.AddDays(studyDay > 0 ? studyDay - 1 : studyDay);
    }

    /// <summary>A subject who gave consent and was screened under the protocol, not randomised yet.</summary>
    public static Trial Screened() {
        DateOnly       anyConsentDate = TrialFields.Date().Generate();
        SubjectCasebook casebook      = Any.SubjectCasebook().WithConsentDate(anyConsentDate).WithSchedule(Protocol()).Generate();

        return new Trial(casebook, anyConsentDate);
    }

    /// <summary>A subject randomised within the screening period.</summary>
    public static Trial Randomised() {
        Trial trial = Screened();
        DateOnly anyRandomisationDate = trial.ConsentDate.AddDays(Any.Int32().Between(0, SubjectCasebook.MaxScreeningDays).Generate());
        trial.Casebook.Randomise(Any.Randomisation().WithDate(anyRandomisationDate).Generate());

        return trial;
    }

    /// <summary>An entry for a visit that took place on a study day, with every field resolved.</summary>
    public VisitEntry CompleteEntry(string visitCode, int studyDay) {
        VisitDefinition definition = Casebook.Schedule.Find(visitCode)!;
        List<LabResult> labs       = definition.RequiredAnalytes.Select(code => new AnyLabResult().WithAnalyteCode(code).Measured().Generate()).ToList();

        return new VisitEntry(Observed<DateOnly>.Of(DateOfDay(studyDay)), new AnyVitalSigns().Complete().Generate(), labs);
    }

    /// <summary>An entry for a visit that took place on a study day, with no data entered yet.</summary>
    public VisitEntry BareEntry(int studyDay) {
        return new VisitEntry(Observed<DateOnly>.Of(DateOfDay(studyDay)), null, Array.Empty<LabResult>());
    }

    /// <summary>An entry for a visit that did not take place.</summary>
    public static VisitEntry MissedEntry(MissingReason reason) {
        return new VisitEntry(Observed<DateOnly>.Missing(reason), null, Array.Empty<LabResult>());
    }

    /// <summary>Records a complete visit on its target day.</summary>
    public VisitForm RecordComplete(string visitCode) {
        return Casebook.RecordVisit(visitCode, CompleteEntry(visitCode, Casebook.Schedule.Find(visitCode)!.TargetDay));
    }

    /// <summary>Records, submits and signs a complete visit on its target day.</summary>
    public void RecordAndSign(string visitCode) {
        RecordComplete(visitCode);
        Casebook.SubmitVisit(visitCode);
        Casebook.SignVisit(visitCode);
    }

    public void RecordAndSignAll() {
        foreach (VisitDefinition visit in Casebook.Schedule.Visits) { RecordAndSign(visit.Code); }
    }

    /// <summary>A casebook ready to lock: every visit signed, every adverse event resolved.</summary>
    public static Trial ReadyToLock() {
        Trial trial = Randomised();
        trial.RecordAndSignAll();

        return trial;
    }

}
