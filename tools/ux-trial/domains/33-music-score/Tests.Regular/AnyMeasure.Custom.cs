// Written by hand, beside the scaffolded AnyMeasure.cs: survives `any generate Measure --force`.

using JustDummies;

namespace MusicScore.Domain;

public sealed partial class AnyMeasure {

    /// <summary>A measure carrying no repeat barline, the plain case most scores are made of.</summary>
    public AnyMeasure WithoutRepeats() {
        return WithRepeatStart(false).WithRepeatEndTimes(0);
    }

    /// <summary>One voice, numbered 1, made of the given entries.</summary>
    public AnyMeasure WithSingleVoice(params VoiceEntry[] entries) {
        return WithVoices([new Voice(1, entries)]);
    }

}
