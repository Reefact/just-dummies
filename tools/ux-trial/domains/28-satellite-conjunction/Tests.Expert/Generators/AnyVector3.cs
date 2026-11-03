// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Vector3 --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Vector3" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVector3 : IAny<Vector3> {

    private IAny<double> _x;
    private IAny<double> _y;
    private IAny<double> _z;

    private AnyDouble _xRecipe;
    private AnyDouble _yRecipe;
    private AnyDouble _zRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVector3()
        : this(x: AnyValidX(),
               y: AnyValidY(),
               z: AnyValidZ()) { }

    private static AnyDouble AnyValidX() {
        // Vector3 only demands a finite component, and Any.Double() never draws NaN or an infinity.
        return Any.Double();
    }

    private static AnyDouble AnyValidY() {
        // Vector3 only demands a finite component, and Any.Double() never draws NaN or an infinity.
        return Any.Double();
    }

    private static AnyDouble AnyValidZ() {
        // Vector3 only demands a finite component, and Any.Double() never draws NaN or an infinity.
        return Any.Double();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVector3(AnyDouble x,
                       AnyDouble y,
                       AnyDouble z) {
        _x       = x;
        _xRecipe = x;
        _y       = y;
        _yRecipe = y;
        _z       = z;
        _zRecipe = z;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVector3(AnyVector3 from) {
        _x       = from._x;
        _xRecipe = from._xRecipe;
        _y       = from._y;
        _yRecipe = from._yRecipe;
        _z       = from._z;
        _zRecipe = from._zRecipe;
    }

    /// <summary>Pins <c>x</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVector3 WithX(double value) {
        AnyVector3 copy = new AnyVector3(this);

        copy._x = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>x</c> is drawn from — <c>AnyValidX()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVector3 WithX(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVector3 copy = new AnyVector3(this);

        copy._xRecipe = configure(_xRecipe);
        copy._x       = copy._xRecipe;

        return copy;
    }

    /// <summary>Pins <c>y</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVector3 WithY(double value) {
        AnyVector3 copy = new AnyVector3(this);

        copy._y = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>y</c> is drawn from — <c>AnyValidY()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVector3 WithY(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVector3 copy = new AnyVector3(this);

        copy._yRecipe = configure(_yRecipe);
        copy._y       = copy._yRecipe;

        return copy;
    }

    /// <summary>Pins <c>z</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVector3 WithZ(double value) {
        AnyVector3 copy = new AnyVector3(this);

        copy._z = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>z</c> is drawn from — <c>AnyValidZ()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVector3 WithZ(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVector3 copy = new AnyVector3(this);

        copy._zRecipe = configure(_zRecipe);
        copy._z       = copy._zRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Vector3" />.</summary>
    public Vector3 Generate() {
        return new Vector3(_x.Generate(),
                           _y.Generate(),
                           _z.Generate());
    }

}
