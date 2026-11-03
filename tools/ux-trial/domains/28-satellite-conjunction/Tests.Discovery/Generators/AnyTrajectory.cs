// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Trajectory --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Trajectory" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTrajectory : IAny<Trajectory> {

    private IAny<SpaceObject>               _spaceObject;
    private IAny<IEnumerable<OrbitalState>> _states;

    private AnySpaceObject            _spaceObjectRecipe;
    private IAny<IEnumerable<OrbitalState>> _statesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTrajectory()
        : this(spaceObject: AnyValidSpaceObject(),
               states:      AnyValidStates()) { }

    private static AnySpaceObject AnyValidSpaceObject() {
        return new AnySpaceObject();
    }

    private static IAny<IEnumerable<OrbitalState>> AnyValidStates() {
        // States on one circular orbit, strictly increasing and at most ten minutes apart (see Orbits).
        return new AnyEpoch().As(anchor => Orbits.StatesAround(anchor, new AnyPositionCovariance().Combinable()));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTrajectory(AnySpaceObject            spaceObject,
                          IAny<IEnumerable<OrbitalState>> states) {
        _spaceObject       = spaceObject;
        _spaceObjectRecipe = spaceObject;
        _states            = states;
        _statesRecipe      = states;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTrajectory(AnyTrajectory from) {
        _spaceObject       = from._spaceObject;
        _spaceObjectRecipe = from._spaceObjectRecipe;
        _states            = from._states;
        _statesRecipe      = from._statesRecipe;
    }

    /// <summary>Pins <c>spaceObject</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrajectory WithSpaceObject(SpaceObject value) {
        AnyTrajectory copy = new AnyTrajectory(this);

        copy._spaceObject = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>spaceObject</c> is drawn from — <c>AnyValidSpaceObject()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySpaceObject, AnySpaceObject&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrajectory WithSpaceObject(Func<AnySpaceObject, AnySpaceObject> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrajectory copy = new AnyTrajectory(this);

        copy._spaceObjectRecipe = configure(_spaceObjectRecipe);
        copy._spaceObject       = copy._spaceObjectRecipe;

        return copy;
    }

    /// <summary>Pins <c>states</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrajectory WithStates(IEnumerable<OrbitalState> value) {
        AnyTrajectory copy = new AnyTrajectory(this);

        copy._states = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>states</c> is drawn from — <c>AnyValidStates()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny&lt;IEnumerable&lt;OrbitalState&gt;&gt;, IAny&lt;IEnumerable&lt;OrbitalState&gt;&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrajectory WithStates(Func<IAny<IEnumerable<OrbitalState>>, IAny<IEnumerable<OrbitalState>>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrajectory copy = new AnyTrajectory(this);

        copy._statesRecipe = configure(_statesRecipe);
        copy._states       = copy._statesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Trajectory" />.</summary>
    public Trajectory Generate() {
        return new Trajectory(_spaceObject.Generate(),
                              _states.Generate());
    }

}
