// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RuleId --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>Hangs <c>Any.RuleId()</c> off the library's own entry point.</summary>
public static class AnyRuleIdEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>RuleId</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyRuleId RuleId() {
            return new AnyRuleId();
        }

    }

}
