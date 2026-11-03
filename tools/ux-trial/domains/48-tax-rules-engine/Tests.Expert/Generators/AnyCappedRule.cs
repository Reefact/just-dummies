// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CappedRule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CappedRule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCappedRule : IAny<CappedRule> {

    private IAny<RuleCode>    _code;
    private IAny<string>      _label;
    private IAny<TaxRule>     _inner;
    private IAny<Money>       _maximum;
    private IAny<Money?>      _minimum;
    private bool              _minimumPinned;
    private IAny<ICondition?> _condition;

    private AnyRuleCode _codeRecipe;
    private AnyString   _labelRecipe;
    private AnyMoney    _maximumRecipe;
    private AnyMoney    _minimumRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCappedRule()
        : this(code:      AnyValidCode(),
               label:     AnyValidLabel(),
               inner:     AnyValidInner(),
               maximum:   AnyValidMaximum(),
               minimum:   AnyValidMinimum(),
               condition: AnyValidCondition()) { }

    private static AnyRuleCode AnyValidCode() {
        return new AnyRuleCode();
    }

    private static AnyString AnyValidLabel() {
        return Plausible.Label();
    }

    private static IAny<TaxRule> AnyValidInner() {
        // Neither a cap nor a credit: the two things the constructor refuses to wrap.
        return new AnyTaxRule().NotCapped();
    }

    private static AnyMoney AnyValidMaximum() {
        return Plausible.Amount().WithAmount(a => a.GreaterThan(0m));
    }

    private static AnyMoney AnyValidMinimum() {
        return Plausible.Amount();
    }

    private static IAny<ICondition?> AnyValidCondition() {
        return new AnyICondition().OrNull();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCappedRule(AnyRuleCode       code,
                          AnyString         label,
                          IAny<TaxRule>     inner,
                          AnyMoney          maximum,
                          AnyMoney          minimum,
                          IAny<ICondition?> condition) {
        _code          = code;
        _codeRecipe    = code;
        _label         = label;
        _labelRecipe   = label;
        _inner         = inner;
        _maximum       = maximum;
        _maximumRecipe = maximum;
        _minimum       = minimum.OrNull();
        _minimumRecipe = minimum;
        _condition     = condition;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCappedRule(AnyCappedRule from) {
        _code          = from._code;
        _codeRecipe    = from._codeRecipe;
        _label         = from._label;
        _labelRecipe   = from._labelRecipe;
        _inner         = from._inner;
        _maximum       = from._maximum;
        _maximumRecipe = from._maximumRecipe;
        _minimum       = from._minimum;
        _minimumRecipe = from._minimumRecipe;
        _minimumPinned = from._minimumPinned;
        _condition     = from._condition;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCappedRule WithCode(RuleCode value) {
        AnyCappedRule copy = new AnyCappedRule(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRuleCode, AnyRuleCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCappedRule WithCode(Func<AnyRuleCode, AnyRuleCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCappedRule copy = new AnyCappedRule(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>label</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCappedRule WithLabel(string value) {
        AnyCappedRule copy = new AnyCappedRule(this);

        copy._label = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>label</c> is drawn from — <c>AnyValidLabel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCappedRule WithLabel(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCappedRule copy = new AnyCappedRule(this);

        copy._labelRecipe = configure(_labelRecipe);
        copy._label       = copy._labelRecipe;

        return copy;
    }

    /// <summary>Pins <c>inner</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyCappedRule WithInner(TaxRule value) {
        AnyCappedRule copy = new AnyCappedRule(this);

        copy._inner = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>maximum</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCappedRule WithMaximum(Money value) {
        AnyCappedRule copy = new AnyCappedRule(this);

        copy._maximum = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maximum</c> is drawn from — <c>AnyValidMaximum()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCappedRule WithMaximum(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCappedRule copy = new AnyCappedRule(this);

        copy._maximumRecipe = configure(_maximumRecipe);
        copy._maximum       = copy._maximumRecipe;

        return copy;
    }

    /// <summary>Pins <c>minimum</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCappedRule WithMinimum(Money? value) {
        AnyCappedRule copy = new AnyCappedRule(this);

        copy._minimum       = Any.Fixed(value);
        copy._minimumPinned = true;

        return copy;
    }

    /// <summary>Pins <c>minimum</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyCappedRule WithoutMinimum() {
        AnyCappedRule copy = new AnyCappedRule(this);

        copy._minimum       = Any.Fixed<Money?>(null);
        copy._minimumPinned = true;

        return copy;
    }

    /// <summary>Pins <c>condition</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyCappedRule WithCondition(ICondition? value) {
        AnyCappedRule copy = new AnyCappedRule(this);

        copy._condition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CappedRule" />.</summary>
    public CappedRule Generate() {
        Money maximum = _maximum.Generate();

        // The constructor refuses a minimum above the maximum: unless a test pinned it, it is drawn below.
        Money? minimum = _minimumPinned ? _minimum.Generate() : DrawMinimumUnder(maximum);

        return new CappedRule(_code.Generate(),
                              _label.Generate(),
                              _inner.Generate(),
                              maximum,
                              minimum,
                              _condition.Generate());
    }

    private static Money? DrawMinimumUnder(Money maximum) {
        if (Any.Boolean().Generate()) { return null; }

        return new Money(Any.Decimal().Between(0m, maximum.Amount).WithScale(2).Generate());
    }

}
