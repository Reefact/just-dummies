// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Exposure --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Exposure" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyExposure : IAny<Exposure> {

    private IAny<InsuredProperty> _property;
    private IAny<PolicyTerms>     _terms;

    private AnyInsuredProperty _propertyRecipe;
    private AnyPolicyTerms     _termsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExposure()
        : this(property: AnyValidProperty(),
               terms:    AnyValidTerms()) { }

    private static AnyInsuredProperty AnyValidProperty() {
        return new AnyInsuredProperty();
    }

    private static AnyPolicyTerms AnyValidTerms() {
        // Not drawn from this recipe: Generate() derives the terms from the insured value.
        return new AnyPolicyTerms();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyExposure(AnyInsuredProperty property,
                        AnyPolicyTerms     terms) {
        _property       = property;
        _propertyRecipe = property;
        _terms          = terms;
        _termsRecipe    = terms;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyExposure(AnyExposure from) {
        _property       = from._property;
        _propertyRecipe = from._propertyRecipe;
        _terms          = from._terms;
        _termsRecipe    = from._termsRecipe;
    }

    /// <summary>Pins <c>property</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExposure WithProperty(InsuredProperty value) {
        AnyExposure copy = new AnyExposure(this);

        copy._property = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>property</c> is drawn from — <c>AnyValidProperty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInsuredProperty, AnyInsuredProperty&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExposure WithProperty(Func<AnyInsuredProperty, AnyInsuredProperty> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExposure copy = new AnyExposure(this);

        copy._propertyRecipe = configure(_propertyRecipe);
        copy._property       = copy._propertyRecipe;

        return copy;
    }

    /// <summary>Pins <c>terms</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExposure WithTerms(PolicyTerms value) {
        AnyExposure copy = new AnyExposure(this);

        copy._terms = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>terms</c> is drawn from — <c>AnyValidTerms()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPolicyTerms, AnyPolicyTerms&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExposure WithTerms(Func<AnyPolicyTerms, AnyPolicyTerms> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExposure copy = new AnyExposure(this);

        copy._termsRecipe = configure(_termsRecipe);
        copy._terms       = copy._termsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Exposure" />.</summary>
    public Exposure Generate() {
        InsuredProperty property = _property.Generate();

        // deductible < insured value, limit <= insured value
        decimal deductible = Any.Decimal().Between(0m, property.InsuredValue / 2m).WithScale(2).Generate();
        decimal limit      = Any.Decimal().Between(property.InsuredValue / 10m, property.InsuredValue).WithScale(2).Generate();

        return new Exposure(property, new PolicyTerms(deductible, limit));
    }

}
