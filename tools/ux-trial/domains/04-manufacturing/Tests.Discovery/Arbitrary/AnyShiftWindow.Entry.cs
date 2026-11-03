// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShiftWindow --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Manufacturing.Tests.Discovery.Arbitrary;

/// <summary>Hangs <c>Any.ShiftWindow()</c> off the library's own entry point.</summary>
public static class AnyShiftWindowEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>ShiftWindow</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyShiftWindow ShiftWindow() {
            return new AnyShiftWindow();
        }

    }

}
