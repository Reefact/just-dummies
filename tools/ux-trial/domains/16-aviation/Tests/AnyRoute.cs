// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Route --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Route" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoute : IAny<Route> {

    private readonly IAny<IataAirportCode> _origin;
    private readonly IAny<IataAirportCode> _destination;
    private readonly IAny<int>             _distanceKm;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoute()
        : this(origin:      AnyValidOrigin(),
               destination: AnyValidDestination(),
               distanceKm:  AnyValidDistanceKm()) { }

    private static AnyIataAirportCode AnyValidOrigin() {
        return new AnyIataAirportCode();
    }

    private static AnyIataAirportCode AnyValidDestination() {
        // Must differ from the origin: enforced in Generate(), the only place both are known.
        return new AnyIataAirportCode();
    }

    private static AnyInt32 AnyValidDistanceKm() {
        return Any.Int32().Between(50, 20000);
    }

    private AnyRoute(IAny<IataAirportCode> origin,
                     IAny<IataAirportCode> destination,
                     IAny<int>             distanceKm) {
        _origin      = origin;
        _destination = destination;
        _distanceKm  = distanceKm;
    }

    /// <summary>Pins <c>origin</c> to a fixed value.</summary>
    public AnyRoute WithOrigin(IataAirportCode value) {
        return new AnyRoute(new FixedValue<IataAirportCode>(value), _destination, _distanceKm);
    }

    /// <summary>Draws <c>origin</c> from <c>AnyValidOrigin()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithOrigin(Func<AnyIataAirportCode, AnyIataAirportCode> configure) {
        return new AnyRoute(configure(AnyValidOrigin()), _destination, _distanceKm);
    }

    /// <summary>Pins <c>destination</c> to a fixed value.</summary>
    public AnyRoute WithDestination(IataAirportCode value) {
        return new AnyRoute(_origin, new FixedValue<IataAirportCode>(value), _distanceKm);
    }

    /// <summary>Draws <c>destination</c> from <c>AnyValidDestination()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithDestination(Func<AnyIataAirportCode, AnyIataAirportCode> configure) {
        return new AnyRoute(_origin, configure(AnyValidDestination()), _distanceKm);
    }

    /// <summary>Pins <c>distanceKm</c> to a fixed value.</summary>
    public AnyRoute WithDistanceKm(int value) {
        return new AnyRoute(_origin, _destination, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>distanceKm</c> from <c>AnyValidDistanceKm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithDistanceKm(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRoute(_origin, _destination, configure(AnyValidDistanceKm()));
    }

    /// <summary>Produces one arbitrary <see cref="Route" />.</summary>
    public Route Generate() {
        // Cross-field rule the per-parameter bases cannot hold: origin and destination differ.
        // A bounded redraw of the destination, as the library itself would do.
        IataAirportCode origin = _origin.Generate();
        for (int attempt = 0; attempt < 100; attempt++) {
            IataAirportCode destination = _destination.Generate();
            if (!destination.Equals(origin)) {
                return new Route(origin, destination, _distanceKm.Generate());
            }
        }

        throw new InvalidOperationException($"No destination different from {origin} could be drawn.");
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
