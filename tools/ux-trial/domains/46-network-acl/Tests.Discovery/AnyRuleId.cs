// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RuleId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RuleId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRuleId : IAny<RuleId> {

    private IAny<string> _value;

    private AnyPattern _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRuleId()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        return Any.StringMatching("[a-z]([a-z0-9]|-[a-z0-9]){0,15}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRuleId(AnyPattern value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRuleId(AnyRuleId from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRuleId WithValue(string value) {
        AnyRuleId copy = new AnyRuleId(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRuleId WithValue(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRuleId copy = new AnyRuleId(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RuleId" />.</summary>
    public RuleId Generate() {
        return new RuleId(_value.Generate());
    }

}
