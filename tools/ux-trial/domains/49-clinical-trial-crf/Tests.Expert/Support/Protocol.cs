using JustDummies;

namespace ClinicalTrial.Domain.Tests.Expert;

/// <summary>
///     The protocol the window, order and screening tests are about, so its days are literals:
///     V1 on day 1, V2 on day 8 (6 to 10), V3 on day 15 (13 to 17), V4 on day 28 (25 to 31).
/// </summary>
internal static class Protocol {

    /// <summary>Study day 1 of the casebooks that need a known calendar.</summary>
    public static readonly DateOnly RandomisationDate = new DateOnly(2026, 3, 10);

    public static VisitSchedule Schedule() {
        return new VisitSchedule([
            new VisitDefinition("V1", 1, 0, 0, []),
            new VisitDefinition("V2", 8, 2, 2, []),
            new VisitDefinition("V3", 15, 2, 2, []),
            new VisitDefinition("V4", 28, 3, 3, ["ALT", "HGB"])
        ]);
    }

    /// <summary>A casebook on this protocol, randomised on <see cref="RandomisationDate" />.</summary>
    public static SubjectCasebook RandomisedCasebook() {
        SubjectCasebook casebook = Any.SubjectCasebook()
                                      .WithSchedule(Schedule())
                                      .WithConsentDate(c => c.Between(RandomisationDate.AddDays(-42), RandomisationDate))
                                      .Generate();

        casebook.Randomise(Any.Randomisation().WithDate(RandomisationDate).Generate());

        return casebook;
    }

    /// <summary>The calendar date of a study day of <see cref="RandomisationDate" />.</summary>
    public static DateOnly DayOf(int studyDay) {
        return RandomisationDate.AddDays(studyDay - 1);
    }

}
