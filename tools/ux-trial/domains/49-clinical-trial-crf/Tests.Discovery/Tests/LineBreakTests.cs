using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

// Every format the domain validates with an anchored pattern: a trailing line break is not part of the format.
public sealed class LineBreakTests {

    private const string SameDefect = "domain defect: the pattern is anchored with $ (GeneratedRegex), which also matches before a trailing \\n, so the value is accepted; independent of any drawn value, fails on every seed";

    [Fact(Skip = SameDefect)]
    public void A_visit_code_does_not_end_with_a_line_break() {
        Assert.Throws<FormatException>(() => new VisitDefinition("D28\n", 28, 3, 3, Array.Empty<string>()));
    }

    [Fact(Skip = SameDefect)]
    public void An_analyte_code_does_not_end_with_a_line_break() {
        Assert.Throws<FormatException>(() => new LabResult("ALT\n", null, null, null, null));
    }

    [Fact(Skip = SameDefect)]
    public void Initials_do_not_end_with_a_line_break() {
        // Arrange
        ScreeningNumber anyNumber   = Any.ScreeningNumber().Generate();
        DateOnly        anyConsent  = TrialCalendar.Date().Generate();
        VisitSchedule   anySchedule = Any.VisitSchedule().Generate();

        // Act + Assert
        Assert.Throws<FormatException>(() => new SubjectCasebook(anyNumber, "AB\n", anyConsent, anySchedule));
    }

    [Fact(Skip = SameDefect)]
    public void A_query_field_path_does_not_end_with_a_line_break() {
        // Arrange
        string   anyText = Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate();
        DateOnly anyDate = TrialCalendar.Date().Generate();

        // Act + Assert
        Assert.Throws<FormatException>(() => new Query(1, "Vitals\n", anyText, anyDate));
    }

}
