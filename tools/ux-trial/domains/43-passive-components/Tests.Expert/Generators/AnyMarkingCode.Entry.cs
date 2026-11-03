// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MarkingCode --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.MarkingCode()</c> off the library's own entry point.</summary>
public static class AnyMarkingCodeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>MarkingCode</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyMarkingCode MarkingCode() {
            return new AnyMarkingCode();
        }

    }

}
