// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DataLogger --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.DataLogger()</c> off the library's own entry point.</summary>
public static class AnyDataLoggerEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>DataLogger</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyDataLogger DataLogger() {
            return new AnyDataLogger();
        }

    }

}
