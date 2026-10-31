// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RouteLock --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RouteLock" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRouteLock : IAny<RouteLock> {

    private readonly IAny<Route>          _route;
    private readonly IAny<TrainId>        _train;
    private readonly IAny<DateTimeOffset> _lockedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRouteLock()
        : this(route:    AnyValidRoute(),
               train:    AnyValidTrain(),
               lockedAt: AnyValidLockedAt()) { }

    private static AnyRoute AnyValidRoute() {
        return new AnyRoute();
    }

    private static AnyTrainId AnyValidTrain() {
        return new AnyTrainId();
    }

    private static AnyDateTimeOffset AnyValidLockedAt() {
        return Any.DateTimeOffset().After(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private AnyRouteLock(IAny<Route>          route,
                         IAny<TrainId>        train,
                         IAny<DateTimeOffset> lockedAt) {
        _route    = route;
        _train    = train;
        _lockedAt = lockedAt;
    }

    /// <summary>Pins <c>route</c> to a fixed value.</summary>
    public AnyRouteLock WithRoute(Route value) {
        return new AnyRouteLock(new FixedValue<Route>(value), _train, _lockedAt);
    }

    /// <summary>Draws <c>route</c> from <c>AnyValidRoute()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRouteLock WithRoute(Func<AnyRoute, AnyRoute> configure) {
        return new AnyRouteLock(configure(AnyValidRoute()), _train, _lockedAt);
    }

    /// <summary>Pins <c>train</c> to a fixed value.</summary>
    public AnyRouteLock WithTrain(TrainId value) {
        return new AnyRouteLock(_route, new FixedValue<TrainId>(value), _lockedAt);
    }

    /// <summary>Draws <c>train</c> from <c>AnyValidTrain()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRouteLock WithTrain(Func<AnyTrainId, AnyTrainId> configure) {
        return new AnyRouteLock(_route, configure(AnyValidTrain()), _lockedAt);
    }

    /// <summary>Pins <c>lockedAt</c> to a fixed value.</summary>
    public AnyRouteLock WithLockedAt(DateTimeOffset value) {
        return new AnyRouteLock(_route, _train, new FixedValue<DateTimeOffset>(value));
    }

    /// <summary>Draws <c>lockedAt</c> from <c>AnyValidLockedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRouteLock WithLockedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnyRouteLock(_route, _train, configure(AnyValidLockedAt()));
    }

    /// <summary>Produces one arbitrary <see cref="RouteLock" />.</summary>
    public RouteLock Generate() {
        return new RouteLock(_route.Generate(),
                             _train.Generate(),
                             _lockedAt.Generate());
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
