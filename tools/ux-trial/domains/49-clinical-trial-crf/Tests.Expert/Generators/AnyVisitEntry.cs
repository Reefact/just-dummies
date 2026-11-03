// Started from `any generate VisitEntry`, then rewritten by hand: the visit date was a TODO (the tool cannot draw an
// Observed<DateOnly>) and the vitals and the labs answer to whether the visit took place. The base is a visit that
// took place; the visits that did not are named (NotDone, LostToFollowUp, Missing).

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary, valid <see cref="VisitEntry" /> values.</summary>
public sealed partial class AnyVisitEntry : IAny<VisitEntry> {

    private IAny<Observed<DateOnly>> _visitDate;
    private VitalSigns?              _vitals;
    private bool                     _vitalsPinned;
    private LabResult[]?             _labs;
    private VisitDefinition?         _completeFor;

    public AnyVisitEntry() {
        _visitDate = AnyObservedOf.OfValues(Any.DateOnly().Between(TrialCalendar.Earliest, TrialCalendar.Latest));
    }

    private AnyVisitEntry(AnyVisitEntry from) {
        _visitDate    = from._visitDate;
        _vitals       = from._vitals;
        _vitalsPinned = from._vitalsPinned;
        _labs         = from._labs;
        _completeFor  = from._completeFor;
    }

    public AnyVisitEntry OnDate(DateOnly value) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._visitDate = Any.Fixed(Observed<DateOnly>.Of(value));

        return copy;
    }

    /// <summary>The visit did not take place, for any reason a scheduled visit can be missed for.</summary>
    public AnyVisitEntry NotTakenPlace() {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._visitDate = AnyObservedOf.MissingExcept<DateOnly>(MissingReason.NotApplicable);

        return copy;
    }

    public AnyVisitEntry Missing(MissingReason reason) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._visitDate = Any.Fixed(Observed<DateOnly>.Missing(reason));

        return copy;
    }

    public AnyVisitEntry WithVitals(VitalSigns? value) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._vitals       = value;
        copy._vitalsPinned = true;

        return copy;
    }

    public AnyVisitEntry WithLabs(params LabResult[] values) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._labs = values;

        return copy;
    }

    /// <summary>Everything the visit asks of the site: fully resolved vital signs and every required analyte reported with a result.</summary>
    public AnyVisitEntry CompleteFor(VisitDefinition definition) {
        AnyVisitEntry copy = new AnyVisitEntry(this);

        copy._completeFor = definition;

        return copy;
    }

    public VisitEntry Generate() {
        Observed<DateOnly> visitDate = _visitDate.Generate();

        if (!visitDate.IsObserved) {
            return new VisitEntry(visitDate, null, Array.Empty<LabResult>());
        }

        VitalSigns? vitals = _vitalsPinned ? _vitals
                           : _completeFor is not null ? new AnyVitalSigns().Resolved().Generate()
                           : new AnyVitalSigns().Generate();

        IEnumerable<LabResult> labs;
        if (_labs is not null) {
            labs = _labs;
        } else if (_completeFor is not null) {
            labs = _completeFor.RequiredAnalytes.Select(code => new AnyLabResult().WithAnalyteCode(code).Resolved().Generate()).ToList();
        } else {
            labs = Any.SetOf(AnyAnalyteCodes.Code()).WithCountBetween(0, 3).Generate()
                      .Select(code => new AnyLabResult().WithAnalyteCode(code).Generate()).ToList();
        }

        return new VisitEntry(visitDate, vitals, labs);
    }

}
