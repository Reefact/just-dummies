// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Variant --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Variant" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVariant : IAny<Variant> {

    private readonly IAny<PassageReference> _passage;
    private readonly IAny<WitnessSiglum>    _witness;
    private readonly IAny<string>           _text;
    private readonly IAny<VariantKind>      _kind;
    private readonly IAny<int>              _hand;

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

    private static AnyPattern AnyValidText() {
        return Any.Reading();
    }

    private static AnyEnum<VariantKind> AnyValidKind() {
        return Any.Enum<VariantKind>();
    }

    private static AnyInt32 AnyValidHand() {
        return Any.Int32().Positive().LessThanOrEqualTo(9);
    }

    private AnyVariant(IAny<PassageReference> passage,
                       IAny<WitnessSiglum>    witness,
                       IAny<string>           text,
                       IAny<VariantKind>      kind,
                       IAny<int>              hand) {
        _passage = passage;
        _witness = witness;
        _text    = text;
        _kind    = kind;
        _hand    = hand;
    }

    /// <summary>Pins <c>passage</c> to a fixed value.</summary>
    public AnyVariant WithPassage(PassageReference value) {
        return new AnyVariant(new FixedValue<PassageReference>(value), _witness, _text, _kind, _hand);
    }

    /// <summary>Draws <c>passage</c> from <c>AnyValidPassage()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithPassage(Func<AnyPassageReference, AnyPassageReference> configure) {
        return new AnyVariant(configure(AnyValidPassage()), _witness, _text, _kind, _hand);
    }

    /// <summary>Pins <c>witness</c> to a fixed value.</summary>
    public AnyVariant WithWitness(WitnessSiglum value) {
        return new AnyVariant(_passage, new FixedValue<WitnessSiglum>(value), _text, _kind, _hand);
    }

    /// <summary>Draws <c>witness</c> from <c>AnyValidWitness()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithWitness(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        return new AnyVariant(_passage, configure(AnyValidWitness()), _text, _kind, _hand);
    }

    /// <summary>Pins <c>text</c> to a fixed value.</summary>
    public AnyVariant WithText(string value) {
        return new AnyVariant(_passage, _witness, new FixedValue<string>(value), _kind, _hand);
    }

    /// <summary>Draws <c>text</c> from <c>AnyValidText()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithText(Func<AnyPattern, AnyPattern> configure) {
        return new AnyVariant(_passage, _witness, configure(AnyValidText()), _kind, _hand);
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyVariant WithKind(VariantKind value) {
        return new AnyVariant(_passage, _witness, _text, new FixedValue<VariantKind>(value), _hand);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithKind(Func<AnyEnum<VariantKind>, AnyEnum<VariantKind>> configure) {
        return new AnyVariant(_passage, _witness, _text, configure(AnyValidKind()), _hand);
    }

    /// <summary>Pins <c>hand</c> to a fixed value.</summary>
    public AnyVariant WithHand(int value) {
        return new AnyVariant(_passage, _witness, _text, _kind, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>hand</c> from <c>AnyValidHand()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithHand(Func<AnyInt32, AnyInt32> configure) {
        return new AnyVariant(_passage, _witness, _text, _kind, configure(AnyValidHand()));
    }

    /// <summary>Produces one arbitrary <see cref="Variant" />.</summary>
    public Variant Generate() {
        // Three rules tie text, kind and hand: an omission carries no text, every other kind carries
        // some, and a correction is never by the first hand. What the test pinned is taken as written
        // and narrows what is still drawn.
        int?    pinnedHand = _hand is AnyInt32 ? null : _hand.Generate();
        string? pinnedText = _text is AnyPattern ? null : _text.Generate();

        VariantKind kind;
        if (_kind is AnyEnum<VariantKind> anyKind) {
            if (pinnedHand == Variant.FirstHand) { anyKind = anyKind.DifferentFrom(VariantKind.Correction); }
            if (pinnedText is not null) {
                anyKind = string.IsNullOrWhiteSpace(pinnedText) ? anyKind.OneOf(VariantKind.Omission) : anyKind.DifferentFrom(VariantKind.Omission);
            }

            kind = anyKind.Generate();
        } else {
            kind = _kind.Generate();
        }

        int hand = pinnedHand ?? (kind == VariantKind.Correction && _hand is AnyInt32 anyHand
                                      ? anyHand.GreaterThan(Variant.FirstHand).Generate()
                                      : _hand.Generate());
        string text = pinnedText ?? (kind == VariantKind.Omission ? string.Empty : _text.Generate());

        return new Variant(_passage.Generate(), _witness.Generate(), text, kind, hand);
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
