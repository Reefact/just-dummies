using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace MusicScore.Domain;

// Written by hand, beside the scaffolded file: the staves of a score share their measure count, their
// meters and their repeat barlines, and never open a repeat inside a repeat.
public sealed partial class AnyScore {

    private sealed record Bar(TimeSignature TimeSignature, bool RepeatStart, int RepeatEndTimes);

    private static IReadOnlyList<Staff> DrawStavesSharingTheirStructure() {
        int              measureCount = Any.Int32().Between(1, 6).Generate();
        IReadOnlyList<Bar> bars         = DrawBars(measureCount);

        int          staffCount = Any.Int32().Between(1, 3).Generate();
        List<string> names      = Any.ListOf(AnyStaff.ValidName()).WithCount(staffCount).Distinct().Generate();

        return names.Select(name => new AnyStaff().WithName(name).WithMeasures(DrawMeasuresOn(bars)).Generate()).ToArray();
    }

    private static IReadOnlyList<Bar> DrawBars(int measureCount) {
        List<Bar> bars = new List<Bar>();
        bool      open = false;
        for (int index = 0; index < measureCount; index++) {
            bool isLast = index == measureCount - 1;
            bool start  = !open && Any.Boolean().Generate();
            open = open || start;

            int endTimes = 0;
            if (open && (isLast || Any.Boolean().Generate())) {
                endTimes = Any.Int32().Between(2, Measure.MaxRepeatTimes).Generate();
                open     = false;
            }

            bars.Add(new Bar(new AnyTimeSignature().Generate(), start, endTimes));
        }

        return bars;
    }

    private static IReadOnlyList<Measure> DrawMeasuresOn(IReadOnlyList<Bar> bars) {
        return bars.Select((bar, index) => new AnyMeasure().WithNumber(index + 1)
                                                           .WithTimeSignature(bar.TimeSignature)
                                                           .WithRepeatStart(bar.RepeatStart)
                                                           .WithRepeatEndTimes(bar.RepeatEndTimes)
                                                           .Generate()).ToArray();
    }

}
