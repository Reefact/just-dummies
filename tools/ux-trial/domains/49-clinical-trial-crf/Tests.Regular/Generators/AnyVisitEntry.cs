using System;
using System.Collections.Generic;
using System.Linq;

using ClinicalTrial.Domain;
using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="VisitEntry" />. Scaffolded by `any`, then completed by hand: a visit
///     that did not take place carries no data, and <see cref="Generate" /> enforces it. A refusal is tested with
///     the constructor, not here.
/// </summary>
public sealed partial class AnyVisitEntry : IAny<VisitEntry> {

    private IAny<Observed<DateOnly>> _visitDate;
    private IAny<VitalSigns?>        _vitals;
    private IAny<List<LabResult>>    _labs;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVisitEntry() {
        _visitDate = Any.Int32().Between(0, 9).Then(k => k < 8
                                                           ? TrialFields.Date().As(Observed<DateOnly>.Of)
                                                           : Any.Enum<MissingReason>().DifferentFrom(MissingReason.NotApplicable).As(Observed<DateOnly>.Missing));
        _vitals    = new AnyVitalSigns().OrNull();
        _labs      = Any.ListOf(new AnyLabResult()).WithCountBetween(0, 4)
                        .As(labs => labs.GroupBy(l => l.AnalyteCode).Select(g => g.First()).ToList());
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVisitEntry(AnyVisitEntry from) {
        _visitDate = from._visitDate;
        _vitals    = from._vitals;
        _labs      = from._labs;
    }

    public AnyVisitEntry WithVisitDate(Observed<DateOnly> value) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._visitDate = Any.Fixed(value);

        return copy;
    }

    public AnyVisitEntry WithVisitDate(DateOnly value) {
        return WithVisitDate(Observed<DateOnly>.Of(value));
    }

    public AnyVisitEntry WithVitals(VitalSigns? value) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._vitals = Any.Fixed(value);

        return copy;
    }

    public AnyVisitEntry WithLabs(params LabResult[] value) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._labs = Any.Fixed(value.ToList());

        return copy;
    }

    /// <summary>The visit took place, so vitals and labs are drawn; the vitals are always there.</summary>
    public AnyVisitEntry TookPlace() {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._visitDate = TrialFields.Date().As(Observed<DateOnly>.Of);
        copy._vitals    = new AnyVitalSigns();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VisitEntry" />.</summary>
    public VisitEntry Generate() {
        Observed<DateOnly> visitDate = _visitDate.Generate();
        VitalSigns?        vitals    = _vitals.Generate();
        List<LabResult>    labs      = _labs.Generate();

        return visitDate.IsObserved ? new VisitEntry(visitDate, vitals, labs)
                                    : new VisitEntry(visitDate, null, new List<LabResult>());
    }

}
