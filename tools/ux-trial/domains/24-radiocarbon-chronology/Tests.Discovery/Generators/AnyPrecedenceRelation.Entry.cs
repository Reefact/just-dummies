// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PrecedenceRelation --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>Hangs <c>Any.PrecedenceRelation()</c> off the library's own entry point.</summary>
public static class AnyPrecedenceRelationEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>PrecedenceRelation</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyPrecedenceRelation PrecedenceRelation() {
            return new AnyPrecedenceRelation();
        }

    }

}
