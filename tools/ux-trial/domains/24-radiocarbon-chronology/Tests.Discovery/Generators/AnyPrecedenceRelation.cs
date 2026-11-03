// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PrecedenceRelation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PrecedenceRelation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPrecedenceRelation : IAny<PrecedenceRelation> {

    private IAny<string> _olderLayerCode;
    private IAny<string> _youngerLayerCode;

    private AnyString _olderLayerCodeRecipe;
    private AnyString _youngerLayerCodeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPrecedenceRelation()
        : this(olderLayerCode:   AnyValidOlderLayerCode(),
               youngerLayerCode: AnyValidYoungerLayerCode()) { }

    private static AnyString AnyValidOlderLayerCode() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidYoungerLayerCode() {
        return Any.String().NotBlank();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPrecedenceRelation(AnyString olderLayerCode,
                                  AnyString youngerLayerCode) {
        _olderLayerCode         = olderLayerCode;
        _olderLayerCodeRecipe   = olderLayerCode;
        _youngerLayerCode       = youngerLayerCode;
        _youngerLayerCodeRecipe = youngerLayerCode;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPrecedenceRelation(AnyPrecedenceRelation from) {
        _olderLayerCode         = from._olderLayerCode;
        _olderLayerCodeRecipe   = from._olderLayerCodeRecipe;
        _youngerLayerCode       = from._youngerLayerCode;
        _youngerLayerCodeRecipe = from._youngerLayerCodeRecipe;
    }

    /// <summary>Pins <c>olderLayerCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrecedenceRelation WithOlderLayerCode(string value) {
        AnyPrecedenceRelation copy = new AnyPrecedenceRelation(this);

        copy._olderLayerCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>olderLayerCode</c> is drawn from — <c>AnyValidOlderLayerCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrecedenceRelation WithOlderLayerCode(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrecedenceRelation copy = new AnyPrecedenceRelation(this);

        copy._olderLayerCodeRecipe = configure(_olderLayerCodeRecipe);
        copy._olderLayerCode       = copy._olderLayerCodeRecipe;

        return copy;
    }

    /// <summary>Pins <c>youngerLayerCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPrecedenceRelation WithYoungerLayerCode(string value) {
        AnyPrecedenceRelation copy = new AnyPrecedenceRelation(this);

        copy._youngerLayerCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>youngerLayerCode</c> is drawn from — <c>AnyValidYoungerLayerCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPrecedenceRelation WithYoungerLayerCode(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPrecedenceRelation copy = new AnyPrecedenceRelation(this);

        copy._youngerLayerCodeRecipe = configure(_youngerLayerCodeRecipe);
        copy._youngerLayerCode       = copy._youngerLayerCodeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PrecedenceRelation" />.</summary>
    public PrecedenceRelation Generate() {
        string older = _olderLayerCode.Generate();

        // A layer cannot precede itself: the younger code is redrawn while it equals the older one.
        return new PrecedenceRelation(older, _youngerLayerCode.DifferentFrom(older).Generate());
    }

}
