using JustDummies;

namespace ClinicalTrial.Domain.Tests.Expert;

/// <summary>
///     Casebooks on a drawn protocol, for the tests of the lifecycle: they need visits to exist, not any visit in particular,
///     so the codes are read back from the casebook rather than written.
/// </summary>
internal static class Scenarios {

    /// <summary>A randomised casebook whose protocol has <paramref name="visits" /> visits and requires no laboratory analyte.</summary>
    public static SubjectCasebook Randomised(int visits = 3) {
        return Any.SubjectCasebook()
                  .WithSchedule(s => s.WithVisitCount(visits).WithoutRequiredAnalytes())
                  .Randomised()
                  .Generate();
    }

    public static string CodeOf(this SubjectCasebook casebook, int index) {
        return casebook.Schedule.Visits[index].Code;
    }

    /// <summary>Records everything the visit asks for, on the first day of its window.</summary>
    public static VisitForm RecordComplete(this SubjectCasebook casebook, string code) {
        DateOnly date = casebook.WindowOf(code).Earliest;

        return casebook.RecordVisit(code, Any.VisitEntry().OnDate(date).CompleteFor(casebook.Schedule.Find(code)!).Generate());
    }

    public static void RecordAndSubmit(this SubjectCasebook casebook, string code) {
        casebook.RecordComplete(code);
        casebook.SubmitVisit(code);
    }

    public static void RecordAndSign(this SubjectCasebook casebook, string code) {
        casebook.RecordAndSubmit(code);
        casebook.SignVisit(code);
    }

    public static void SignEveryVisit(this SubjectCasebook casebook) {
        foreach (VisitDefinition visit in casebook.Schedule.Visits) {
            casebook.RecordAndSign(visit.Code);
        }
    }

    /// <summary>A submitted form with one query, answered on the day it was raised.</summary>
    public static Query RaiseAnsweredQuery(this SubjectCasebook casebook, string code, DateOnly raisedOn) {
        Query query = casebook.RaiseQuery(code, "Vitals.HeartRateBpm", Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate(), raisedOn);

        casebook.AnswerQuery(code, query.Number, Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate(), raisedOn);

        return query;
    }

    public static DateOnly AnyDate() {
        return Any.DateOnly().Between(TrialCalendar.Earliest, TrialCalendar.Latest).Generate();
    }

    public static string AnyQueryText() {
        return Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate();
    }

}
