using System.Collections.Generic;
using System.Linq;

namespace MusicScore.Domain;

// Written by hand, beside the scaffolded file: a staff numbers its measures 1..n in order.
public sealed partial class AnyStaff {

    private static IReadOnlyList<Measure> Renumbered(IReadOnlyList<Measure> measures) {
        return measures.Select((measure, index) => new Measure(index + 1,
                                                                measure.TimeSignature,
                                                                measure.Dynamic,
                                                                measure.Voices,
                                                                measure.RepeatStart,
                                                                measure.RepeatEndTimes))
                       .ToArray();
    }

}
