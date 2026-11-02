// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Plan --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Telecom.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.Plan()</c> off the library's own entry point.</summary>
public static class AnyPlanEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Plan</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyPlan Plan() {
            return new AnyPlan();
        }

    }

}
