// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate KinshipPolicy --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>Hangs <c>Any.KinshipPolicy()</c> off the library's own entry point.</summary>
public static class AnyKinshipPolicyEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>KinshipPolicy</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyKinshipPolicy KinshipPolicy() {
            return new AnyKinshipPolicy();
        }

    }

}
