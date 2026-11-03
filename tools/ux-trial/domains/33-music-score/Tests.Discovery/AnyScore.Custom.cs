using System.Collections.Generic;

using JustDummies;

namespace MusicScore.Domain;

// Written by hand, beside the scaffolded file: the staves of a score share their measure count, their
// meters and their repeat barlines, and the repeats are neither nested nor left open.
public sealed partial class AnyScore {

    private static IReadOnlyList<Staff> DrawStavesSharingStructure() {
        int measureCount = Any.Int32().Between(1, 8).Generate();
        int staffCount   = Any.Int32().Between(1, 3).Generate();

        List<TimeSignature> meters  = new List<TimeSignature>();
        List<bool>          starts  = new List<bool>();
        List<int>           endings = new List<int>();
        bool                open    = false;
        for (int index = 0; index < measureCount; index++) {
            meters.Add(new AnyTimeSignature().Generate());

            bool start = !open && Any.Boolean().Generate();
            open = open || start;

            bool end = open && (index == measureCount - 1 || Any.Boolean().Generate());
            starts.Add(start);
            endings.Add(end ? Any.Int32().Between(2, Measure.MaxRepeatTimes).Generate() : 0);
            open = open && !end;
        }

        List<string> names = Any.ListOf(Any.String().Alpha().WithLengthBetween(1, Staff.MaxNameLength))
                                .WithCount(staffCount)
                                .Distinct()
                                .Generate();

        List<Staff> staves = new List<Staff>();
        foreach (string name in names) {
            List<Measure> measures = new List<Measure>();
            for (int index = 0; index < measureCount; index++) {
                measures.Add(new AnyMeasure().WithNumber(index + 1)
                                             .WithTimeSignature(meters[index])
                                             .WithRepeatStart(starts[index])
                                             .WithRepeatEndTimes(endings[index])
                                             .Generate());
            }

            staves.Add(new Staff(name, measures));
        }

        return staves;
    }

}
