// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TraceEntry --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>Hangs <c>Any.TraceEntry()</c> off the library's own entry point.</summary>
public static class AnyTraceEntryEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>TraceEntry</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTraceEntry TraceEntry() {
            return new AnyTraceEntry();
        }

    }

}
