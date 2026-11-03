using System;
using System.Collections.Generic;
using System.Linq;

using ClinicalTrial.Domain;
using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="VisitSchedule" />. Scaffolded by `any`, then completed by hand: the
///     visits are built one after the other, each window opening after the previous one has closed, so the
///     schedule holds by construction. Pin the whole list with <see cref="WithVisits" /> when the test is about it.
/// </summary>
public sealed partial class AnyVisitSchedule : IAny<VisitSchedule> {

    private IAny<IEnumerable<VisitDefinition>> _visits;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVisitSchedule()
        : this(visits: AnyValidVisits()) { }

    private static IAny<IEnumerable<VisitDefinition>> AnyValidVisits() {
        return Any.Int32().Between(1, 6)
                  .Then(count => Any.ListOf(TrialFields.VisitCode()).Distinct().WithCount(count)
                                    .Then(codes => Any.ListOf(Any.Combine(Any.Int32().Between(1, 15),
                                                                          Any.Int32().Between(0, 5),
                                                                          Any.Int32().Between(0, 5),
                                                                          Any.ListOf(TrialFields.AnalyteCode()).Distinct().WithCountBetween(0, 3),
                                                                          (gap, before, after, analytes) => (Gap: gap, Before: before, After: after, Analytes: analytes)))
                                                      .WithCount(count)
                                                      .As(specs => Build(codes, specs))));
    }

    private static IEnumerable<VisitDefinition> Build(List<string> codes, List<(int Gap, int Before, int After, List<string> Analytes)> specs) {
        List<VisitDefinition> visits    = new List<VisitDefinition>();
        int                   lastClose = 0;
        for (int i = 0; i < codes.Count; i++) {
            int opens  = lastClose + specs[i].Gap;
            int target = opens + specs[i].Before;
            visits.Add(new VisitDefinition(codes[i], target, specs[i].Before, specs[i].After, specs[i].Analytes));
            lastClose = target + specs[i].After;
        }

        return visits;
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVisitSchedule(IAny<IEnumerable<VisitDefinition>> visits) {
        _visits = visits;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVisitSchedule(AnyVisitSchedule from) {
        _visits = from._visits;
    }

    public AnyVisitSchedule WithVisits(params VisitDefinition[] value) {
        AnyVisitSchedule copy = new AnyVisitSchedule(this);

        copy._visits = Any.Fixed<IEnumerable<VisitDefinition>>(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VisitSchedule" />.</summary>
    public VisitSchedule Generate() {
        return new VisitSchedule(_visits.Generate());
    }

}
