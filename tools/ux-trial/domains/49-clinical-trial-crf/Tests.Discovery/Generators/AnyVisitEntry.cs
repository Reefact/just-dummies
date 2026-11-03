// Scaffolded by `any` (JustDummies), then completed by hand: a visit that did not take place carries nothing.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary, valid <see cref="VisitEntry" />.</summary>
public sealed partial class AnyVisitEntry : IAny<VisitEntry> {

    private IAny<Observed<DateOnly>> _visitDate;
    private IAny<VitalSigns?>        _vitals;
    private IAny<IEnumerable<LabResult>> _labs;

    public AnyVisitEntry() {
        _visitDate = Any.Boolean().Then(tookPlace => tookPlace
                                                         ? TrialCalendar.Date().As(Observed<DateOnly>.Of)
                                                         : Observations.ReasonVisitDidNotTakePlace().As(Observed<DateOnly>.Missing));
        _vitals    = new AnyVitalSigns().OrNull();
        _labs      = Any.ListOf(new AnyLabResult()).WithMaxCount(5).DistinctBy(lab => lab.AnalyteCode).As(labs => (IEnumerable<LabResult>)labs);
    }

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

    public AnyVisitEntry WithVitals(VitalSigns? value) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._vitals = Any.Fixed(value);

        return copy;
    }

    public AnyVisitEntry WithLabs(IEnumerable<LabResult> value) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._labs = Any.Fixed(value);

        return copy;
    }

    public VisitEntry Generate() {
        Observed<DateOnly> visitDate = _visitDate.Generate();
        VitalSigns?        vitals    = _vitals.Generate();
        List<LabResult>    labs      = _labs.Generate().ToList();

        // A visit that did not take place carries no vitals and no labs.
        return visitDate.IsObserved ? new VisitEntry(visitDate, vitals, labs) : new VisitEntry(visitDate, null, Array.Empty<LabResult>());
    }

}
