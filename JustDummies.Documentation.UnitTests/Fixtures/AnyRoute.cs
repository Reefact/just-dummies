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

    private IAny<string> _origin;
    private IAny<string> _destination;
    private IAny<int>    _distanceKm;
    private IAny<bool>   _domestic;

    private AnyString  _originRecipe;
    private AnyString  _destinationRecipe;
    private AnyInt32   _distanceKmRecipe;
    private AnyBoolean _domesticRecipe;

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

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRoute(AnyString  origin,
                     AnyString  destination,
                     AnyInt32   distanceKm,
                     AnyBoolean domestic) {
        _origin            = origin;
        _originRecipe      = origin;
        _destination       = destination;
        _destinationRecipe = destination;
        _distanceKm        = distanceKm;
        _distanceKmRecipe  = distanceKm;
        _domestic          = domestic;
        _domesticRecipe    = domestic;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRoute(AnyRoute from) {
        _origin            = from._origin;
        _originRecipe      = from._originRecipe;
        _destination       = from._destination;
        _destinationRecipe = from._destinationRecipe;
        _distanceKm        = from._distanceKm;
        _distanceKmRecipe  = from._distanceKmRecipe;
        _domestic          = from._domestic;
        _domesticRecipe    = from._domesticRecipe;
    }

    /// <summary>Pins <c>origin</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithOrigin(string value) {
        AnyRoute copy = new AnyRoute(this);

        copy._origin = new FixedValue<string>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>origin</c> is drawn from — <c>AnyValidOrigin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyRoute WithOrigin(Func<AnyString, AnyString> configure) {
        AnyRoute copy = new AnyRoute(this);

        copy._originRecipe = configure(_originRecipe);
        copy._origin       = copy._originRecipe;

        return copy;
    }

    /// <summary>Pins <c>destination</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithDestination(string value) {
        AnyRoute copy = new AnyRoute(this);

        copy._destination = new FixedValue<string>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>destination</c> is drawn from — <c>AnyValidDestination()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyRoute WithDestination(Func<AnyString, AnyString> configure) {
        AnyRoute copy = new AnyRoute(this);

        copy._destinationRecipe = configure(_destinationRecipe);
        copy._destination       = copy._destinationRecipe;

        return copy;
    }

    /// <summary>Pins <c>distanceKm</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithDistanceKm(int value) {
        AnyRoute copy = new AnyRoute(this);

        copy._distanceKm = new FixedValue<int>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>distanceKm</c> is drawn from — <c>AnyValidDistanceKm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyRoute WithDistanceKm(Func<AnyInt32, AnyInt32> configure) {
        AnyRoute copy = new AnyRoute(this);

        copy._distanceKmRecipe = configure(_distanceKmRecipe);
        copy._distanceKm       = copy._distanceKmRecipe;

        return copy;
    }

    /// <summary>Pins <c>domestic</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyRoute WithDomestic(bool value) {
        AnyRoute copy = new AnyRoute(this);

        copy._domestic = new FixedValue<bool>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>domestic</c> is drawn from — <c>AnyValidDomestic()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyRoute WithDomestic(Func<AnyBoolean, AnyBoolean> configure) {
        AnyRoute copy = new AnyRoute(this);

        copy._domesticRecipe = configure(_domesticRecipe);
        copy._domestic       = copy._domesticRecipe;

        return copy;
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
