// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GasSchedule --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>Hangs <c>Any.GasSchedule()</c> off the library's own entry point.</summary>
public static class AnyGasScheduleEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>GasSchedule</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyGasSchedule GasSchedule() {
            return new AnyGasSchedule();
        }

    }

}
