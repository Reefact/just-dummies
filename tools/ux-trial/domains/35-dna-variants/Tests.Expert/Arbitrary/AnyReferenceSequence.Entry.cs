// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReferenceSequence --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace DnaVariants.Domain;

/// <summary>Hangs <c>Any.ReferenceSequence()</c> off the library's own entry point.</summary>
public static class AnyReferenceSequenceEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>ReferenceSequence</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyReferenceSequence ReferenceSequence() {
            return new AnyReferenceSequence();
        }

    }

}
