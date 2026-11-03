// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CatastropheModel --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CatastropheModel" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCatastropheModel : IAny<CatastropheModel> {

    private IAny<IReadOnlyList<CatastropheEvent>> _catalogue;
    private IAny<double>                          _severityVolatility;

    private AnyList<CatastropheEvent> _catalogueRecipe;
    private AnyDouble                 _severityVolatilityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCatastropheModel()
        : this(catalogue:          AnyValidCatalogue(),
               severityVolatility: AnyValidSeverityVolatility()) { }

    private static AnyList<CatastropheEvent> AnyValidCatalogue() {
        return Any.ListOf(new AnyCatastropheEvent()).WithCountBetween(1, 5).DistinctBy(e => e.Id);
    }

    private static AnyDouble AnyValidSeverityVolatility() {
        return Any.Double().Between(0d, CatastropheModel.MaxSeverityVolatility);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCatastropheModel(AnyList<CatastropheEvent> catalogue,
                                AnyDouble                 severityVolatility) {
        _catalogue                = catalogue;
        _catalogueRecipe          = catalogue;
        _severityVolatility       = severityVolatility;
        _severityVolatilityRecipe = severityVolatility;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCatastropheModel(AnyCatastropheModel from) {
        _catalogue                = from._catalogue;
        _catalogueRecipe          = from._catalogueRecipe;
        _severityVolatility       = from._severityVolatility;
        _severityVolatilityRecipe = from._severityVolatilityRecipe;
    }

    /// <summary>Pins <c>catalogue</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCatastropheModel WithCatalogue(IReadOnlyList<CatastropheEvent> value) {
        AnyCatastropheModel copy = new AnyCatastropheModel(this);

        copy._catalogue = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>catalogue</c> is drawn from — <c>AnyValidCatalogue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;CatastropheEvent&gt;, AnyList&lt;CatastropheEvent&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCatastropheModel WithCatalogue(Func<AnyList<CatastropheEvent>, AnyList<CatastropheEvent>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCatastropheModel copy = new AnyCatastropheModel(this);

        copy._catalogueRecipe = configure(_catalogueRecipe);
        copy._catalogue       = copy._catalogueRecipe;

        return copy;
    }

    /// <summary>Pins <c>severityVolatility</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCatastropheModel WithSeverityVolatility(double value) {
        AnyCatastropheModel copy = new AnyCatastropheModel(this);

        copy._severityVolatility = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>severityVolatility</c> is drawn from — <c>AnyValidSeverityVolatility()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCatastropheModel WithSeverityVolatility(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCatastropheModel copy = new AnyCatastropheModel(this);

        copy._severityVolatilityRecipe = configure(_severityVolatilityRecipe);
        copy._severityVolatility       = copy._severityVolatilityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CatastropheModel" />.</summary>
    public CatastropheModel Generate() {
        return new CatastropheModel(_catalogue.Generate(),
                                    _severityVolatility.Generate());
    }

}
