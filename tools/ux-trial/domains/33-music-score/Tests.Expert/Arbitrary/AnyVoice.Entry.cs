// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Voice --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace MusicScore.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.Voice()</c> off the library's own entry point.</summary>
// Renamed by hand: `any` named it AnyVoiceEntry, the name of the VoiceEntry generator (CS0260).
public static class AnyVoiceEntryPoint {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Voice</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyVoice Voice() {
            return new AnyVoice();
        }

    }

}
