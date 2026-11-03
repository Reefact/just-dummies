// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tradition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Tradition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTradition : IAny<Tradition> {

    private IAny<string> _workTitle;

    private AnyString _workTitleRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTradition()
        : this(workTitle: AnyValidWorkTitle()) { }

    private static AnyString AnyValidWorkTitle() {
        return Any.String().NotBlank();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTradition(AnyString workTitle) {
        _workTitle       = workTitle;
        _workTitleRecipe = workTitle;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTradition(AnyTradition from) {
        _workTitle       = from._workTitle;
        _workTitleRecipe = from._workTitleRecipe;
    }

    /// <summary>Pins <c>workTitle</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTradition WithWorkTitle(string value) {
        AnyTradition copy = new AnyTradition(this);

        copy._workTitle = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>workTitle</c> is drawn from — <c>AnyValidWorkTitle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTradition WithWorkTitle(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTradition copy = new AnyTradition(this);

        copy._workTitleRecipe = configure(_workTitleRecipe);
        copy._workTitle       = copy._workTitleRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Tradition" />.</summary>
    public Tradition Generate() {
        return new Tradition(_workTitle.Generate());
    }

}
