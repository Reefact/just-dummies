using System;
using System.Collections.Generic;

using JustDummies;

namespace MusicScore.Domain;

// Written by hand, beside the scaffolded file: what a voice must satisfy to sit in a measure.
public sealed partial class AnyVoice {

    /// <summary>A voice of the given number whose entries fill exactly the length of <paramref name="timeSignature" />.</summary>
    public static Voice FillingMeasure(int number, TimeSignature timeSignature) {
        List<VoiceEntry> entries   = new List<VoiceEntry>();
        int              remaining = timeSignature.Beats;
        while (remaining > 0) {
            int      beats    = Any.Int32().Between(1, remaining).Generate();
            Duration duration = new Duration(beats, timeSignature.BeatValue);

            entries.Add(new AnyVoiceEntry().WithDuration(duration).WithTiedToNext(false).Generate());
            remaining -= beats;
        }

        return new Voice(number, entries);
    }

}
