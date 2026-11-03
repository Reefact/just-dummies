// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TranslationTransform --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="TranslationTransform" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTranslationTransform : IAny<TranslationTransform> {

    private IAny<double> _dxMm;
    private IAny<double> _dyMm;
    private IAny<double> _dzMm;
    private IAny<double> _residualErrorMm;

    private AnyDouble _dxMmRecipe;
    private AnyDouble _dyMmRecipe;
    private AnyDouble _dzMmRecipe;
    private AnyDouble _residualErrorMmRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTranslationTransform()
        : this(dxMm:            AnyValidDxMm(),
               dyMm:            AnyValidDyMm(),
               dzMm:            AnyValidDzMm(),
               residualErrorMm: AnyValidResidualErrorMm()) { }

    private static AnyDouble AnyValidDxMm() {
        return Any.Double().Between(-300d, 300d);
    }

    private static AnyDouble AnyValidDyMm() {
        return Any.Double().Between(-300d, 300d);
    }

    private static AnyDouble AnyValidDzMm() {
        return Any.Double().Between(-300d, 300d);
    }

    private static AnyDouble AnyValidResidualErrorMm() {
        return Any.Double().Between(0d, TranslationTransform.MaxResidualErrorMm);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTranslationTransform(AnyDouble dxMm,
                                    AnyDouble dyMm,
                                    AnyDouble dzMm,
                                    AnyDouble residualErrorMm) {
        _dxMm                  = dxMm;
        _dxMmRecipe            = dxMm;
        _dyMm                  = dyMm;
        _dyMmRecipe            = dyMm;
        _dzMm                  = dzMm;
        _dzMmRecipe            = dzMm;
        _residualErrorMm       = residualErrorMm;
        _residualErrorMmRecipe = residualErrorMm;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTranslationTransform(AnyTranslationTransform from) {
        _dxMm                  = from._dxMm;
        _dxMmRecipe            = from._dxMmRecipe;
        _dyMm                  = from._dyMm;
        _dyMmRecipe            = from._dyMmRecipe;
        _dzMm                  = from._dzMm;
        _dzMmRecipe            = from._dzMmRecipe;
        _residualErrorMm       = from._residualErrorMm;
        _residualErrorMmRecipe = from._residualErrorMmRecipe;
    }

    /// <summary>Pins <c>dxMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTranslationTransform WithDxMm(double value) {
        AnyTranslationTransform copy = new AnyTranslationTransform(this);

        copy._dxMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dxMm</c> is drawn from — <c>AnyValidDxMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTranslationTransform WithDxMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTranslationTransform copy = new AnyTranslationTransform(this);

        copy._dxMmRecipe = configure(_dxMmRecipe);
        copy._dxMm       = copy._dxMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>dyMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTranslationTransform WithDyMm(double value) {
        AnyTranslationTransform copy = new AnyTranslationTransform(this);

        copy._dyMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dyMm</c> is drawn from — <c>AnyValidDyMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTranslationTransform WithDyMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTranslationTransform copy = new AnyTranslationTransform(this);

        copy._dyMmRecipe = configure(_dyMmRecipe);
        copy._dyMm       = copy._dyMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>dzMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTranslationTransform WithDzMm(double value) {
        AnyTranslationTransform copy = new AnyTranslationTransform(this);

        copy._dzMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dzMm</c> is drawn from — <c>AnyValidDzMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTranslationTransform WithDzMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTranslationTransform copy = new AnyTranslationTransform(this);

        copy._dzMmRecipe = configure(_dzMmRecipe);
        copy._dzMm       = copy._dzMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>residualErrorMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTranslationTransform WithResidualErrorMm(double value) {
        AnyTranslationTransform copy = new AnyTranslationTransform(this);

        copy._residualErrorMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>residualErrorMm</c> is drawn from — <c>AnyValidResidualErrorMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTranslationTransform WithResidualErrorMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTranslationTransform copy = new AnyTranslationTransform(this);

        copy._residualErrorMmRecipe = configure(_residualErrorMmRecipe);
        copy._residualErrorMm       = copy._residualErrorMmRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TranslationTransform" />.</summary>
    public TranslationTransform Generate() {
        return new TranslationTransform(_dxMm.Generate(),
                                        _dyMm.Generate(),
                                        _dzMm.Generate(),
                                        _residualErrorMm.Generate());
    }

}
