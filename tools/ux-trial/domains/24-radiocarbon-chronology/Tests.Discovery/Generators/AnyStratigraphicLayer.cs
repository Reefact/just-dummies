// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StratigraphicLayer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StratigraphicLayer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStratigraphicLayer : IAny<StratigraphicLayer> {

    private IAny<string> _code;
    private IAny<string> _description;

    private AnyPattern _codeRecipe;
    private AnyString _descriptionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStratigraphicLayer()
        : this(code:        AnyValidCode(),
               description: AnyValidDescription()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching("[A-Z]{1,3}[0-9]{1,4}");
    }

    private static AnyString AnyValidDescription() {
        return Any.String().NotBlank().WithMaxLength(500);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStratigraphicLayer(AnyPattern code,
                                  AnyString description) {
        _code              = code;
        _codeRecipe        = code;
        _description       = description;
        _descriptionRecipe = description;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStratigraphicLayer(AnyStratigraphicLayer from) {
        _code              = from._code;
        _codeRecipe        = from._codeRecipe;
        _description       = from._description;
        _descriptionRecipe = from._descriptionRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStratigraphicLayer WithCode(string value) {
        AnyStratigraphicLayer copy = new AnyStratigraphicLayer(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStratigraphicLayer WithCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStratigraphicLayer copy = new AnyStratigraphicLayer(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>description</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStratigraphicLayer WithDescription(string value) {
        AnyStratigraphicLayer copy = new AnyStratigraphicLayer(this);

        copy._description = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>description</c> is drawn from — <c>AnyValidDescription()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStratigraphicLayer WithDescription(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStratigraphicLayer copy = new AnyStratigraphicLayer(this);

        copy._descriptionRecipe = configure(_descriptionRecipe);
        copy._description       = copy._descriptionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="StratigraphicLayer" />.</summary>
    public StratigraphicLayer Generate() {
        return new StratigraphicLayer(_code.Generate(),
                                      _description.Generate());
    }

}
