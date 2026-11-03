// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CompositeRule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CompositeRule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCompositeRule : IAny<CompositeRule> {

    private IAny<RuleCode>             _code;
    private IAny<string>               _label;
    private IAny<IEnumerable<TaxRule>> _components;
    private IAny<ICondition?>          _condition;

    private AnyRuleCode _codeRecipe;
    private AnyString   _labelRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCompositeRule()
        : this(code:       AnyValidCode(),
               label:      AnyValidLabel(),
               components: AnyValidComponents(),
               condition:  AnyValidCondition()) { }

    private static AnyRuleCode AnyValidCode() {
        return new AnyRuleCode();
    }

    private static AnyString AnyValidLabel() {
        return Plausible.Label();
    }

    private static IAny<IEnumerable<TaxRule>> AnyValidComponents() {
        // Taxes only, never a credit; codes are long enough that two draws never meet by luck.
        return Any.ListOf(new AnyTaxRule().Leaves()).WithCountBetween(2, 4).As(components => (IEnumerable<TaxRule>)components);
    }

    private static IAny<ICondition?> AnyValidCondition() {
        return new AnyICondition().OrNull();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCompositeRule(AnyRuleCode                code,
                             AnyString                  label,
                             IAny<IEnumerable<TaxRule>> components,
                             IAny<ICondition?>          condition) {
        _code        = code;
        _codeRecipe  = code;
        _label       = label;
        _labelRecipe = label;
        _components  = components;
        _condition   = condition;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCompositeRule(AnyCompositeRule from) {
        _code        = from._code;
        _codeRecipe  = from._codeRecipe;
        _label       = from._label;
        _labelRecipe = from._labelRecipe;
        _components  = from._components;
        _condition   = from._condition;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCompositeRule WithCode(RuleCode value) {
        AnyCompositeRule copy = new AnyCompositeRule(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRuleCode, AnyRuleCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCompositeRule WithCode(Func<AnyRuleCode, AnyRuleCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCompositeRule copy = new AnyCompositeRule(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>label</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCompositeRule WithLabel(string value) {
        AnyCompositeRule copy = new AnyCompositeRule(this);

        copy._label = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>label</c> is drawn from — <c>AnyValidLabel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCompositeRule WithLabel(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCompositeRule copy = new AnyCompositeRule(this);

        copy._labelRecipe = configure(_labelRecipe);
        copy._label       = copy._labelRecipe;

        return copy;
    }

    /// <summary>Pins <c>components</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyCompositeRule WithComponents(IEnumerable<TaxRule> value) {
        AnyCompositeRule copy = new AnyCompositeRule(this);

        copy._components = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>condition</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyCompositeRule WithCondition(ICondition? value) {
        AnyCompositeRule copy = new AnyCompositeRule(this);

        copy._condition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CompositeRule" />.</summary>
    public CompositeRule Generate() {
        return new CompositeRule(_code.Generate(),
                                 _label.Generate(),
                                 _components.Generate(),
                                 _condition.Generate());
    }

}
