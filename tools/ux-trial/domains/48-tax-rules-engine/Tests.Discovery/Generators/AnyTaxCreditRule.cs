// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TaxCreditRule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TaxCreditRule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTaxCreditRule : IAny<TaxCreditRule> {

    private IAny<RuleCode>    _code;
    private IAny<string>      _label;
    private IAny<Money>       _baseAmount;
    private IAny<Money>       _perAdditionalMember;
    private IAny<ICondition?> _condition;

    private AnyRuleCode _codeRecipe;
    private AnyString   _labelRecipe;
    private AnyMoney    _baseAmountRecipe;
    private AnyMoney    _perAdditionalMemberRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTaxCreditRule()
        : this(code:                AnyValidCode(),
               label:               AnyValidLabel(),
               baseAmount:          AnyValidBaseAmount(),
               perAdditionalMember: AnyValidPerAdditionalMember(),
               condition:           AnyValidCondition()) { }

    private static AnyRuleCode AnyValidCode() {
        return new AnyRuleCode();
    }

    private static AnyString AnyValidLabel() {
        return AnyLabel.Create();
    }

    private static AnyMoney AnyValidBaseAmount() {
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(10_000m));
    }

    private static AnyMoney AnyValidPerAdditionalMember() {
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(1_000m));
    }

    private static IAny<ICondition?> AnyValidCondition() {
        // A rule under test applies unless the test says otherwise: no condition, rather than a random one that would fail it.
        return Any.Fixed<ICondition?>(null);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTaxCreditRule(AnyRuleCode       code,
                             AnyString         label,
                             AnyMoney          baseAmount,
                             AnyMoney          perAdditionalMember,
                             IAny<ICondition?> condition) {
        _code                      = code;
        _codeRecipe                = code;
        _label                     = label;
        _labelRecipe               = label;
        _baseAmount                = baseAmount;
        _baseAmountRecipe          = baseAmount;
        _perAdditionalMember       = perAdditionalMember;
        _perAdditionalMemberRecipe = perAdditionalMember;
        _condition                 = condition;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTaxCreditRule(AnyTaxCreditRule from) {
        _code                      = from._code;
        _codeRecipe                = from._codeRecipe;
        _label                     = from._label;
        _labelRecipe               = from._labelRecipe;
        _baseAmount                = from._baseAmount;
        _baseAmountRecipe          = from._baseAmountRecipe;
        _perAdditionalMember       = from._perAdditionalMember;
        _perAdditionalMemberRecipe = from._perAdditionalMemberRecipe;
        _condition                 = from._condition;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxCreditRule WithCode(RuleCode value) {
        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRuleCode, AnyRuleCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxCreditRule WithCode(Func<AnyRuleCode, AnyRuleCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>label</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxCreditRule WithLabel(string value) {
        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._label = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>label</c> is drawn from — <c>AnyValidLabel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxCreditRule WithLabel(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._labelRecipe = configure(_labelRecipe);
        copy._label       = copy._labelRecipe;

        return copy;
    }

    /// <summary>Pins <c>baseAmount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxCreditRule WithBaseAmount(Money value) {
        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._baseAmount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>baseAmount</c> is drawn from — <c>AnyValidBaseAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxCreditRule WithBaseAmount(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._baseAmountRecipe = configure(_baseAmountRecipe);
        copy._baseAmount       = copy._baseAmountRecipe;

        return copy;
    }

    /// <summary>Pins <c>perAdditionalMember</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxCreditRule WithPerAdditionalMember(Money value) {
        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._perAdditionalMember = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>perAdditionalMember</c> is drawn from — <c>AnyValidPerAdditionalMember()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxCreditRule WithPerAdditionalMember(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._perAdditionalMemberRecipe = configure(_perAdditionalMemberRecipe);
        copy._perAdditionalMember       = copy._perAdditionalMemberRecipe;

        return copy;
    }

    /// <summary>Pins <c>condition</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyTaxCreditRule WithCondition(ICondition? value) {
        AnyTaxCreditRule copy = new AnyTaxCreditRule(this);

        copy._condition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TaxCreditRule" />.</summary>
    public TaxCreditRule Generate() {
        Money baseAmount          = _baseAmount.Generate();
        Money perAdditionalMember = _perAdditionalMember.Generate();

        // A credit grants something: when both parts are zero, the base is drawn again above zero.
        if (baseAmount.IsZero && perAdditionalMember.IsZero) {
            baseAmount = new Money(Any.Decimal().Between(0.01m, 10_000m).WithScale(2).Generate());
        }

        return new TaxCreditRule(_code.Generate(),
                                 _label.Generate(),
                                 baseAmount,
                                 perAdditionalMember,
                                 _condition.Generate());
    }

}
