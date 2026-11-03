// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Stake --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>Hangs <c>Any.Stake()</c> off the library's own entry point.</summary>
public static class AnyStakeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Stake</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyStake Stake() {
            return new AnyStake();
        }

    }

}
