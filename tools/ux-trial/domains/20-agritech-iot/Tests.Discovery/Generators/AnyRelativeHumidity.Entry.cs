// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RelativeHumidity --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace AgriTech.Domain;

/// <summary>Hangs <c>Any.RelativeHumidity()</c> off the library's own entry point.</summary>
public static class AnyRelativeHumidityEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>RelativeHumidity</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyRelativeHumidity RelativeHumidity() {
            return new AnyRelativeHumidity();
        }

    }

}
