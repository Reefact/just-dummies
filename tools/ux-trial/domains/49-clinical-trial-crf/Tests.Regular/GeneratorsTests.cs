using System;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

/// <summary>
///     The hand-completed generators are the foundation of every other test: a draw they cannot build would
///     fail those tests for a reason that has nothing to do with the domain. This checks them on a few hundred
///     draws each — it checks the generators, not the domain.
/// </summary>
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Every_generator_builds_the_values_the_domain_accepts() {
        for (int i = 0; i < Draws; i++) {
            Assert.NotNull(Any.ScreeningNumber().Generate());
            Assert.NotNull(Any.Randomisation().Generate());
            Assert.NotNull(Any.VisitDefinition().Generate());
            Assert.NotNull(Any.VisitSchedule().Generate());
            Assert.NotNull(Any.VitalSigns().Generate());
            Assert.NotNull(Any.LabResult().Generate());
            Assert.NotNull(Any.VisitEntry().Generate());
            Assert.NotNull(Any.AdverseEvent().Generate());
            Assert.NotNull(Any.Query().Generate());
            Assert.NotNull(Any.SubjectCasebook().Generate());
        }
    }

    [Fact]
    public void The_draws_reach_every_state_of_the_fields_that_can_be_blank_or_missing() {
        bool sawBlank    = false;
        bool sawMissing  = false;
        bool sawObserved = false;
        for (int i = 0; i < Draws; i++) {
            Observed<decimal>? result = Any.LabResult().Generate().Result;
            sawBlank    |= result is null;
            sawMissing  |= result is { IsObserved: false };
            sawObserved |= result is { IsObserved: true };
        }

        Assert.True(sawBlank && sawMissing && sawObserved);
    }

}
