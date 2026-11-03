// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PositionCovariance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PositionCovariance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPositionCovariance : IAny<PositionCovariance> {

    private IAny<double> _xx;
    private IAny<double> _yy;
    private IAny<double> _zz;
    private IAny<double> _xy;
    private IAny<double> _xz;
    private IAny<double> _yz;

    private AnyDouble _xxRecipe;
    private AnyDouble _yyRecipe;
    private AnyDouble _zzRecipe;
    private AnyDouble _xyRecipe;
    private AnyDouble _xzRecipe;
    private AnyDouble _yzRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPositionCovariance()
        : this(xx: AnyValidXx(),
               yy: AnyValidYy(),
               zz: AnyValidZz(),
               xy: AnyValidXy(),
               xz: AnyValidXz(),
               yz: AnyValidYz()) { }

    private static AnyDouble AnyValidXx() {
        // A variance lies in (0, MaximumSigmaKm squared]; the lower bound keeps the determinant clear of underflow.
        return Any.Double().Between(1e-4, PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm);
    }

    private static AnyDouble AnyValidYy() {
        // A variance lies in (0, MaximumSigmaKm squared]; the lower bound keeps the determinant clear of underflow.
        return Any.Double().Between(1e-4, PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm);
    }

    private static AnyDouble AnyValidZz() {
        // A variance lies in (0, MaximumSigmaKm squared]; the lower bound keeps the determinant clear of underflow.
        return Any.Double().Between(1e-4, PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm);
    }

    private static AnyDouble AnyValidXy() {
        // A CORRELATION coefficient, not a covariance: Generate() scales it by the two standard deviations.
        // |rho| <= 0.3 on each pair makes the correlation matrix diagonally dominant, hence positive definite.
        return Any.Double().Between(-0.3, 0.3);
    }

    private static AnyDouble AnyValidXz() {
        // A CORRELATION coefficient, not a covariance: Generate() scales it by the two standard deviations.
        // |rho| <= 0.3 on each pair makes the correlation matrix diagonally dominant, hence positive definite.
        return Any.Double().Between(-0.3, 0.3);
    }

    private static AnyDouble AnyValidYz() {
        // A CORRELATION coefficient, not a covariance: Generate() scales it by the two standard deviations.
        // |rho| <= 0.3 on each pair makes the correlation matrix diagonally dominant, hence positive definite.
        return Any.Double().Between(-0.3, 0.3);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPositionCovariance(AnyDouble xx,
                                  AnyDouble yy,
                                  AnyDouble zz,
                                  AnyDouble xy,
                                  AnyDouble xz,
                                  AnyDouble yz) {
        _xx       = xx;
        _xxRecipe = xx;
        _yy       = yy;
        _yyRecipe = yy;
        _zz       = zz;
        _zzRecipe = zz;
        _xy       = xy;
        _xyRecipe = xy;
        _xz       = xz;
        _xzRecipe = xz;
        _yz       = yz;
        _yzRecipe = yz;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPositionCovariance(AnyPositionCovariance from) {
        _xx       = from._xx;
        _xxRecipe = from._xxRecipe;
        _yy       = from._yy;
        _yyRecipe = from._yyRecipe;
        _zz       = from._zz;
        _zzRecipe = from._zzRecipe;
        _xy       = from._xy;
        _xyRecipe = from._xyRecipe;
        _xz       = from._xz;
        _xzRecipe = from._xzRecipe;
        _yz       = from._yz;
        _yzRecipe = from._yzRecipe;
    }

    /// <summary>Pins <c>xx</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPositionCovariance WithXx(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xx = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>xx</c> is drawn from — <c>AnyValidXx()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithXx(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xxRecipe = configure(_xxRecipe);
        copy._xx       = copy._xxRecipe;

        return copy;
    }

    /// <summary>Pins <c>yy</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPositionCovariance WithYy(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._yy = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>yy</c> is drawn from — <c>AnyValidYy()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithYy(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._yyRecipe = configure(_yyRecipe);
        copy._yy       = copy._yyRecipe;

        return copy;
    }

    /// <summary>Pins <c>zz</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPositionCovariance WithZz(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._zz = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>zz</c> is drawn from — <c>AnyValidZz()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithZz(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._zzRecipe = configure(_zzRecipe);
        copy._zz       = copy._zzRecipe;

        return copy;
    }

    /// <summary>Pins <c>xy</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPositionCovariance WithXyCorrelation(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xy = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>xy</c> is drawn from — <c>AnyValidXy()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithXyCorrelation(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xyRecipe = configure(_xyRecipe);
        copy._xy       = copy._xyRecipe;

        return copy;
    }

    /// <summary>Pins <c>xz</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPositionCovariance WithXzCorrelation(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xz = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>xz</c> is drawn from — <c>AnyValidXz()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithXzCorrelation(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xzRecipe = configure(_xzRecipe);
        copy._xz       = copy._xzRecipe;

        return copy;
    }

    /// <summary>Pins <c>yz</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPositionCovariance WithYzCorrelation(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._yz = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>yz</c> is drawn from — <c>AnyValidYz()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithYzCorrelation(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._yzRecipe = configure(_yzRecipe);
        copy._yz       = copy._yzRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PositionCovariance" />.</summary>
    public PositionCovariance Generate() {
        double xx = _xx.Generate();
        double yy = _yy.Generate();
        double zz = _zz.Generate();

        return new PositionCovariance(xx,
                                      yy,
                                      zz,
                                      _xy.Generate() * Math.Sqrt(xx * yy),
                                      _xz.Generate() * Math.Sqrt(xx * zz),
                                      _yz.Generate() * Math.Sqrt(yy * zz));
    }

}
