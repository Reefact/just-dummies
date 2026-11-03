// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate InvestmentIncome --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace TaxRules.Domain;

/// <summary>Hangs <c>Any.InvestmentIncome()</c> off the library's own entry point.</summary>
public static class AnyInvestmentIncomeEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>InvestmentIncome</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyInvestmentIncome InvestmentIncome() {
            return new AnyInvestmentIncome();
        }

    }

}
