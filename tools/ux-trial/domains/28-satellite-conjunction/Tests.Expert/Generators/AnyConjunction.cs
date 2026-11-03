// Scaffolded by `any` (JustDummies), then rewritten by hand: the constructor ties its four parameters together
// (an active primary, two different objects, a time of closest approach inside both trajectories), which no
// per-parameter recipe can hold, so Generate() draws them in the order the rules need. `any generate Conjunction --force`
// overwrites it.

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>
///     A generator of arbitrary, freshly screened <see cref="Conjunction" /> values: an active payload as the primary,
///     any other object as the secondary, and a time of closest approach that both trajectories cover.
/// </summary>
public sealed partial class AnyConjunction : IAny<Conjunction> {

    private IAny<Guid>  _id;
    private IAny<Epoch> _timeOfClosestApproach;

    public AnyConjunction()
        : this(id: Any.Guid().NonEmpty(),
               timeOfClosestApproach: new AnyEpoch().WithUtc(utc => utc.Between(Epoch.SpaceAgeStart.AddDays(1), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)))) { }

    private AnyConjunction(IAny<Guid> id, IAny<Epoch> timeOfClosestApproach) {
        _id                    = id;
        _timeOfClosestApproach = timeOfClosestApproach;
    }

    private AnyConjunction(AnyConjunction from) {
        _id                    = from._id;
        _timeOfClosestApproach = from._timeOfClosestApproach;
    }

    /// <summary>Pins the time of closest approach: the trajectories are then drawn around it.</summary>
    public AnyConjunction WithTimeOfClosestApproach(Epoch value) {
        AnyConjunction copy = new AnyConjunction(this);

        copy._timeOfClosestApproach = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Conjunction" />.</summary>
    public Conjunction Generate() {
        Guid  id  = _id.Generate();
        Epoch tca = _timeOfClosestApproach.Generate();

        Trajectory primary = new AnyTrajectory().Covering(tca)
                                                .WithSpaceObject(o => o.Active())
                                                .Generate();

        // Another catalogue number is the whole of "a different object": the kind and the rest stay arbitrary.
        Trajectory secondary = new AnyTrajectory().Covering(tca)
                                                  .WithSpaceObject(o => o.WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)))
                                                  .Generate();

        return new Conjunction(id, primary, secondary, tca);
    }

}
