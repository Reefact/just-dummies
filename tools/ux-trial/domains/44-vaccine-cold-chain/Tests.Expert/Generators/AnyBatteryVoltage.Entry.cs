// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BatteryVoltage --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.BatteryVoltage()</c> off the library's own entry point.</summary>
public static class AnyBatteryVoltageEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>BatteryVoltage</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyBatteryVoltage BatteryVoltage() {
            return new AnyBatteryVoltage();
        }

    }

}
