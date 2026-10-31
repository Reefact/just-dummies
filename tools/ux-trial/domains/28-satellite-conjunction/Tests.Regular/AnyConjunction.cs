// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Conjunction --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Conjunction" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyConjunction : IAny<Conjunction> {

    private readonly IAny<Guid>       _id;
    private readonly IAny<Trajectory> _primary;
    private readonly IAny<Trajectory> _secondary;
    private readonly IAny<Epoch>      _timeOfClosestApproach;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyConjunction()
        : this(id:                    AnyValidId(),
               primary:               AnyValidPrimary(),
               secondary:             AnyValidSecondary(),
               timeOfClosestApproach: AnyValidTimeOfClosestApproach()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyTrajectory AnyValidPrimary() {
        return new AnyTrajectory().WithSpaceObject(o => o.WithKind(ObjectKind.Payload).WithIsActive(true));
    }

    private static AnyTrajectory AnyValidSecondary() {
        return new AnyTrajectory();
    }

    private static AnyEpoch AnyValidTimeOfClosestApproach() {
        return new AnyEpoch();
    }

    private AnyConjunction(IAny<Guid>       id,
                           IAny<Trajectory> primary,
                           IAny<Trajectory> secondary,
                           IAny<Epoch>      timeOfClosestApproach) {
        _id                    = id;
        _primary               = primary;
        _secondary             = secondary;
        _timeOfClosestApproach = timeOfClosestApproach;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyConjunction WithId(Guid value) {
        return new AnyConjunction(new FixedValue<Guid>(value), _primary, _secondary, _timeOfClosestApproach);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyConjunction WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyConjunction(configure(AnyValidId()), _primary, _secondary, _timeOfClosestApproach);
    }

    /// <summary>Pins <c>primary</c> to a fixed value.</summary>
    public AnyConjunction WithPrimary(Trajectory value) {
        return new AnyConjunction(_id, new FixedValue<Trajectory>(value), _secondary, _timeOfClosestApproach);
    }

    /// <summary>Draws <c>primary</c> from <c>AnyValidPrimary()</c>, refined by <paramref name="configure" />.</summary>
    public AnyConjunction WithPrimary(Func<AnyTrajectory, AnyTrajectory> configure) {
        return new AnyConjunction(_id, configure(AnyValidPrimary()), _secondary, _timeOfClosestApproach);
    }

    /// <summary>Pins <c>secondary</c> to a fixed value.</summary>
    public AnyConjunction WithSecondary(Trajectory value) {
        return new AnyConjunction(_id, _primary, new FixedValue<Trajectory>(value), _timeOfClosestApproach);
    }

    /// <summary>Draws <c>secondary</c> from <c>AnyValidSecondary()</c>, refined by <paramref name="configure" />.</summary>
    public AnyConjunction WithSecondary(Func<AnyTrajectory, AnyTrajectory> configure) {
        return new AnyConjunction(_id, _primary, configure(AnyValidSecondary()), _timeOfClosestApproach);
    }

    /// <summary>Pins <c>timeOfClosestApproach</c> to a fixed value.</summary>
    public AnyConjunction WithTimeOfClosestApproach(Epoch value) {
        return new AnyConjunction(_id, _primary, _secondary, new FixedValue<Epoch>(value));
    }

    /// <summary>Draws <c>timeOfClosestApproach</c> from <c>AnyValidTimeOfClosestApproach()</c>, refined by <paramref name="configure" />.</summary>
    public AnyConjunction WithTimeOfClosestApproach(Func<AnyEpoch, AnyEpoch> configure) {
        return new AnyConjunction(_id, _primary, _secondary, configure(AnyValidTimeOfClosestApproach()));
    }

    /// <summary>Produces one arbitrary <see cref="Conjunction" />.</summary>
    public Conjunction Generate() {
        // Both trajectories must cover the TCA and name two different objects: rules between parameters.
        // A drawn trajectory is drawn around the TCA; a pinned one is taken as given.
        Epoch      timeOfClosestApproach = _timeOfClosestApproach.Generate();
        Trajectory primary               = Around(_primary, timeOfClosestApproach).Generate();
        Trajectory secondary             = Around(_secondary, timeOfClosestApproach).Generate();
        for (int attempt = 0; attempt < 10 && secondary.SpaceObject.CatalogNumber == primary.SpaceObject.CatalogNumber; attempt++) {
            secondary = Around(_secondary, timeOfClosestApproach).Generate();
        }

        return new Conjunction(_id.Generate(), primary, secondary, timeOfClosestApproach);
    }

    private static IAny<Trajectory> Around(IAny<Trajectory> trajectory, Epoch epoch) {
        return trajectory is AnyTrajectory drawn ? drawn.Covering(epoch) : trajectory;
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
