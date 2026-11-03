// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VisitSchedule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VisitSchedule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVisitSchedule : IAny<VisitSchedule> {

    private IAny<IEnumerable<VisitDefinition>> _visits;

    private IAny<IEnumerable<VisitDefinition>> _visitsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVisitSchedule()
        : this(visits: AnyValidVisits()) { }

    // At least one visit; unique codes; strictly increasing target days; each window opens after the previous one closes.
    private static IAny<IEnumerable<VisitDefinition>> AnyValidVisits() {
        IAny<string> code    = Any.StringMatching("[A-Z][A-Z0-9]{0,7}");
        IAny<string> analyte = Any.StringMatching("[A-Z][A-Z0-9]{1,9}");

        return Any.Int32().Between(1, 6).Then(count => Any.Combine(
            Any.ListOf(code).WithCount(count).Distinct(),
            Any.ListOf(Any.Combine(Any.Int32().Between(1, 20),   // days between the previous window and this one
                                   Any.Int32().Between(0, 10),   // days before
                                   Any.Int32().Between(0, 10),   // days after
                                   Any.SetOf(analyte).WithMaxCount(4),
                                   (gap, before, after, analytes) => (gap, before, after, analytes))).WithCount(count),
            (codes, shapes) => {
                List<VisitDefinition> visits = new List<VisitDefinition>();
                int                   closes = 0;
                for (int i = 0; i < count; i++) {
                    (int gap, int before, int after, HashSet<string> analytes) = shapes[i];
                    int opens  = closes + gap;
                    int target = opens + before;
                    visits.Add(new VisitDefinition(codes[i], target, before, after, analytes));
                    closes = target + after;
                }

                return (IEnumerable<VisitDefinition>)visits;
            }));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVisitSchedule(IAny<IEnumerable<VisitDefinition>> visits) {
        _visits       = visits;
        _visitsRecipe = visits;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVisitSchedule(AnyVisitSchedule from) {
        _visits       = from._visits;
        _visitsRecipe = from._visitsRecipe;
    }

    /// <summary>Pins <c>visits</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVisitSchedule WithVisits(IEnumerable<VisitDefinition> value) {
        AnyVisitSchedule copy = new AnyVisitSchedule(this);

        copy._visits = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VisitSchedule" />.</summary>
    public VisitSchedule Generate() {
        return new VisitSchedule(_visits.Generate());
    }

}
