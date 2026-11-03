using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace MusicScore.Domain;

// Written by hand, beside the scaffolded file: the voices of a measure depend on its meter.
public sealed partial class AnyMeasure {

    private static IReadOnlyList<Voice> DrawVoicesFilling(TimeSignature timeSignature) {
        List<int> numbers = Any.SetOf(Any.Int32().Between(1, Voice.MaxVoices)).NonEmpty().Generate().ToList();

        return numbers.Select(number => AnyVoice.FillingMeasure(number, timeSignature)).ToArray();
    }

}
