// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShiftRequest --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ShiftPlanning.Domain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.ShiftRequest()</c> off the library's own entry point.</summary>
public static class AnyShiftRequestEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>ShiftRequest</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyShiftRequest ShiftRequest() {
            return new AnyShiftRequest();
        }

    }

}
