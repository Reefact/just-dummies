// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EncodingVariant --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EncodingVariant" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEncodingVariant : IAny<EncodingVariant> {

    private IAny<int>           _framesPerSecond;
    private AnyOrderedPair<int> _heightAndWidth;
    private IAny<Bitrate>       _bitrate;

    private AnyInt32   _framesPerSecondRecipe;
    private AnyInt32   _widthRecipe;
    private AnyInt32   _heightRecipe;
    private AnyBitrate _bitrateRecipe;

    private int? _widthPin;
    private int? _heightPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEncodingVariant()
        : this(framesPerSecond: AnyValidFramesPerSecond(),
               width:           AnyValidWidth(),
               height:          AnyValidHeight(),
               bitrate:         AnyValidBitrate()) { }

    // The id is not drawn on its own: it is "<height>p<fps>" (the constructor ties it to the height), so the
    // draw is on the frame rate and Generate() spells the id.
    private static AnyInt32 AnyValidFramesPerSecond() {
        return Any.Int32().OneOf(24, 25, 30, 50, 60);
    }

    private static AnyInt32 AnyValidWidth() {
        return Any.Int32().Between(128, 7680).Even();
    }

    private static AnyInt32 AnyValidHeight() {
        // 100, not 96: the id needs three digits ("<height>p<fps>"), so heights 96 and 98 admit no valid id.
        return Any.Int32().Between(100, 4320).Even();
    }

    private static AnyBitrate AnyValidBitrate() {
        return new AnyBitrate();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEncodingVariant(AnyInt32   framesPerSecond,
                               AnyInt32   width,
                               AnyInt32   height,
                               AnyBitrate bitrate) {
        _framesPerSecond       = framesPerSecond;
        _framesPerSecondRecipe = framesPerSecond;
        _widthRecipe    = width;
        _heightRecipe   = height;
        _bitrate        = bitrate;
        _bitrateRecipe  = bitrate;
        _heightAndWidth = HeightAndWidth();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEncodingVariant(AnyEncodingVariant from) {
        _framesPerSecond       = from._framesPerSecond;
        _framesPerSecondRecipe = from._framesPerSecondRecipe;
        _heightAndWidth = from._heightAndWidth;
        _widthRecipe    = from._widthRecipe;
        _widthPin       = from._widthPin;
        _heightRecipe   = from._heightRecipe;
        _heightPin      = from._heightPin;
        _bitrate        = from._bitrate;
        _bitrateRecipe  = from._bitrateRecipe;
    }

    /// <summary>Draws <c>height</c> and <c>width</c> together, <c>height</c> never after <c>width</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<int> HeightAndWidth() {
        AnyOrderedPair<int> pair = Any.OrderedPair(_heightRecipe, _widthRecipe);

        if (_heightPin is { } height) { pair = pair.WithFirst(height); }
        if (_widthPin is { } width) { pair = pair.WithSecond(width); }

        return pair;
    }

    /// <summary>Pins the frame rate (the <c>fps</c> of the id) to <paramref name="value" />.</summary>
    public AnyEncodingVariant WithFramesPerSecond(int value) {
        AnyEncodingVariant copy = new AnyEncodingVariant(this);

        copy._framesPerSecond = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe the frame rate is drawn from through <paramref name="configure" />.</summary>
    public AnyEncodingVariant WithFramesPerSecond(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEncodingVariant copy = new AnyEncodingVariant(this);

        copy._framesPerSecondRecipe = configure(_framesPerSecondRecipe);
        copy._framesPerSecond       = copy._framesPerSecondRecipe;

        return copy;
    }

    /// <summary>Pins <c>width</c> to a fixed value, which bounds <c>height</c>; its recipe is kept for a later refinement.</summary>
    public AnyEncodingVariant WithWidth(int value) {
        AnyEncodingVariant copy = new AnyEncodingVariant(this);

        copy._widthPin       = value;
        copy._heightAndWidth = copy.HeightAndWidth();

        return copy;
    }

    /// <summary>Refines the recipe <c>width</c> is drawn from — <c>AnyValidWidth()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>height</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEncodingVariant WithWidth(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEncodingVariant copy = new AnyEncodingVariant(this);

        copy._widthRecipe    = configure(_widthRecipe);
        copy._widthPin       = null;
        copy._heightAndWidth = copy.HeightAndWidth();

        return copy;
    }

    /// <summary>Pins <c>height</c> to a fixed value, which bounds <c>width</c>; its recipe is kept for a later refinement.</summary>
    public AnyEncodingVariant WithHeight(int value) {
        AnyEncodingVariant copy = new AnyEncodingVariant(this);

        copy._heightPin      = value;
        copy._heightAndWidth = copy.HeightAndWidth();

        return copy;
    }

    /// <summary>Refines the recipe <c>height</c> is drawn from — <c>AnyValidHeight()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>width</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEncodingVariant WithHeight(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEncodingVariant copy = new AnyEncodingVariant(this);

        copy._heightRecipe   = configure(_heightRecipe);
        copy._heightPin      = null;
        copy._heightAndWidth = copy.HeightAndWidth();

        return copy;
    }

    /// <summary>Pins <c>bitrate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEncodingVariant WithBitrate(Bitrate value) {
        AnyEncodingVariant copy = new AnyEncodingVariant(this);

        copy._bitrate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bitrate</c> is drawn from — <c>AnyValidBitrate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBitrate, AnyBitrate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEncodingVariant WithBitrate(Func<AnyBitrate, AnyBitrate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEncodingVariant copy = new AnyEncodingVariant(this);

        copy._bitrateRecipe = configure(_bitrateRecipe);
        copy._bitrate       = copy._bitrateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="EncodingVariant" />.</summary>
    public EncodingVariant Generate() {
        (int height, int width) = _heightAndWidth.Generate();

        return new EncodingVariant($"{height}p{_framesPerSecond.Generate()}",
                                   width,
                                   height,
                                   _bitrate.Generate());
    }

}
