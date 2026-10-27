// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Route --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace JustDummies.Documentation.UnitTests.Fixtures;

/// <summary>
///     A generator of arbitrary <see cref="Route" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoute : IAny<Route> {

    private readonly IAny<string> _origin;
    private readonly IAny<string> _destination;
    private readonly IAny<int>    _distanceKm;
    private readonly IAny<bool>   _domestic;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoute()
        : this(origin:      AnyValidOrigin(),
               destination: AnyValidDestination(),
               distanceKm:  AnyValidDistanceKm(),
               domestic:    AnyValidDomestic()) { }

    private static AnyString AnyValidOrigin() {
        return Any.String().NonEmpty();
    }

    private static AnyString AnyValidDestination() {
        return Any.String().NonEmpty();
    }

    private static AnyInt32 AnyValidDistanceKm() {
        return Any.Int32().Positive();
    }

    private static AnyBoolean AnyValidDomestic() {
        return Any.Boolean();
    }

    private AnyRoute(IAny<string> origin,
                     IAny<string> destination,
                     IAny<int>    distanceKm,
                     IAny<bool>   domestic) {
        _origin      = origin;
        _destination = destination;
        _distanceKm  = distanceKm;
        _domestic    = domestic;
    }

    /// <summary>Pins <c>origin</c> to a fixed value.</summary>
    public AnyRoute WithOrigin(string value) {
        return new AnyRoute(new FixedValue<string>(value), _destination, _distanceKm, _domestic);
    }

    /// <summary>Draws <c>origin</c> from <c>AnyValidOrigin()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithOrigin(Func<AnyString, AnyString> configure) {
        return new AnyRoute(configure(AnyValidOrigin()), _destination, _distanceKm, _domestic);
    }

    /// <summary>Pins <c>destination</c> to a fixed value.</summary>
    public AnyRoute WithDestination(string value) {
        return new AnyRoute(_origin, new FixedValue<string>(value), _distanceKm, _domestic);
    }

    /// <summary>Draws <c>destination</c> from <c>AnyValidDestination()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithDestination(Func<AnyString, AnyString> configure) {
        return new AnyRoute(_origin, configure(AnyValidDestination()), _distanceKm, _domestic);
    }

    /// <summary>Pins <c>distanceKm</c> to a fixed value.</summary>
    public AnyRoute WithDistanceKm(int value) {
        return new AnyRoute(_origin, _destination, new FixedValue<int>(value), _domestic);
    }

    /// <summary>Draws <c>distanceKm</c> from <c>AnyValidDistanceKm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithDistanceKm(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRoute(_origin, _destination, configure(AnyValidDistanceKm()), _domestic);
    }

    /// <summary>Pins <c>domestic</c> to a fixed value.</summary>
    public AnyRoute WithDomestic(bool value) {
        return new AnyRoute(_origin, _destination, _distanceKm, new FixedValue<bool>(value));
    }

    /// <summary>Draws <c>domestic</c> from <c>AnyValidDomestic()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithDomestic(Func<AnyBoolean, AnyBoolean> configure) {
        return new AnyRoute(_origin, _destination, _distanceKm, configure(AnyValidDomestic()));
    }

    /// <summary>Produces one arbitrary <see cref="Route" />.</summary>
    public Route Generate() {
        return new Route(_origin.Generate(),
                         _destination.Generate(),
                         _distanceKm.Generate(),
                         _domestic.Generate());
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
