// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeBucket --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>Hangs <c>Any.TimeBucket()</c> off the library's own entry point.</summary>
public static class AnyTimeBucketEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>TimeBucket</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyTimeBucket TimeBucket() {
            return new AnyTimeBucket();
        }

    }

}
