// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate InvestmentIncome --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="InvestmentIncome" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyInvestmentIncome : IAny<InvestmentIncome> {

    private IAny<string>         _payer;
    private IAny<Money>          _gross;
    private IAny<InvestmentKind> _investmentKind;

    private AnyString               _payerRecipe;
    private AnyMoney                _grossRecipe;
    private AnyEnum<InvestmentKind> _investmentKindRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyInvestmentIncome()
        : this(payer:          AnyValidPayer(),
               gross:          AnyValidGross(),
               investmentKind: AnyValidInvestmentKind()) { }

    private static AnyString AnyValidPayer() {
        return Any.String().NotBlank().WithMaxLength(IncomeComponent.MaxSourceLength);
    }

    private static AnyMoney AnyValidGross() {
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(1_000_000m));
    }

    private static AnyEnum<InvestmentKind> AnyValidInvestmentKind() {
        return Any.Enum<InvestmentKind>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyInvestmentIncome(AnyString               payer,
                                AnyMoney                gross,
                                AnyEnum<InvestmentKind> investmentKind) {
        _payer                = payer;
        _payerRecipe          = payer;
        _gross                = gross;
        _grossRecipe          = gross;
        _investmentKind       = investmentKind;
        _investmentKindRecipe = investmentKind;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyInvestmentIncome(AnyInvestmentIncome from) {
        _payer                = from._payer;
        _payerRecipe          = from._payerRecipe;
        _gross                = from._gross;
        _grossRecipe          = from._grossRecipe;
        _investmentKind       = from._investmentKind;
        _investmentKindRecipe = from._investmentKindRecipe;
    }

    /// <summary>Pins <c>payer</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInvestmentIncome WithPayer(string value) {
        AnyInvestmentIncome copy = new AnyInvestmentIncome(this);

        copy._payer = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>payer</c> is drawn from — <c>AnyValidPayer()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInvestmentIncome WithPayer(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInvestmentIncome copy = new AnyInvestmentIncome(this);

        copy._payerRecipe = configure(_payerRecipe);
        copy._payer       = copy._payerRecipe;

        return copy;
    }

    /// <summary>Pins <c>gross</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInvestmentIncome WithGross(Money value) {
        AnyInvestmentIncome copy = new AnyInvestmentIncome(this);

        copy._gross = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gross</c> is drawn from — <c>AnyValidGross()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInvestmentIncome WithGross(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInvestmentIncome copy = new AnyInvestmentIncome(this);

        copy._grossRecipe = configure(_grossRecipe);
        copy._gross       = copy._grossRecipe;

        return copy;
    }

    /// <summary>Pins <c>investmentKind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInvestmentIncome WithInvestmentKind(InvestmentKind value) {
        AnyInvestmentIncome copy = new AnyInvestmentIncome(this);

        copy._investmentKind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>investmentKind</c> is drawn from — <c>AnyValidInvestmentKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;InvestmentKind&gt;, AnyEnum&lt;InvestmentKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInvestmentIncome WithInvestmentKind(Func<AnyEnum<InvestmentKind>, AnyEnum<InvestmentKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInvestmentIncome copy = new AnyInvestmentIncome(this);

        copy._investmentKindRecipe = configure(_investmentKindRecipe);
        copy._investmentKind       = copy._investmentKindRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="InvestmentIncome" />.</summary>
    public InvestmentIncome Generate() {
        return new InvestmentIncome(_payer.Generate(),
                                    _gross.Generate(),
                                    _investmentKind.Generate());
    }

}
