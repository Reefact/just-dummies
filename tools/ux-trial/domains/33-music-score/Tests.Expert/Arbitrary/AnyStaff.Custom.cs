// Written by hand, never touched by `any generate Staff --force`.

using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

public sealed partial class AnyStaff {

    /// <summary>
    ///     One arbitrary measure per shape, numbered 1..n in the order given. A shape states only
    ///     what the test needs of that measure: a repeat barline, a meter, its voices.
    /// </summary>
    public AnyStaff WithMeasures(params Func<AnyMeasure, AnyMeasure>[] shapes) {
        return new AnyStaff(_name, new Drawn<IReadOnlyList<Measure>>(() => MeasuresShapedBy(shapes)));
    }

    private static IReadOnlyList<Measure> MeasuresShapedBy(IEnumerable<Func<AnyMeasure, AnyMeasure>> shapes) {
        return shapes.Select((shape, index) => shape(new AnyMeasure()).WithNumber(index + 1).Generate()).ToArray();
    }

}
