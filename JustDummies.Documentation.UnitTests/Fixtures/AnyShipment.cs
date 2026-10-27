// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Shipment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace JustDummies.Documentation.UnitTests.Fixtures;

/// <summary>
///     A generator of arbitrary <see cref="Shipment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShipment : IAny<Shipment> {

    private readonly IAny<Route>                 _route;
    private readonly IAny<int>                   _parcels;
    private readonly IAny<IReadOnlyList<string>> _labels;
    private readonly IAny<decimal?>              _declaredValue;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShipment()
        : this(route:         AnyValidRoute(),
               parcels:       AnyValidParcels(),
               labels:        AnyValidLabels(),
               declaredValue: AnyValidDeclaredValue().AsNullable()) { }

    private static AnyRoute AnyValidRoute() {
        // A shipment only accepts a domestic route; a route on its own may cross a border.
        return new AnyRoute().WithDomestic(true);
    }

    private static AnyInt32 AnyValidParcels() {
        return Any.Int32().Positive();
    }

    private static AnyList<string> AnyValidLabels() {
        return Any.ListOf(Any.String().NonEmpty());
    }

    private static AnyDecimal AnyValidDeclaredValue() {
        return Any.Decimal();
    }

    private AnyShipment(IAny<Route>                 route,
                        IAny<int>                   parcels,
                        IAny<IReadOnlyList<string>> labels,
                        IAny<decimal?>              declaredValue) {
        _route         = route;
        _parcels       = parcels;
        _labels        = labels;
        _declaredValue = declaredValue;
    }

    /// <summary>Pins <c>route</c> to a fixed value.</summary>
    public AnyShipment WithRoute(Route value) {
        return new AnyShipment(new FixedValue<Route>(value), _parcels, _labels, _declaredValue);
    }

    /// <summary>Draws <c>route</c> from <c>AnyValidRoute()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithRoute(Func<AnyRoute, AnyRoute> configure) {
        return new AnyShipment(configure(AnyValidRoute()), _parcels, _labels, _declaredValue);
    }

    /// <summary>Pins <c>parcels</c> to a fixed value.</summary>
    public AnyShipment WithParcels(int value) {
        return new AnyShipment(_route, new FixedValue<int>(value), _labels, _declaredValue);
    }

    /// <summary>Draws <c>parcels</c> from <c>AnyValidParcels()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithParcels(Func<AnyInt32, AnyInt32> configure) {
        return new AnyShipment(_route, configure(AnyValidParcels()), _labels, _declaredValue);
    }

    /// <summary>Pins <c>labels</c> to a fixed value.</summary>
    public AnyShipment WithLabels(IReadOnlyList<string> value) {
        return new AnyShipment(_route, _parcels, new FixedValue<IReadOnlyList<string>>(value), _declaredValue);
    }

    /// <summary>Draws <c>labels</c> from <c>AnyValidLabels()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithLabels(Func<AnyList<string>, AnyList<string>> configure) {
        return new AnyShipment(_route, _parcels, configure(AnyValidLabels()), _declaredValue);
    }

    /// <summary>Pins <c>declaredValue</c> to a fixed value.</summary>
    public AnyShipment WithDeclaredValue(decimal? value) {
        return new AnyShipment(_route, _parcels, _labels, new FixedValue<decimal?>(value));
    }

    /// <summary>Draws <c>declaredValue</c> from <c>AnyValidDeclaredValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithDeclaredValue(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyShipment(_route, _parcels, _labels, configure(AnyValidDeclaredValue()).AsNullable());
    }

    /// <summary>Produces one arbitrary <see cref="Shipment" />.</summary>
    public Shipment Generate() {
        return new Shipment(_route.Generate(),
                            _parcels.Generate(),
                            _labels.Generate(),
                            _declaredValue.Generate());
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
