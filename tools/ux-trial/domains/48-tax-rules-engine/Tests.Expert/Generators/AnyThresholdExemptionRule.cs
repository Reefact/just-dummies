// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ThresholdExemptionRule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ThresholdExemptionRule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyThresholdExemptionRule : IAny<ThresholdExemptionRule> {

    private IAny<RuleCode>    _code;
    private IAny<string>      _label;
    private IAny<IncomeKind>  _taxBase;
    private IAny<Money>       _threshold;
    private IAny<Rate>        _rate;
    private IAny<Money?>      _withdrawalCeiling;
    private bool              _ceilingPinned;
    private IAny<ICondition?> _condition;

    private AnyRuleCode         _codeRecipe;
    private AnyString           _labelRecipe;
    private AnyEnum<IncomeKind> _taxBaseRecipe;
    private AnyMoney            _thresholdRecipe;
    private AnyRate             _rateRecipe;
    private AnyMoney            _withdrawalCeilingRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyThresholdExemptionRule()
        : this(code:              AnyValidCode(),
               label:             AnyValidLabel(),
               taxBase:           AnyValidTaxBase(),
               threshold:         AnyValidThreshold(),
               rate:              AnyValidRate(),
               withdrawalCeiling: AnyValidWithdrawalCeiling(),
               condition:         AnyValidCondition()) { }

    private static AnyRuleCode AnyValidCode() {
        return new AnyRuleCode();
    }

    private static AnyString AnyValidLabel() {
        return Plausible.Label();
    }

    private static AnyEnum<IncomeKind> AnyValidTaxBase() {
        return Plausible.TaxBase();
    }

    private static AnyMoney AnyValidThreshold() {
        return Plausible.Amount().WithAmount(a => a.GreaterThan(0m));
    }

    private static AnyRate AnyValidRate() {
        return new AnyRate().WithFraction(r => r.GreaterThan(0m));
    }

    private static AnyMoney AnyValidWithdrawalCeiling() {
        return Plausible.Amount();
    }

    private static IAny<ICondition?> AnyValidCondition() {
        return new AnyICondition().OrNull();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyThresholdExemptionRule(AnyRuleCode         code,
                                      AnyString           label,
                                      AnyEnum<IncomeKind> taxBase,
                                      AnyMoney            threshold,
                                      AnyRate             rate,
                                      AnyMoney            withdrawalCeiling,
                                      IAny<ICondition?>   condition) {
        _code                    = code;
        _codeRecipe              = code;
        _label                   = label;
        _labelRecipe             = label;
        _taxBase                 = taxBase;
        _taxBaseRecipe           = taxBase;
        _threshold               = threshold;
        _thresholdRecipe         = threshold;
        _rate                    = rate;
        _rateRecipe              = rate;
        _withdrawalCeiling       = withdrawalCeiling.OrNull();
        _withdrawalCeilingRecipe = withdrawalCeiling;
        _condition               = condition;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyThresholdExemptionRule(AnyThresholdExemptionRule from) {
        _code                    = from._code;
        _codeRecipe              = from._codeRecipe;
        _label                   = from._label;
        _labelRecipe             = from._labelRecipe;
        _taxBase                 = from._taxBase;
        _taxBaseRecipe           = from._taxBaseRecipe;
        _threshold               = from._threshold;
        _thresholdRecipe         = from._thresholdRecipe;
        _rate                    = from._rate;
        _rateRecipe              = from._rateRecipe;
        _withdrawalCeiling       = from._withdrawalCeiling;
        _withdrawalCeilingRecipe = from._withdrawalCeilingRecipe;
        _ceilingPinned           = from._ceilingPinned;
        _condition               = from._condition;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyThresholdExemptionRule WithCode(RuleCode value) {
        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRuleCode, AnyRuleCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyThresholdExemptionRule WithCode(Func<AnyRuleCode, AnyRuleCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>label</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyThresholdExemptionRule WithLabel(string value) {
        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._label = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>label</c> is drawn from — <c>AnyValidLabel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyThresholdExemptionRule WithLabel(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._labelRecipe = configure(_labelRecipe);
        copy._label       = copy._labelRecipe;

        return copy;
    }

    /// <summary>Pins <c>taxBase</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyThresholdExemptionRule WithTaxBase(IncomeKind value) {
        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._taxBase = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>taxBase</c> is drawn from — <c>AnyValidTaxBase()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;IncomeKind&gt;, AnyEnum&lt;IncomeKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyThresholdExemptionRule WithTaxBase(Func<AnyEnum<IncomeKind>, AnyEnum<IncomeKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._taxBaseRecipe = configure(_taxBaseRecipe);
        copy._taxBase       = copy._taxBaseRecipe;

        return copy;
    }

    /// <summary>Pins <c>threshold</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyThresholdExemptionRule WithThreshold(Money value) {
        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._threshold = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>threshold</c> is drawn from — <c>AnyValidThreshold()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyThresholdExemptionRule WithThreshold(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._thresholdRecipe = configure(_thresholdRecipe);
        copy._threshold       = copy._thresholdRecipe;

        return copy;
    }

    /// <summary>Pins <c>rate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyThresholdExemptionRule WithRate(Rate value) {
        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._rate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rate</c> is drawn from — <c>AnyValidRate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRate, AnyRate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyThresholdExemptionRule WithRate(Func<AnyRate, AnyRate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._rateRecipe = configure(_rateRecipe);
        copy._rate       = copy._rateRecipe;

        return copy;
    }

    /// <summary>Pins <c>withdrawalCeiling</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyThresholdExemptionRule WithWithdrawalCeiling(Money? value) {
        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._withdrawalCeiling = Any.Fixed(value);
        copy._ceilingPinned     = true;

        return copy;
    }

    /// <summary>Pins <c>withdrawalCeiling</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyThresholdExemptionRule WithoutWithdrawalCeiling() {
        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._withdrawalCeiling = Any.Fixed<Money?>(null);
        copy._ceilingPinned     = true;

        return copy;
    }

    /// <summary>Pins <c>condition</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyThresholdExemptionRule WithCondition(ICondition? value) {
        AnyThresholdExemptionRule copy = new AnyThresholdExemptionRule(this);

        copy._condition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ThresholdExemptionRule" />.</summary>
    public ThresholdExemptionRule Generate() {
        Money threshold = _threshold.Generate();

        // The constructor refuses a ceiling at or below the threshold, a rule between two parameters that
        // no per-parameter recipe can honour: unless a test pinned it, the ceiling is drawn above the threshold.
        Money? ceiling = _ceilingPinned ? _withdrawalCeiling.Generate() : DrawCeilingAbove(threshold);

        return new ThresholdExemptionRule(_code.Generate(),
                                          _label.Generate(),
                                          _taxBase.Generate(),
                                          threshold,
                                          _rate.Generate(),
                                          ceiling,
                                          _condition.Generate());
    }

    private static Money? DrawCeilingAbove(Money threshold) {
        if (Any.Boolean().Generate()) { return null; }

        return threshold.Plus(new Money(Any.Decimal().Between(0.01m, 10_000_000m).WithScale(2).Generate()));
    }

}
