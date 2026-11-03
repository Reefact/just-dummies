// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CappedRule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Linq;

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
        return AnyLabel.Create();
    }

    private static IAny<TaxRule> AnyValidInner() {
        return new AnyLeafTaxRule();
    }

    private static AnyMoney AnyValidMaximum() {
        return new AnyMoney().WithAmount(a => a.GreaterThan(0m).LessThanOrEqualTo(100_000_000m));
    }

    private static AnyMoney AnyValidMinimum() {
        // Drawn at or below the maximum in Generate() when it falls above it.
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(100_000_000m));
    }

    private static IAny<ICondition?> AnyValidCondition() {
        // A rule under test applies unless the test says otherwise: no condition, rather than a random one that would fail it.
        return Any.Fixed<ICondition?>(null);
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

        copy._minimum = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minimum</c> is drawn from — <c>AnyValidMinimum()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCappedRule WithMinimum(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCappedRule copy = new AnyCappedRule(this);

        copy._minimumRecipe = configure(_minimumRecipe);
        copy._minimum       = copy._minimumRecipe;

        return copy;
    }

    /// <summary>Pins <c>minimum</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyCappedRule WithoutMinimum() {
        AnyCappedRule copy = new AnyCappedRule(this);

        copy._minimum = Any.Fixed<Money?>(null);

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
        TaxRule inner   = _inner.Generate();
        Money   maximum = _maximum.Generate();
        Money?  minimum = _minimum.Generate();
        RuleCode code   = _code.Generate();

        // The minimum never exceeds the maximum, and the cap's own code is new to the wrapped tree: two ties, resolved by redrawing.
        if (minimum is not null && minimum.CompareTo(maximum) > 0) {
            minimum = new Money(Any.Decimal().Between(0m, maximum.Amount).WithScale(2).Generate());
        }

        while (inner.SelfAndDescendants().Any(r => r.Code.Equals(code))) {
            code = new AnyRuleCode().Generate();
        }

        return new CappedRule(code,
                              _label.Generate(),
                              inner,
                              maximum,
                              minimum,
                              _condition.Generate());
    }

}
