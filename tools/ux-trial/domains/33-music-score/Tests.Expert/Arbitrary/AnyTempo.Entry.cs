// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tempo --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace MusicScore.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.Tempo()</c> off the library's own entry point.</summary>
public static class AnyTempoEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Tempo</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTempo Tempo() {
            return new AnyTempo();
        }

    }

}
