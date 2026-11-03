// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SampleId --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Laboratory.Domain;

/// <summary>Hangs <c>Any.SampleId()</c> off the library's own entry point.</summary>
public static class AnySampleIdEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>SampleId</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnySampleId SampleId() {
            return new AnySampleId();
        }

    }

}
