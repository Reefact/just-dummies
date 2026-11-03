// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FirewallPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="FirewallPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFirewallPolicy : IAny<FirewallPolicy> {

    private IAny<string>     _name;
    private IAny<RuleAction> _defaultAction;
    private IAny<int>        _capacity;
    private IAny<bool>       _strict;

    private AnyString           _nameRecipe;
    private AnyEnum<RuleAction> _defaultActionRecipe;
    private AnyInt32            _capacityRecipe;
    private AnyBoolean          _strictRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFirewallPolicy()
        : this(name:          AnyValidName(),
               defaultAction: AnyValidDefaultAction(),
               capacity:      AnyValidCapacity(),
               strict:        AnyValidStrict()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(FirewallPolicy.MaxNameLength);
    }

    private static AnyEnum<RuleAction> AnyValidDefaultAction() {
        return Any.Enum<RuleAction>();
    }

    private static AnyInt32 AnyValidCapacity() {
        return Any.Int32().Positive().LessThanOrEqualTo(FirewallPolicy.MaxCapacity);
    }

    private static AnyBoolean AnyValidStrict() {
        return Any.Boolean();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFirewallPolicy(AnyString           name,
                              AnyEnum<RuleAction> defaultAction,
                              AnyInt32            capacity,
                              AnyBoolean          strict) {
        _name                = name;
        _nameRecipe          = name;
        _defaultAction       = defaultAction;
        _defaultActionRecipe = defaultAction;
        _capacity            = capacity;
        _capacityRecipe      = capacity;
        _strict              = strict;
        _strictRecipe        = strict;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFirewallPolicy(AnyFirewallPolicy from) {
        _name                = from._name;
        _nameRecipe          = from._nameRecipe;
        _defaultAction       = from._defaultAction;
        _defaultActionRecipe = from._defaultActionRecipe;
        _capacity            = from._capacity;
        _capacityRecipe      = from._capacityRecipe;
        _strict              = from._strict;
        _strictRecipe        = from._strictRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFirewallPolicy WithName(string value) {
        AnyFirewallPolicy copy = new AnyFirewallPolicy(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFirewallPolicy WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFirewallPolicy copy = new AnyFirewallPolicy(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>defaultAction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFirewallPolicy WithDefaultAction(RuleAction value) {
        AnyFirewallPolicy copy = new AnyFirewallPolicy(this);

        copy._defaultAction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>defaultAction</c> is drawn from — <c>AnyValidDefaultAction()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;RuleAction&gt;, AnyEnum&lt;RuleAction&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFirewallPolicy WithDefaultAction(Func<AnyEnum<RuleAction>, AnyEnum<RuleAction>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFirewallPolicy copy = new AnyFirewallPolicy(this);

        copy._defaultActionRecipe = configure(_defaultActionRecipe);
        copy._defaultAction       = copy._defaultActionRecipe;

        return copy;
    }

    /// <summary>Pins <c>capacity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFirewallPolicy WithCapacity(int value) {
        AnyFirewallPolicy copy = new AnyFirewallPolicy(this);

        copy._capacity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>capacity</c> is drawn from — <c>AnyValidCapacity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFirewallPolicy WithCapacity(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFirewallPolicy copy = new AnyFirewallPolicy(this);

        copy._capacityRecipe = configure(_capacityRecipe);
        copy._capacity       = copy._capacityRecipe;

        return copy;
    }

    /// <summary>Pins <c>strict</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFirewallPolicy WithStrict(bool value) {
        AnyFirewallPolicy copy = new AnyFirewallPolicy(this);

        copy._strict = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>strict</c> is drawn from — <c>AnyValidStrict()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFirewallPolicy WithStrict(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFirewallPolicy copy = new AnyFirewallPolicy(this);

        copy._strictRecipe = configure(_strictRecipe);
        copy._strict       = copy._strictRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="FirewallPolicy" />.</summary>
    public FirewallPolicy Generate() {
        return new FirewallPolicy(_name.Generate(),
                                  _defaultAction.Generate(),
                                  _capacity.Generate(),
                                  _strict.Generate());
    }

}
