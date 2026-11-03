// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RemittanceInformation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RemittanceInformation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRemittanceInformation : IAny<RemittanceInformation> {

    private IAny<string> _text;

    private AnyString _textRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRemittanceInformation()
        : this(text: AnyValidText()) { }

    private static AnyString AnyValidText() {
        return Any.String().WithChars(SepaCharacters).NotBlank().WithMaxLength(RemittanceInformation.MaximumLength);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRemittanceInformation(AnyString text) {
        _text       = text;
        _textRecipe = text;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRemittanceInformation(AnyRemittanceInformation from) {
        _text       = from._text;
        _textRecipe = from._textRecipe;
    }

    /// <summary>Pins <c>text</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRemittanceInformation WithText(string value) {
        AnyRemittanceInformation copy = new AnyRemittanceInformation(this);

        copy._text = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>text</c> is drawn from — <c>AnyValidText()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRemittanceInformation WithText(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRemittanceInformation copy = new AnyRemittanceInformation(this);

        copy._textRecipe = configure(_textRecipe);
        copy._text       = copy._textRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RemittanceInformation" />.</summary>
    public RemittanceInformation Generate() {
        return new RemittanceInformation(_text.Generate());
    }

}
