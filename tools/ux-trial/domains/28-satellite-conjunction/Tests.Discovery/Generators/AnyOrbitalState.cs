// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OrbitalState --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="OrbitalState" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrbitalState : IAny<OrbitalState> {

    private IAny<Epoch>              _epoch;
    private IAny<Vector3>            _positionKm;
    private IAny<Vector3>            _velocityKmPerSecond;
    private IAny<PositionCovariance> _covariance;

    private AnyEpoch              _epochRecipe;
    private IAny<Vector3>            _positionKmRecipe;
    private IAny<Vector3>            _velocityKmPerSecondRecipe;
    private AnyPositionCovariance _covarianceRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrbitalState()
        : this(epoch:               AnyValidEpoch(),
               positionKm:          AnyValidPositionKm(),
               velocityKmPerSecond: AnyValidVelocityKmPerSecond(),
               covariance:          AnyValidCovariance()) { }

    private static AnyEpoch AnyValidEpoch() {
        return new AnyEpoch();
    }

    private static IAny<Vector3> AnyValidPositionKm() {
        return Orbits.PositionKm();
    }

    private static IAny<Vector3> AnyValidVelocityKmPerSecond() {
        return Orbits.VelocityKmPerSecond();
    }

    private static AnyPositionCovariance AnyValidCovariance() {
        return new AnyPositionCovariance();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOrbitalState(AnyEpoch              epoch,
                            IAny<Vector3>            positionKm,
                            IAny<Vector3>            velocityKmPerSecond,
                            AnyPositionCovariance covariance) {
        _epoch                     = epoch;
        _epochRecipe               = epoch;
        _positionKm                = positionKm;
        _positionKmRecipe          = positionKm;
        _velocityKmPerSecond       = velocityKmPerSecond;
        _velocityKmPerSecondRecipe = velocityKmPerSecond;
        _covariance                = covariance;
        _covarianceRecipe          = covariance;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOrbitalState(AnyOrbitalState from) {
        _epoch                     = from._epoch;
        _epochRecipe               = from._epochRecipe;
        _positionKm                = from._positionKm;
        _positionKmRecipe          = from._positionKmRecipe;
        _velocityKmPerSecond       = from._velocityKmPerSecond;
        _velocityKmPerSecondRecipe = from._velocityKmPerSecondRecipe;
        _covariance                = from._covariance;
        _covarianceRecipe          = from._covarianceRecipe;
    }

    /// <summary>Pins <c>epoch</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrbitalState WithEpoch(Epoch value) {
        AnyOrbitalState copy = new AnyOrbitalState(this);

        copy._epoch = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>epoch</c> is drawn from — <c>AnyValidEpoch()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEpoch, AnyEpoch&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrbitalState WithEpoch(Func<AnyEpoch, AnyEpoch> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrbitalState copy = new AnyOrbitalState(this);

        copy._epochRecipe = configure(_epochRecipe);
        copy._epoch       = copy._epochRecipe;

        return copy;
    }

    /// <summary>Pins <c>positionKm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrbitalState WithPositionKm(Vector3 value) {
        AnyOrbitalState copy = new AnyOrbitalState(this);

        copy._positionKm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>positionKm</c> is drawn from — <c>AnyValidPositionKm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny&lt;Vector3&gt;, IAny&lt;Vector3&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrbitalState WithPositionKm(Func<IAny<Vector3>, IAny<Vector3>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrbitalState copy = new AnyOrbitalState(this);

        copy._positionKmRecipe = configure(_positionKmRecipe);
        copy._positionKm       = copy._positionKmRecipe;

        return copy;
    }

    /// <summary>Pins <c>velocityKmPerSecond</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrbitalState WithVelocityKmPerSecond(Vector3 value) {
        AnyOrbitalState copy = new AnyOrbitalState(this);

        copy._velocityKmPerSecond = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>velocityKmPerSecond</c> is drawn from — <c>AnyValidVelocityKmPerSecond()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny&lt;Vector3&gt;, IAny&lt;Vector3&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrbitalState WithVelocityKmPerSecond(Func<IAny<Vector3>, IAny<Vector3>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrbitalState copy = new AnyOrbitalState(this);

        copy._velocityKmPerSecondRecipe = configure(_velocityKmPerSecondRecipe);
        copy._velocityKmPerSecond       = copy._velocityKmPerSecondRecipe;

        return copy;
    }

    /// <summary>Pins <c>covariance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrbitalState WithCovariance(PositionCovariance value) {
        AnyOrbitalState copy = new AnyOrbitalState(this);

        copy._covariance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>covariance</c> is drawn from — <c>AnyValidCovariance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPositionCovariance, AnyPositionCovariance&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrbitalState WithCovariance(Func<AnyPositionCovariance, AnyPositionCovariance> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrbitalState copy = new AnyOrbitalState(this);

        copy._covarianceRecipe = configure(_covarianceRecipe);
        copy._covariance       = copy._covarianceRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="OrbitalState" />.</summary>
    public OrbitalState Generate() {
        return new OrbitalState(_epoch.Generate(),
                                _positionKm.Generate(),
                                _velocityKmPerSecond.Generate(),
                                _covariance.Generate());
    }

}
