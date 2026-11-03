using JustDummies;

namespace MusicScore.Domain;

public sealed partial class AnyVoice {

    /// <summary>Entries (notes and rests, never tied) whose durations add up to exactly <paramref name="length" />.</summary>
    public AnyVoice Filling(Duration length) {
        AnyVoice copy = new AnyVoice(this);

        copy._entries = new Tiling(length);

        return copy;
    }

    /// <summary>
    ///     Cuts a length into 1 to 6 pieces on a grid of 1, 2 or 4 subdivisions of its own denominator, so the
    ///     pieces add up to it by construction.
    /// </summary>
    private sealed class Tiling : IAny<IReadOnlyList<VoiceEntry>> {

        private readonly Duration _length;

        public Tiling(Duration length) {
            _length = length;
        }

        public IReadOnlyList<VoiceEntry> Generate() {
            long subdivision = Any.OneOf(1L, 2L, 4L).Generate();
            long units       = _length.Numerator * subdivision;
            long denominator = _length.Denominator * subdivision;

            List<long> cuts = new() { 0, units };
            if (units > 1) {
                long maxCuts = Math.Min(units - 1, 5);
                foreach (int cut in Any.SetOf(Any.Int32().Between(1, (int)units - 1)).WithCountBetween(0, (int)maxCuts).Generate()) {
                    cuts.Add(cut);
                }
            }

            cuts.Sort();

            List<VoiceEntry> entries = new();
            for (int index = 1; index < cuts.Count; index++) {
                AnyVoiceEntry entry = new AnyVoiceEntry().WithDuration(new Duration(cuts[index] - cuts[index - 1], denominator))
                                                         .WithTiedToNext(false);
                bool isRest = Any.Int32().Between(0, 3).Generate() == 0;

                entries.Add((isRest ? entry.Rest() : entry).Generate());
            }

            return entries;
        }

    }

}
