// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LoggerReading --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.LoggerReading()</c> off the library's own entry point.</summary>
public static class AnyLoggerReadingEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>LoggerReading</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyLoggerReading LoggerReading() {
            return new AnyLoggerReading();
        }

    }

}
