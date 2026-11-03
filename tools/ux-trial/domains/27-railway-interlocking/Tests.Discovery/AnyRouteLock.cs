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

    private IAny<Route>          _route;
    private IAny<TrainId>        _train;
    private IAny<DateTimeOffset> _lockedAt;

    private AnyRoute          _routeRecipe;
    private AnyTrainId        _trainRecipe;
    private AnyDateTimeOffset _lockedAtRecipe;

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
        return Any.DateTimeOffset().DifferentFrom(default);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRouteLock(AnyRoute          route,
                         AnyTrainId        train,
                         AnyDateTimeOffset lockedAt) {
        _route          = route;
        _routeRecipe    = route;
        _train          = train;
        _trainRecipe    = train;
        _lockedAt       = lockedAt;
        _lockedAtRecipe = lockedAt;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRouteLock(AnyRouteLock from) {
        _route          = from._route;
        _routeRecipe    = from._routeRecipe;
        _train          = from._train;
        _trainRecipe    = from._trainRecipe;
        _lockedAt       = from._lockedAt;
        _lockedAtRecipe = from._lockedAtRecipe;
    }

    /// <summary>Pins <c>route</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRouteLock WithRoute(Route value) {
        AnyRouteLock copy = new AnyRouteLock(this);

        copy._route = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>route</c> is drawn from — <c>AnyValidRoute()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRoute, AnyRoute&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRouteLock WithRoute(Func<AnyRoute, AnyRoute> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRouteLock copy = new AnyRouteLock(this);

        copy._routeRecipe = configure(_routeRecipe);
        copy._route       = copy._routeRecipe;

        return copy;
    }

    /// <summary>Pins <c>train</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRouteLock WithTrain(TrainId value) {
        AnyRouteLock copy = new AnyRouteLock(this);

        copy._train = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>train</c> is drawn from — <c>AnyValidTrain()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTrainId, AnyTrainId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRouteLock WithTrain(Func<AnyTrainId, AnyTrainId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRouteLock copy = new AnyRouteLock(this);

        copy._trainRecipe = configure(_trainRecipe);
        copy._train       = copy._trainRecipe;

        return copy;
    }

    /// <summary>Pins <c>lockedAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRouteLock WithLockedAt(DateTimeOffset value) {
        AnyRouteLock copy = new AnyRouteLock(this);

        copy._lockedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lockedAt</c> is drawn from — <c>AnyValidLockedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRouteLock WithLockedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRouteLock copy = new AnyRouteLock(this);

        copy._lockedAtRecipe = configure(_lockedAtRecipe);
        copy._lockedAt       = copy._lockedAtRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RouteLock" />.</summary>
    public RouteLock Generate() {
        return new RouteLock(_route.Generate(),
                             _train.Generate(),
                             _lockedAt.Generate());
    }

}
