using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     The bases several generators share, written once. Each one is a rule of the domain that the
///     scaffolder could not read, because the guard lives in the internal <c>Guard</c> class or in
///     a method of another type.
/// </summary>
internal static class Plausible {

    /// <summary>
    ///     An amount a person could earn or owe. <c>Money</c> alone spans [0, 999 999 999 999.99]; thirty
    ///     such incomes add up past what <c>Money</c> holds, so a taxpayer's income would no longer be
    ///     computable. A ceiling of ten million keeps every sum the domain makes representable.
    /// </summary>
    public static AnyMoney Amount() {
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(10_000_000m));
    }

    /// <summary>A label: not blank, at most 80 characters (<c>Guard.Label</c>, internal to the domain).</summary>
    public static AnyString Label() {
        return Any.String().NotBlank().WithMaxLength(80);
    }

    /// <summary>A tax base: one or several known income kinds, never <c>None</c> (<c>Guard.IncomeKinds</c>).</summary>
    public static AnyEnum<IncomeKind> TaxBase() {
        return Any.Enum<IncomeKind>().AllowingCombinations().DifferentFrom(IncomeKind.None);
    }

}
