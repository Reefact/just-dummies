// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ProgressiveRule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Linq;
using System.Collections.Generic;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ProgressiveRule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyProgressiveRule : IAny<ProgressiveRule> {

    private IAny<RuleCode>                _code;
    private IAny<string>                  _label;
    private IAny<IncomeKind>              _taxBase;
    private IAny<IEnumerable<TaxBracket>> _brackets;
    private IAny<ICondition?>             _condition;

    private AnyRuleCode             _codeRecipe;
    private AnyString               _labelRecipe;
    private AnyEnum<IncomeKind>     _taxBaseRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyProgressiveRule()
        : this(code:      AnyValidCode(),
               label:     AnyValidLabel(),
               taxBase:   AnyValidTaxBase(),
               brackets:  AnyValidBrackets(),
               condition: AnyValidCondition()) { }

    private static AnyRuleCode AnyValidCode() {
        return new AnyRuleCode();
    }

    private static AnyString AnyValidLabel() {
        return Plausible.Label();
    }

    private static AnyEnum<IncomeKind> AnyValidTaxBase() {
        return Plausible.TaxBase();
    }

    /// <summary>
    ///     One to six contiguous brackets from zero, the last open-ended, the rates never decreasing. The cut points are
    ///     drawn ascending and distinct, the rates ascending, so the scale is valid by construction.
    /// </summary>
    private static IAny<IEnumerable<TaxBracket>> AnyValidBrackets() {
        return Any.Int32().Between(1, 6).Then(count => Any.Combine(
            Any.ListOf(Any.Decimal().Between(0.01m, 5_000_000m).WithScale(2)).WithCount(count - 1).Ordered().Distinct(),
            Any.ListOf(Any.Decimal().Between(0m, 1m).WithScale(4)).WithCount(count).Ordered(),
            (cuts, rates) => (IEnumerable<TaxBracket>)Enumerable.Range(0, rates.Count)
                                                                .Select(i => new TaxBracket(new Money(i == 0 ? 0m : cuts[i - 1]),
                                                                                            i < cuts.Count ? new Money(cuts[i]) : null,
                                                                                            new Rate(rates[i])))
                                                                .ToList()));
    }

    private static IAny<ICondition?> AnyValidCondition() {
        return new AnyICondition().OrNull();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyProgressiveRule(AnyRuleCode             code,
                               AnyString               label,
                               AnyEnum<IncomeKind>     taxBase,
                               IAny<IEnumerable<TaxBracket>> brackets,
                               IAny<ICondition?>       condition) {
        _code           = code;
        _codeRecipe     = code;
        _label          = label;
        _labelRecipe    = label;
        _taxBase        = taxBase;
        _taxBaseRecipe  = taxBase;
        _brackets       = brackets;
        _condition      = condition;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyProgressiveRule(AnyProgressiveRule from) {
        _code           = from._code;
        _codeRecipe     = from._codeRecipe;
        _label          = from._label;
        _labelRecipe    = from._labelRecipe;
        _taxBase        = from._taxBase;
        _taxBaseRecipe  = from._taxBaseRecipe;
        _brackets       = from._brackets;
        _condition      = from._condition;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProgressiveRule WithCode(RuleCode value) {
        AnyProgressiveRule copy = new AnyProgressiveRule(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRuleCode, AnyRuleCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProgressiveRule WithCode(Func<AnyRuleCode, AnyRuleCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProgressiveRule copy = new AnyProgressiveRule(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>label</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProgressiveRule WithLabel(string value) {
        AnyProgressiveRule copy = new AnyProgressiveRule(this);

        copy._label = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>label</c> is drawn from — <c>AnyValidLabel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProgressiveRule WithLabel(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProgressiveRule copy = new AnyProgressiveRule(this);

        copy._labelRecipe = configure(_labelRecipe);
        copy._label       = copy._labelRecipe;

        return copy;
    }

    /// <summary>Pins <c>taxBase</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProgressiveRule WithTaxBase(IncomeKind value) {
        AnyProgressiveRule copy = new AnyProgressiveRule(this);

        copy._taxBase = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>taxBase</c> is drawn from — <c>AnyValidTaxBase()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;IncomeKind&gt;, AnyEnum&lt;IncomeKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyProgressiveRule WithTaxBase(Func<AnyEnum<IncomeKind>, AnyEnum<IncomeKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyProgressiveRule copy = new AnyProgressiveRule(this);

        copy._taxBaseRecipe = configure(_taxBaseRecipe);
        copy._taxBase       = copy._taxBaseRecipe;

        return copy;
    }

    /// <summary>Pins <c>brackets</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyProgressiveRule WithBrackets(IEnumerable<TaxBracket> value) {
        AnyProgressiveRule copy = new AnyProgressiveRule(this);

        copy._brackets = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>condition</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyProgressiveRule WithCondition(ICondition? value) {
        AnyProgressiveRule copy = new AnyProgressiveRule(this);

        copy._condition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ProgressiveRule" />.</summary>
    public ProgressiveRule Generate() {
        return new ProgressiveRule(_code.Generate(),
                                   _label.Generate(),
                                   _taxBase.Generate(),
                                   _brackets.Generate(),
                                   _condition.Generate());
    }

}
