// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Query --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>Hangs <c>Any.Query()</c> off the library's own entry point.</summary>
public static class AnyQueryEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Query</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyQuery Query() {
            return new AnyQuery();
        }

    }

}
