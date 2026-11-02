// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Property --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace RealEstate.Domain;

/// <summary>Hangs <c>Any.Property()</c> off the library's own entry point.</summary>
public static class AnyPropertyEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Property</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyProperty Property() {
            return new AnyProperty();
        }

    }

}
