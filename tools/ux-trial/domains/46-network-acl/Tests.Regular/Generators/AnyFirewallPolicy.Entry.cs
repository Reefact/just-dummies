// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FirewallPolicy --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>Hangs <c>Any.FirewallPolicy()</c> off the library's own entry point.</summary>
public static class AnyFirewallPolicyEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>FirewallPolicy</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyFirewallPolicy FirewallPolicy() {
            return new AnyFirewallPolicy();
        }

    }

}
