// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Leg --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Leg" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLeg : IAny<Leg> {

    private IAny<UnLocode> _origin;
    private IAny<UnLocode> _destination;

    private AnyUnLocode       _originRecipe;
    private AnyUnLocode       _destinationRecipe;
    private AnyDateTimeOffset _departureRecipe;
    private AnyTimeSpan       _durationRecipe;
    private AnyTimeSpan       _arrivalOffsetRecipe;

    private DateTimeOffset? _departurePin;
    private DateTimeOffset? _arrivalPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLeg()
        : this(origin:        new AnyUnLocode(),
               destination:   new AnyUnLocode(),
               departure:     AnyValidDeparture(),
               duration:      AnyValidDuration(),
               arrivalOffset: AnyValidOffset()) { }

    private static AnyDateTimeOffset AnyValidDeparture() {
        // Each port keeps its own clock: the draw varies the offset too, so code comparing local times shows up.
        return Any.DateTimeOffset()
                  .Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero))
                  .WithOffsetBetween(TimeSpan.FromHours(-12), TimeSpan.FromHours(14));
    }

    // The 90 days a leg may last are the constructor's, not the test's: a duration is drawn rather than a second instant.
    private static AnyTimeSpan AnyValidDuration() {
        return Any.TimeSpan().Between(TimeSpan.FromMinutes(1), Leg.MaxTransit).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private static AnyTimeSpan AnyValidOffset() {
        return Any.TimeSpan().Between(TimeSpan.FromHours(-12), TimeSpan.FromHours(14)).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private AnyLeg(AnyUnLocode       origin,
                   AnyUnLocode       destination,
                   AnyDateTimeOffset departure,
                   AnyTimeSpan       duration,
                   AnyTimeSpan       arrivalOffset) {
        _origin              = origin;
        _originRecipe        = origin;
        _destination         = destination;
        _destinationRecipe   = destination;
        _departureRecipe     = departure;
        _durationRecipe      = duration;
        _arrivalOffsetRecipe = arrivalOffset;
    }

    private AnyLeg(AnyLeg from) {
        _origin              = from._origin;
        _originRecipe        = from._originRecipe;
        _destination         = from._destination;
        _destinationRecipe   = from._destinationRecipe;
        _departureRecipe     = from._departureRecipe;
        _departurePin        = from._departurePin;
        _arrivalPin          = from._arrivalPin;
        _durationRecipe      = from._durationRecipe;
        _arrivalOffsetRecipe = from._arrivalOffsetRecipe;
    }

    public AnyLeg WithOrigin(UnLocode value) {
        AnyLeg copy = new AnyLeg(this);

        copy._origin = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithOrigin(Func<AnyUnLocode, AnyUnLocode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._originRecipe = configure(_originRecipe);
        copy._origin       = copy._originRecipe;

        return copy;
    }

    public AnyLeg WithDestination(UnLocode value) {
        AnyLeg copy = new AnyLeg(this);

        copy._destination = Any.Fixed(value);

        return copy;
    }

    public AnyLeg WithDestination(Func<AnyUnLocode, AnyUnLocode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._destinationRecipe = configure(_destinationRecipe);
        copy._destination       = copy._destinationRecipe;

        return copy;
    }

    /// <summary>Pins the departure; the arrival follows it, unless it is pinned too.</summary>
    public AnyLeg WithDeparture(DateTimeOffset value) {
        AnyLeg copy = new AnyLeg(this);

        copy._departurePin = value;

        return copy;
    }

    public AnyLeg WithDeparture(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._departureRecipe = configure(_departureRecipe);
        copy._departurePin    = null;

        return copy;
    }

    /// <summary>Pins the arrival; the departure precedes it by a drawn duration, unless it is pinned too.</summary>
    public AnyLeg WithArrival(DateTimeOffset value) {
        AnyLeg copy = new AnyLeg(this);

        copy._arrivalPin = value;

        return copy;
    }

    /// <summary>Refines the transit duration, from one minute to <see cref="Leg.MaxTransit" />.</summary>
    public AnyLeg WithDuration(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLeg copy = new AnyLeg(this);

        copy._durationRecipe = configure(_durationRecipe);

        return copy;
    }

    public AnyLeg WithDuration(TimeSpan value) {
        AnyLeg copy = new AnyLeg(this);

        copy._durationRecipe = Any.TimeSpan().Between(value, value);

        return copy;
    }

    public Leg Generate() {
        UnLocode origin      = _origin.Generate();
        UnLocode destination = _destination.DifferentFrom(origin).Generate();

        DateTimeOffset departure;
        DateTimeOffset arrival;
        if (_departurePin is { } pinnedDeparture && _arrivalPin is { } pinnedArrival) {
            departure = pinnedDeparture;
            arrival   = pinnedArrival;
        } else if (_arrivalPin is { } onlyArrival) {
            arrival   = onlyArrival;
            departure = arrival - _durationRecipe.Generate();
        } else {
            departure = _departurePin ?? _departureRecipe.Generate();
            arrival   = (departure + _durationRecipe.Generate()).ToOffset(_arrivalOffsetRecipe.Generate());
        }

        return new Leg(origin, destination, departure, arrival);
    }

}
