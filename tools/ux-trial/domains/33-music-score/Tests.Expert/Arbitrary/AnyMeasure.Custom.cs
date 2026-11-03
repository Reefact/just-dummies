using JustDummies;

namespace MusicScore.Domain;

public sealed partial class AnyMeasure {

    private static IAny<IReadOnlyList<Voice>> AnyVoicesFilling(TimeSignature timeSignature) {
        return new VoicesFilling(timeSignature.MeasureLength);
    }

    /// <summary>One to three voices with distinct numbers, each filling the meter's length exactly.</summary>
    private sealed class VoicesFilling : IAny<IReadOnlyList<Voice>> {

        private readonly Duration _length;

        public VoicesFilling(Duration length) {
            _length = length;
        }

        public IReadOnlyList<Voice> Generate() {
            HashSet<int> numbers = Any.SetOf(Any.Int32().Between(1, Voice.MaxVoices)).WithCountBetween(1, 3).Generate();

            return numbers.OrderBy(number => number)
                          .Select(number => new AnyVoice().WithNumber(number).Filling(_length).Generate())
                          .ToList();
        }

    }

}
