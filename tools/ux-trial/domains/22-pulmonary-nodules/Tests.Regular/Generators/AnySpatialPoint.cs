// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SpatialPoint --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SpatialPoint" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpatialPoint : IAny<SpatialPoint> {

    private IAny<double> _xMm;
    private IAny<double> _yMm;
    private IAny<double> _zMm;

    private AnyDouble _xMmRecipe;
    private AnyDouble _yMmRecipe;
    private AnyDouble _zMmRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpatialPoint()
        : this(xMm: AnyValidXMm(),
               yMm: AnyValidYMm(),
               zMm: AnyValidZMm()) { }

    private static AnyDouble AnyValidXMm() {
        return Any.Double().Between(-2000d, 2000d);
    }

    private static AnyDouble AnyValidYMm() {
        return Any.Double().Between(-2000d, 2000d);
    }

    private static AnyDouble AnyValidZMm() {
        return Any.Double().Between(-2000d, 2000d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySpatialPoint(AnyDouble xMm,
                            AnyDouble yMm,
                            AnyDouble zMm) {
        _xMm       = xMm;
        _xMmRecipe = xMm;
        _yMm       = yMm;
        _yMmRecipe = yMm;
        _zMm       = zMm;
        _zMmRecipe = zMm;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySpatialPoint(AnySpatialPoint from) {
        _xMm       = from._xMm;
        _xMmRecipe = from._xMmRecipe;
        _yMm       = from._yMm;
        _yMmRecipe = from._yMmRecipe;
        _zMm       = from._zMm;
        _zMmRecipe = from._zMmRecipe;
    }

    /// <summary>Pins <c>xMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpatialPoint WithXMm(double value) {
        AnySpatialPoint copy = new AnySpatialPoint(this);

        copy._xMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>xMm</c> is drawn from — <c>AnyValidXMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpatialPoint WithXMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpatialPoint copy = new AnySpatialPoint(this);

        copy._xMmRecipe = configure(_xMmRecipe);
        copy._xMm       = copy._xMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>yMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpatialPoint WithYMm(double value) {
        AnySpatialPoint copy = new AnySpatialPoint(this);

        copy._yMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>yMm</c> is drawn from — <c>AnyValidYMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpatialPoint WithYMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpatialPoint copy = new AnySpatialPoint(this);

        copy._yMmRecipe = configure(_yMmRecipe);
        copy._yMm       = copy._yMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>zMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpatialPoint WithZMm(double value) {
        AnySpatialPoint copy = new AnySpatialPoint(this);

        copy._zMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>zMm</c> is drawn from — <c>AnyValidZMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpatialPoint WithZMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpatialPoint copy = new AnySpatialPoint(this);

        copy._zMmRecipe = configure(_zMmRecipe);
        copy._zMm       = copy._zMmRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SpatialPoint" />.</summary>
    public SpatialPoint Generate() {
        return new SpatialPoint(_xMm.Generate(),
                                _yMm.Generate(),
                                _zMm.Generate());
    }

}
