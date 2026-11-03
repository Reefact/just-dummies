// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ExactDecimal --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Numerics;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ExactDecimal" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyExactDecimal : IAny<ExactDecimal> {

    private IAny<BigInteger> _mantissa;
    private IAny<int>        _scale;

    private AnyBigInteger _mantissaRecipe;
    private AnyInt32      _scaleRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExactDecimal()
        : this(mantissa: AnyValidMantissa(),
               scale:    AnyValidScale()) { }

    private static AnyBigInteger AnyValidMantissa() {
        return new AnyBigInteger();
    }

    private static AnyInt32 AnyValidScale() {
        return Any.Int32().Between(0, ExactDecimal.MaxScale);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyExactDecimal(AnyBigInteger mantissa,
                            AnyInt32      scale) {
        _mantissa       = mantissa;
        _mantissaRecipe = mantissa;
        _scale          = scale;
        _scaleRecipe    = scale;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyExactDecimal(AnyExactDecimal from) {
        _mantissa       = from._mantissa;
        _mantissaRecipe = from._mantissaRecipe;
        _scale          = from._scale;
        _scaleRecipe    = from._scaleRecipe;
    }

    /// <summary>Pins <c>mantissa</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExactDecimal WithMantissa(BigInteger value) {
        AnyExactDecimal copy = new AnyExactDecimal(this);

        copy._mantissa = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>mantissa</c> is drawn from — <c>AnyValidMantissa()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBigInteger, AnyBigInteger&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExactDecimal WithMantissa(Func<AnyBigInteger, AnyBigInteger> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExactDecimal copy = new AnyExactDecimal(this);

        copy._mantissaRecipe = configure(_mantissaRecipe);
        copy._mantissa       = copy._mantissaRecipe;

        return copy;
    }

    /// <summary>Pins <c>scale</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExactDecimal WithScale(int value) {
        AnyExactDecimal copy = new AnyExactDecimal(this);

        copy._scale = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>scale</c> is drawn from — <c>AnyValidScale()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExactDecimal WithScale(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExactDecimal copy = new AnyExactDecimal(this);

        copy._scaleRecipe = configure(_scaleRecipe);
        copy._scale       = copy._scaleRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ExactDecimal" />.</summary>
    public ExactDecimal Generate() {
        return new ExactDecimal(_mantissa.Generate(),
                                _scale.Generate());
    }

}
