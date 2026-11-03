// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate WorkingHours --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ShiftPlanning.Domain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.WorkingHours()</c> off the library's own entry point.</summary>
public static class AnyWorkingHoursEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>WorkingHours</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyWorkingHours WorkingHours() {
            return new AnyWorkingHours();
        }

    }

}
