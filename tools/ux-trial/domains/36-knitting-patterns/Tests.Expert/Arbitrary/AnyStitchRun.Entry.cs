// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StitchRun --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>Hangs <c>Any.StitchRun()</c> off the library's own entry point.</summary>
public static class AnyStitchRunEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>StitchRun</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyStitchRun StitchRun() {
            return new AnyStitchRun();
        }

    }

}
