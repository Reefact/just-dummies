// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DailyLoadCurve --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>Hangs <c>Any.DailyLoadCurve()</c> off the library's own entry point.</summary>
public static class AnyDailyLoadCurveEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>DailyLoadCurve</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyDailyLoadCurve DailyLoadCurve() {
            return new AnyDailyLoadCurve();
        }

    }

}
