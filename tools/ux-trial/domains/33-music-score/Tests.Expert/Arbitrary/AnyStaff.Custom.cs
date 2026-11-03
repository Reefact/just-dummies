using JustDummies;

namespace MusicScore.Domain;

public sealed partial class AnyStaff {

    /// <summary>Measures numbered 1..n, n drawn by <c>count</c>, each in a meter of its own and without repeat barlines.</summary>
    private sealed class NumberedMeasures : IAny<IReadOnlyList<Measure>> {

        private readonly IAny<int> _count;

        public NumberedMeasures(IAny<int> count) {
            _count = count;
        }

        public IReadOnlyList<Measure> Generate() {
            int count = _count.Generate();

            return Enumerable.Range(1, count).Select(number => new AnyMeasure().WithNumber(number).Generate()).ToList();
        }

    }

}
