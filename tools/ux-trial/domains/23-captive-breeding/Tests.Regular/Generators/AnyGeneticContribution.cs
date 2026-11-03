// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GeneticContribution --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GeneticContribution" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGeneticContribution : IAny<GeneticContribution> {

    private IAny<StudbookId> _founderId;
    private IAny<double>     _proportion;

    private AnyStudbookId _founderIdRecipe;
    private AnyDouble     _proportionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeneticContribution()
        : this(founderId:  AnyValidFounderId(),
               proportion: AnyValidProportion()) { }

    private static AnyStudbookId AnyValidFounderId() {
        return new AnyStudbookId();
    }

    private static AnyDouble AnyValidProportion() {
        return Any.Double().Between(0d, 1d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGeneticContribution(AnyStudbookId founderId,
                                   AnyDouble     proportion) {
        _founderId        = founderId;
        _founderIdRecipe  = founderId;
        _proportion       = proportion;
        _proportionRecipe = proportion;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGeneticContribution(AnyGeneticContribution from) {
        _founderId        = from._founderId;
        _founderIdRecipe  = from._founderIdRecipe;
        _proportion       = from._proportion;
        _proportionRecipe = from._proportionRecipe;
    }

    /// <summary>Pins <c>founderId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeneticContribution WithFounderId(StudbookId value) {
        AnyGeneticContribution copy = new AnyGeneticContribution(this);

        copy._founderId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>founderId</c> is drawn from — <c>AnyValidFounderId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStudbookId, AnyStudbookId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeneticContribution WithFounderId(Func<AnyStudbookId, AnyStudbookId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeneticContribution copy = new AnyGeneticContribution(this);

        copy._founderIdRecipe = configure(_founderIdRecipe);
        copy._founderId       = copy._founderIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>proportion</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGeneticContribution WithProportion(double value) {
        AnyGeneticContribution copy = new AnyGeneticContribution(this);

        copy._proportion = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>proportion</c> is drawn from — <c>AnyValidProportion()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGeneticContribution WithProportion(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeneticContribution copy = new AnyGeneticContribution(this);

        copy._proportionRecipe = configure(_proportionRecipe);
        copy._proportion       = copy._proportionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="GeneticContribution" />.</summary>
    public GeneticContribution Generate() {
        return new GeneticContribution(_founderId.Generate(),
                                       _proportion.Generate());
    }

}
