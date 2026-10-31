// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeSignature --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace MusicScore.Domain;

/// <summary>Hangs <c>Any.TimeSignature()</c> off the library's own entry point.</summary>
public static class AnyTimeSignatureEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>TimeSignature</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTimeSignature TimeSignature() {
            return new AnyTimeSignature();
        }

    }

}
