// Started from `any generate VisitSchedule`, then rewritten by hand: its single parameter, `visits`, was a sequence of
// anything, while the schedule demands unique codes, increasing targets and windows that neither overlap nor touch.
// Here the visits are drawn one after the other, each opening after the previous one closed.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary <see cref="VisitSchedule" /> values.</summary>
public sealed partial class AnyVisitSchedule : IAny<VisitSchedule> {

    private IAny<int>                      _count;
    private IEnumerable<VisitDefinition>?  _pinned;
    private bool                           _withoutAnalytes;

    public AnyVisitSchedule() {
        _count           = Any.Int32().Between(1, 6);
        _pinned          = null;
        _withoutAnalytes = false;
    }

    private AnyVisitSchedule(AnyVisitSchedule from) {
        _count           = from._count;
        _pinned          = from._pinned;
        _withoutAnalytes = from._withoutAnalytes;
    }

    public AnyVisitSchedule WithVisitCount(int value) {
        AnyVisitSchedule copy = new AnyVisitSchedule(this);

        copy._count = Any.Fixed(value);

        return copy;
    }

    public AnyVisitSchedule WithVisits(params VisitDefinition[] values) {
        AnyVisitSchedule copy = new AnyVisitSchedule(this);

        copy._pinned = values;

        return copy;
    }

    /// <summary>No visit of the schedule requires an analyte: the labs of an entry are then up to the test.</summary>
    public AnyVisitSchedule WithoutRequiredAnalytes() {
        AnyVisitSchedule copy = new AnyVisitSchedule(this);

        copy._withoutAnalytes = true;

        return copy;
    }

    public VisitSchedule Generate() {
        if (_pinned is not null) {
            return new VisitSchedule(_pinned);
        }

        int                  count     = _count.Generate();
        List<string>         codes     = Any.SetOf(AnyVisitCodes.Code()).WithCount(count).Generate().ToList();
        List<VisitDefinition> visits    = new List<VisitDefinition>();
        int                  lastClose = 0;

        foreach (string code in codes) {
            int daysBefore = Any.Int32().Between(0, VisitDefinition.MaxToleranceDays).Generate();
            int gap        = Any.Int32().Between(0, 60).Generate();
            int targetDay  = lastClose + 1 + daysBefore + gap;

            AnyVisitDefinition visit = new AnyVisitDefinition().WithCode(code).WithTargetDay(targetDay).WithDaysBefore(daysBefore);
            if (_withoutAnalytes) { visit = visit.WithoutRequiredAnalytes(); }

            VisitDefinition drawn = visit.Generate();
            visits.Add(drawn);
            lastClose = drawn.LatestDay;
        }

        return new VisitSchedule(visits);
    }

}
