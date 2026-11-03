// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Verdict --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.Verdict()</c> off the library's own entry point.</summary>
public static class AnyVerdictEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Verdict</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyVerdict Verdict() {
            return new AnyVerdict();
        }

    }

}
