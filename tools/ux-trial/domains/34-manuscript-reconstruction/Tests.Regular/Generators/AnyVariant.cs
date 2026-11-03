// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Variant --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Variant" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVariant : IAny<Variant> {

    private IAny<PassageReference> _passage;
    private IAny<WitnessSiglum>    _witness;
    private IAny<string>           _text;
    private IAny<VariantKind>      _kind;
    private IAny<int>              _hand;

    private AnyPassageReference  _passageRecipe;
    private AnyWitnessSiglum     _witnessRecipe;
    private AnyString            _textRecipe;
    private AnyEnum<VariantKind> _kindRecipe;
    private AnyInt32             _handRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVariant()
        : this(passage: AnyValidPassage(),
               witness: AnyValidWitness(),
               text:    AnyValidText(),
               kind:    AnyValidKind(),
               hand:    AnyValidHand()) { }

    private static AnyPassageReference AnyValidPassage() {
        return new AnyPassageReference();
    }

    private static AnyWitnessSiglum AnyValidWitness() {
        return new AnyWitnessSiglum();
    }

    private static AnyString AnyValidText() {
        return Any.String().NotBlank().WithMaxLength(Variant.MaxTextLength);
    }

    private static AnyEnum<VariantKind> AnyValidKind() {
        return Any.Enum<VariantKind>();
    }

    private static AnyInt32 AnyValidHand() {
        return Any.Int32().Between(Variant.FirstHand, Variant.MaxHand);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVariant(AnyPassageReference  passage,
                       AnyWitnessSiglum     witness,
                       AnyString            text,
                       AnyEnum<VariantKind> kind,
                       AnyInt32             hand) {
        _passage       = passage;
        _passageRecipe = passage;
        _witness       = witness;
        _witnessRecipe = witness;
        _text          = text;
        _textRecipe    = text;
        _kind          = kind;
        _kindRecipe    = kind;
        _hand          = hand;
        _handRecipe    = hand;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVariant(AnyVariant from) {
        _passage       = from._passage;
        _passageRecipe = from._passageRecipe;
        _witness       = from._witness;
        _witnessRecipe = from._witnessRecipe;
        _text          = from._text;
        _textRecipe    = from._textRecipe;
        _kind          = from._kind;
        _kindRecipe    = from._kindRecipe;
        _hand          = from._hand;
        _handRecipe    = from._handRecipe;
    }

    /// <summary>Pins <c>passage</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithPassage(PassageReference value) {
        AnyVariant copy = new AnyVariant(this);

        copy._passage = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>passage</c> is drawn from — <c>AnyValidPassage()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPassageReference, AnyPassageReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithPassage(Func<AnyPassageReference, AnyPassageReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._passageRecipe = configure(_passageRecipe);
        copy._passage       = copy._passageRecipe;

        return copy;
    }

    /// <summary>Pins <c>witness</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithWitness(WitnessSiglum value) {
        AnyVariant copy = new AnyVariant(this);

        copy._witness = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>witness</c> is drawn from — <c>AnyValidWitness()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWitnessSiglum, AnyWitnessSiglum&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithWitness(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._witnessRecipe = configure(_witnessRecipe);
        copy._witness       = copy._witnessRecipe;

        return copy;
    }

    /// <summary>Pins <c>text</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithText(string value) {
        AnyVariant copy = new AnyVariant(this);

        copy._text = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>text</c> is drawn from — <c>AnyValidText()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithText(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._textRecipe = configure(_textRecipe);
        copy._text       = copy._textRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithKind(VariantKind value) {
        AnyVariant copy = new AnyVariant(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;VariantKind&gt;, AnyEnum&lt;VariantKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithKind(Func<AnyEnum<VariantKind>, AnyEnum<VariantKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>hand</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVariant WithHand(int value) {
        AnyVariant copy = new AnyVariant(this);

        copy._hand = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>hand</c> is drawn from — <c>AnyValidHand()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVariant WithHand(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVariant copy = new AnyVariant(this);

        copy._handRecipe = configure(_handRecipe);
        copy._hand       = copy._handRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Variant" />.</summary>
    public Variant Generate() {
        VariantKind kind = _kind.Generate();
        int         hand = _hand.Generate();

        // A correction is by a later hand, and an omission carries no text: the kind decides, the rest follows.
        if (kind == VariantKind.Correction && hand == Variant.FirstHand) {
            hand = Any.Int32().Between(Variant.FirstHand + 1, Variant.MaxHand).Generate();
        }

        string text = kind == VariantKind.Omission ? string.Empty : _text.Generate();

        return new Variant(_passage.Generate(),
                           _witness.Generate(),
                           text,
                           kind,
                           hand);
    }

}
