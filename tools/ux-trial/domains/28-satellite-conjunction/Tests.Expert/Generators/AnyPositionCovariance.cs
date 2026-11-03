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
    private IAny<double> _xyCorrelation;
    private IAny<double> _xzCorrelation;
    private IAny<double> _yzCorrelation;

    private AnyDouble _xxRecipe;
    private AnyDouble _yyRecipe;
    private AnyDouble _zzRecipe;
    private AnyDouble _xyCorrelationRecipe;
    private AnyDouble _xzCorrelationRecipe;
    private AnyDouble _yzCorrelationRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPositionCovariance()
        : this(xx: AnyValidXx(),
               yy: AnyValidYy(),
               zz: AnyValidZz(),
               xy: AnyValidXyCorrelation(),
               xz: AnyValidXzCorrelation(),
               yz: AnyValidYzCorrelation()) { }

    private static AnyDouble AnyValidXx() {
        // A variance in km², positive and at most the square of the 1000 km sigma the domain calls "lost".
        return Any.Double().Positive().LessThanOrEqualTo(PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm);
    }

    private static AnyDouble AnyValidYy() {
        // A variance in km², positive and at most the square of the 1000 km sigma the domain calls "lost".
        return Any.Double().Positive().LessThanOrEqualTo(PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm);
    }

    private static AnyDouble AnyValidZz() {
        // A variance in km², positive and at most the square of the 1000 km sigma the domain calls "lost".
        return Any.Double().Positive().LessThanOrEqualTo(PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm);
    }

    private static AnyDouble AnyValidXyCorrelation() {
        // A CORRELATION, not a covariance: the cross term is derived from it in Generate(). Staying within +/-0.4 keeps
        // every off-diagonal pair below 0.8, so the unit-diagonal matrix is diagonally dominant and thus positive definite.
        return Any.Double().Between(-0.4, 0.4);
    }

    private static AnyDouble AnyValidXzCorrelation() {
        // A CORRELATION, not a covariance: the cross term is derived from it in Generate(). Staying within +/-0.4 keeps
        // every off-diagonal pair below 0.8, so the unit-diagonal matrix is diagonally dominant and thus positive definite.
        return Any.Double().Between(-0.4, 0.4);
    }

    private static AnyDouble AnyValidYzCorrelation() {
        // A CORRELATION, not a covariance: the cross term is derived from it in Generate(). Staying within +/-0.4 keeps
        // every off-diagonal pair below 0.8, so the unit-diagonal matrix is diagonally dominant and thus positive definite.
        return Any.Double().Between(-0.4, 0.4);
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
        _xyCorrelation       = xy;
        _xyCorrelationRecipe = xy;
        _xzCorrelation       = xz;
        _xzCorrelationRecipe = xz;
        _yzCorrelation       = yz;
        _yzCorrelationRecipe = yz;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPositionCovariance(AnyPositionCovariance from) {
        _xx       = from._xx;
        _xxRecipe = from._xxRecipe;
        _yy       = from._yy;
        _yyRecipe = from._yyRecipe;
        _zz       = from._zz;
        _zzRecipe = from._zzRecipe;
        _xyCorrelation       = from._xyCorrelation;
        _xyCorrelationRecipe = from._xyCorrelationRecipe;
        _xzCorrelation       = from._xzCorrelation;
        _xzCorrelationRecipe = from._xzCorrelationRecipe;
        _yzCorrelation       = from._yzCorrelation;
        _yzCorrelationRecipe = from._yzCorrelationRecipe;
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

        copy._xyCorrelation = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>xy</c> is drawn from — <c>AnyValidXyCorrelation()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithXyCorrelation(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xyCorrelationRecipe = configure(_xyCorrelationRecipe);
        copy._xyCorrelation       = copy._xyCorrelationRecipe;

        return copy;
    }

    /// <summary>Pins <c>xz</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPositionCovariance WithXzCorrelation(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xzCorrelation = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>xz</c> is drawn from — <c>AnyValidXzCorrelation()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithXzCorrelation(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xzCorrelationRecipe = configure(_xzCorrelationRecipe);
        copy._xzCorrelation       = copy._xzCorrelationRecipe;

        return copy;
    }

    /// <summary>Pins <c>yz</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPositionCovariance WithYzCorrelation(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._yzCorrelation = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>yz</c> is drawn from — <c>AnyValidYzCorrelation()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPositionCovariance WithYzCorrelation(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._yzCorrelationRecipe = configure(_yzCorrelationRecipe);
        copy._yzCorrelation       = copy._yzCorrelationRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PositionCovariance" />.</summary>
    public PositionCovariance Generate() {
        double xx = _xx.Generate();
        double yy = _yy.Generate();
        double zz = _zz.Generate();

        // The cross terms are drawn as correlations and scaled by the variances they relate, which is how the matrix
        // stays positive definite whatever the variances: the constructor's guard ties all six parameters together.
        return new PositionCovariance(xx,
                                      yy,
                                      zz,
                                      _xyCorrelation.Generate() * Math.Sqrt(xx * yy),
                                      _xzCorrelation.Generate() * Math.Sqrt(xx * zz),
                                      _yzCorrelation.Generate() * Math.Sqrt(yy * zz));
    }

}
