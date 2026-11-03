// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Verdict --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Verdict" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVerdict : IAny<Verdict> {

    private IAny<RuleAction> _action;
    private IAny<RuleId?>    _matchedRule;

    private AnyEnum<RuleAction> _actionRecipe;
    private AnyRuleId           _matchedRuleRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVerdict()
        : this(action:      AnyValidAction(),
               matchedRule: AnyValidMatchedRule()) { }

    private static AnyEnum<RuleAction> AnyValidAction() {
        return Any.Enum<RuleAction>();
    }

    private static AnyRuleId AnyValidMatchedRule() {
        return new AnyRuleId();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVerdict(AnyEnum<RuleAction> action,
                       AnyRuleId           matchedRule) {
        _action            = action;
        _actionRecipe      = action;
        _matchedRule       = matchedRule.OrNull();
        _matchedRuleRecipe = matchedRule;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVerdict(AnyVerdict from) {
        _action            = from._action;
        _actionRecipe      = from._actionRecipe;
        _matchedRule       = from._matchedRule;
        _matchedRuleRecipe = from._matchedRuleRecipe;
    }

    /// <summary>Pins <c>action</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVerdict WithAction(RuleAction value) {
        AnyVerdict copy = new AnyVerdict(this);

        copy._action = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>action</c> is drawn from — <c>AnyValidAction()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;RuleAction&gt;, AnyEnum&lt;RuleAction&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVerdict WithAction(Func<AnyEnum<RuleAction>, AnyEnum<RuleAction>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVerdict copy = new AnyVerdict(this);

        copy._actionRecipe = configure(_actionRecipe);
        copy._action       = copy._actionRecipe;

        return copy;
    }

    /// <summary>Pins <c>matchedRule</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVerdict WithMatchedRule(RuleId? value) {
        AnyVerdict copy = new AnyVerdict(this);

        copy._matchedRule = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>matchedRule</c> is drawn from — <c>AnyValidMatchedRule()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRuleId, AnyRuleId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVerdict WithMatchedRule(Func<AnyRuleId, AnyRuleId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVerdict copy = new AnyVerdict(this);

        copy._matchedRuleRecipe = configure(_matchedRuleRecipe);
        copy._matchedRule       = copy._matchedRuleRecipe;

        return copy;
    }

    /// <summary>Pins <c>matchedRule</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyVerdict WithoutMatchedRule() {
        AnyVerdict copy = new AnyVerdict(this);

        copy._matchedRule = Any.Fixed<RuleId?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Verdict" />.</summary>
    public Verdict Generate() {
        return new Verdict(_action.Generate(),
                           _matchedRule.Generate());
    }

}
