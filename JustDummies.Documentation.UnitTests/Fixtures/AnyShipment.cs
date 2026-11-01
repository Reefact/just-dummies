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

    private IAny<Route>                 _route;
    private IAny<int>                   _parcels;
    private IAny<IReadOnlyList<string>> _labels;
    private IAny<decimal?>              _declaredValue;

    private AnyRoute        _routeRecipe;
    private AnyInt32        _parcelsRecipe;
    private AnyList<string> _labelsRecipe;
    private AnyDecimal      _declaredValueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShipment()
        : this(route:         AnyValidRoute(),
               parcels:       AnyValidParcels(),
               labels:        AnyValidLabels(),
               declaredValue: AnyValidDeclaredValue()) { }

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

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyShipment(AnyRoute        route,
                        AnyInt32        parcels,
                        AnyList<string> labels,
                        AnyDecimal      declaredValue) {
        _route               = route;
        _routeRecipe         = route;
        _parcels             = parcels;
        _parcelsRecipe       = parcels;
        _labels              = labels;
        _labelsRecipe        = labels;
        _declaredValue       = declaredValue.AsNullable();
        _declaredValueRecipe = declaredValue;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyShipment(AnyShipment from) {
        _route               = from._route;
        _routeRecipe         = from._routeRecipe;
        _parcels             = from._parcels;
        _parcelsRecipe       = from._parcelsRecipe;
        _labels              = from._labels;
        _labelsRecipe        = from._labelsRecipe;
        _declaredValue       = from._declaredValue;
        _declaredValueRecipe = from._declaredValueRecipe;
    }

    /// <summary>Pins <c>route</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithRoute(Route value) {
        AnyShipment copy = new AnyShipment(this);

        copy._route = new FixedValue<Route>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>route</c> is drawn from — <c>AnyValidRoute()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyShipment WithRoute(Func<AnyRoute, AnyRoute> configure) {
        AnyShipment copy = new AnyShipment(this);

        copy._routeRecipe = configure(_routeRecipe);
        copy._route       = copy._routeRecipe;

        return copy;
    }

    /// <summary>Pins <c>parcels</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithParcels(int value) {
        AnyShipment copy = new AnyShipment(this);

        copy._parcels = new FixedValue<int>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>parcels</c> is drawn from — <c>AnyValidParcels()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyShipment WithParcels(Func<AnyInt32, AnyInt32> configure) {
        AnyShipment copy = new AnyShipment(this);

        copy._parcelsRecipe = configure(_parcelsRecipe);
        copy._parcels       = copy._parcelsRecipe;

        return copy;
    }

    /// <summary>Pins <c>labels</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithLabels(IReadOnlyList<string> value) {
        AnyShipment copy = new AnyShipment(this);

        copy._labels = new FixedValue<IReadOnlyList<string>>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>labels</c> is drawn from — <c>AnyValidLabels()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyShipment WithLabels(Func<AnyList<string>, AnyList<string>> configure) {
        AnyShipment copy = new AnyShipment(this);

        copy._labelsRecipe = configure(_labelsRecipe);
        copy._labels       = copy._labelsRecipe;

        return copy;
    }

    /// <summary>Pins <c>declaredValue</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithDeclaredValue(decimal? value) {
        AnyShipment copy = new AnyShipment(this);

        copy._declaredValue = new FixedValue<decimal?>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>declaredValue</c> is drawn from — <c>AnyValidDeclaredValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyShipment WithDeclaredValue(Func<AnyDecimal, AnyDecimal> configure) {
        AnyShipment copy = new AnyShipment(this);

        copy._declaredValueRecipe = configure(_declaredValueRecipe);
        copy._declaredValue       = copy._declaredValueRecipe.AsNullable();

        return copy;
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
