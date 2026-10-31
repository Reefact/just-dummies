// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Measure --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace MusicScore.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.Measure()</c> off the library's own entry point.</summary>
public static class AnyMeasureEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Measure</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyMeasure Measure() {
            return new AnyMeasure();
        }

    }

}
