using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace MusicScore.Domain;

// Written by hand, beside the scaffolded file: how a measure's voices are drawn so that each one fills the meter.
public sealed partial class AnyMeasure {

    // Every meter's length is a whole number of 1/64 of a whole note (beat value is a power of two up to 64).
    private const int Resolution = 64;

    private static IReadOnlyList<Voice> DrawVoicesFilling(TimeSignature timeSignature) {
        int voiceCount = Any.Int32().Between(1, 3).Generate();

        List<int> numbers = Any.ListOf(Any.Int32().Between(1, Voice.MaxVoices)).WithCount(voiceCount).Distinct().Generate();

        return numbers.Select(number => new Voice(number, DrawEntriesFilling(timeSignature))).ToArray();
    }

    private static IReadOnlyList<VoiceEntry> DrawEntriesFilling(TimeSignature timeSignature) {
        int units = timeSignature.Beats * Resolution / timeSignature.BeatValue;

        int entryCount = Any.Int32().Between(1, System.Math.Min(units, 6)).Generate();

        List<int> cuts = entryCount == 1
            ? new List<int>()
            : Any.SetOf(Any.Int32().Between(1, units - 1)).WithCount(entryCount - 1).Generate().OrderBy(cut => cut).ToList();

        List<VoiceEntry> entries = new List<VoiceEntry>();
        int              previous = 0;
        foreach (int cut in cuts.Append(units)) {
            entries.Add(new AnyVoiceEntry().WithDuration(new Duration(cut - previous, Resolution)).Generate());
            previous = cut;
        }

        return entries;
    }

}
