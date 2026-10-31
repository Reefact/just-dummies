// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CellCoordinate --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.CellCoordinate()</c> off the library's own entry point.</summary>
public static class AnyCellCoordinateEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>CellCoordinate</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyCellCoordinate CellCoordinate() {
            return new AnyCellCoordinate();
        }

    }

}
