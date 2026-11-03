using JustDummies;

namespace MusicScore.Domain;

public sealed partial class AnyScore {

    /// <summary>
    ///     One to three staves with distinct names, the same number of measures, the same meter in each
    ///     measure and no repeat barline: what a score requires of its staves to be a score.
    /// </summary>
    private sealed class CoherentStaves : IAny<IReadOnlyList<Staff>> {

        public IReadOnlyList<Staff> Generate() {
            int           measureCount = Any.Int32().Between(1, 5).Generate();
            TimeSignature[] meters     = Enumerable.Range(0, measureCount).Select(_ => new AnyTimeSignature().Generate()).ToArray();
            List<string>  names        = Any.ListOf(Any.String().Alpha().WithLengthBetween(3, Staff.MaxNameLength)).WithCountBetween(1, 3).Distinct().Generate();

            return names.Select(name => new AnyStaff().WithName(name)
                                                      .WithMeasures(Enumerable.Range(1, measureCount)
                                                                              .Select(number => new AnyMeasure().WithNumber(number).WithTimeSignature(meters[number - 1]).Generate())
                                                                              .ToList())
                                                      .Generate())
                        .ToList();
        }

    }

}
