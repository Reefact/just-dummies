// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FlatRateRule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="FlatRateRule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFlatRateRule : IAny<FlatRateRule> {

    private IAny<RuleCode>    _code;
    private IAny<string>      _label;
    private IAny<IncomeKind>  _taxBase;
    private IAny<Rate>        _rate;
    private IAny<ICondition?> _condition;

    private AnyRuleCode         _codeRecipe;
    private AnyString           _labelRecipe;
    private AnyEnum<IncomeKind> _taxBaseRecipe;
    private AnyRate             _rateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFlatRateRule()
        : this(code:      AnyValidCode(),
               label:     AnyValidLabel(),
               taxBase:   AnyValidTaxBase(),
               rate:      AnyValidRate(),
               condition: AnyValidCondition()) { }

    private static AnyRuleCode AnyValidCode() {
        return new AnyRuleCode();
    }

    private static AnyString AnyValidLabel() {
        return AnyLabel.Create();
    }

    private static AnyEnum<IncomeKind> AnyValidTaxBase() {
        return Any.Enum<IncomeKind>().OneOf(IncomeKind.Employment, IncomeKind.SelfEmployment, IncomeKind.Investment,
                                            IncomeKind.All, IncomeKind.Employment | IncomeKind.Investment);
    }

    private static AnyRate AnyValidRate() {
        return new AnyRate().WithFraction(f => f.GreaterThan(0m));
    }

    private static IAny<ICondition?> AnyValidCondition() {
        // A rule under test applies unless the test says otherwise: no condition, rather than a random one that would fail it.
        return Any.Fixed<ICondition?>(null);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFlatRateRule(AnyRuleCode         code,
                            AnyString           label,
                            AnyEnum<IncomeKind> taxBase,
                            AnyRate             rate,
                            IAny<ICondition?>   condition) {
        _code          = code;
        _codeRecipe    = code;
        _label         = label;
        _labelRecipe   = label;
        _taxBase       = taxBase;
        _taxBaseRecipe = taxBase;
        _rate          = rate;
        _rateRecipe    = rate;
        _condition     = condition;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFlatRateRule(AnyFlatRateRule from) {
        _code          = from._code;
        _codeRecipe    = from._codeRecipe;
        _label         = from._label;
        _labelRecipe   = from._labelRecipe;
        _taxBase       = from._taxBase;
        _taxBaseRecipe = from._taxBaseRecipe;
        _rate          = from._rate;
        _rateRecipe    = from._rateRecipe;
        _condition     = from._condition;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlatRateRule WithCode(RuleCode value) {
        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRuleCode, AnyRuleCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlatRateRule WithCode(Func<AnyRuleCode, AnyRuleCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>label</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlatRateRule WithLabel(string value) {
        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._label = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>label</c> is drawn from — <c>AnyValidLabel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlatRateRule WithLabel(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._labelRecipe = configure(_labelRecipe);
        copy._label       = copy._labelRecipe;

        return copy;
    }

    /// <summary>Pins <c>taxBase</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlatRateRule WithTaxBase(IncomeKind value) {
        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._taxBase = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>taxBase</c> is drawn from — <c>AnyValidTaxBase()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;IncomeKind&gt;, AnyEnum&lt;IncomeKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlatRateRule WithTaxBase(Func<AnyEnum<IncomeKind>, AnyEnum<IncomeKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._taxBaseRecipe = configure(_taxBaseRecipe);
        copy._taxBase       = copy._taxBaseRecipe;

        return copy;
    }

    /// <summary>Pins <c>rate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlatRateRule WithRate(Rate value) {
        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._rate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rate</c> is drawn from — <c>AnyValidRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRate, AnyRate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlatRateRule WithRate(Func<AnyRate, AnyRate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._rateRecipe = configure(_rateRecipe);
        copy._rate       = copy._rateRecipe;

        return copy;
    }

    /// <summary>Pins <c>condition</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyFlatRateRule WithCondition(ICondition? value) {
        AnyFlatRateRule copy = new AnyFlatRateRule(this);

        copy._condition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="FlatRateRule" />.</summary>
    public FlatRateRule Generate() {
        return new FlatRateRule(_code.Generate(),
                                _label.Generate(),
                                _taxBase.Generate(),
                                _rate.Generate(),
                                _condition.Generate());
    }

}
