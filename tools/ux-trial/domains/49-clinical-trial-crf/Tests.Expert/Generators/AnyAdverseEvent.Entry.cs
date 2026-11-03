// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AdverseEvent --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>Hangs <c>Any.AdverseEvent()</c> off the library's own entry point.</summary>
public static class AnyAdverseEventEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>AdverseEvent</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyAdverseEvent AdverseEvent() {
            return new AnyAdverseEvent();
        }

    }

}
