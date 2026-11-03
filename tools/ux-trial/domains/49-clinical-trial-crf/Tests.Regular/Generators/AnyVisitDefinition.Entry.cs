// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VisitDefinition --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>Hangs <c>Any.VisitDefinition()</c> off the library's own entry point.</summary>
public static class AnyVisitDefinitionEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>VisitDefinition</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyVisitDefinition VisitDefinition() {
            return new AnyVisitDefinition();
        }

    }

}
