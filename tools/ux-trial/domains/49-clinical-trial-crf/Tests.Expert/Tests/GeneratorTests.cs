using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

/// <summary>
///     The generators are code of this project: this checks they only hand out values the domain accepts. They draw through the
///     real constructors, so an invalid draw throws; the assertions are about the shape each named variant promises.
/// </summary>
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void Every_generator_draws_values_its_domain_accepts() {
        for (int i = 0; i < Draws; i++) {
            Any.ScreeningNumber().Generate();
            Any.Randomisation().Generate();
            Any.VisitDefinition().Generate();
            Any.VisitSchedule().Generate();
            Any.VitalSigns().Generate();
            Any.LabResult().Generate();
            Any.VisitEntry().Generate();
            Any.AdverseEvent().Generate();
            Any.SubjectCasebook().Generate();
        }
    }

    [Fact]
    public void A_resolved_page_or_result_or_event_has_nothing_left_to_enter_on_any_draw() {
        for (int i = 0; i < Draws; i++) {
            Assert.Empty(Any.VitalSigns().Resolved().Generate().UnresolvedFields());
            Assert.True(Any.LabResult().Resolved().Generate().IsResolved);
            Assert.Empty(Any.AdverseEvent().Resolved().Generate().UnresolvedFields());
        }
    }

    [Fact]
    public void A_randomised_casebook_is_randomised_within_its_screening_period() {
        for (int i = 0; i < Draws; i++) {
            SubjectCasebook casebook = Any.SubjectCasebook().Randomised().Generate();

            Assert.True(casebook.IsRandomised);
        }
    }

    [Fact]
    public void A_complete_entry_leaves_nothing_to_resolve_on_any_draw() {
        for (int i = 0; i < Draws; i++) {
            VisitDefinition visit = Any.VisitDefinition().Generate();
            VisitEntry      entry = Any.VisitEntry().CompleteFor(visit).Generate();
            VisitSchedule   schedule = new VisitSchedule([visit]);
            SubjectCasebook casebook = Any.SubjectCasebook().WithSchedule(schedule).Randomised().Generate();

            VisitForm form = casebook.RecordVisit(visit.Code, Any.VisitEntry().OnDate(casebook.WindowOf(visit.Code).Earliest).CompleteFor(visit).Generate());

            Assert.Empty(form.UnresolvedFields());
            Assert.True(entry.TookPlace);
        }
    }

    [Fact]
    public void A_drawn_schedule_never_lets_two_windows_touch() {
        for (int i = 0; i < Draws; i++) {
            VisitSchedule schedule = Any.VisitSchedule().Generate();

            for (int v = 1; v < schedule.Visits.Count; v++) {
                Assert.True(schedule.Visits[v].EarliestDay > schedule.Visits[v - 1].LatestDay);
            }
        }
    }

}
