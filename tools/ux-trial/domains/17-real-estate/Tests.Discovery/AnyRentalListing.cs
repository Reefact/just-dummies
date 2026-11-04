// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RentalListing --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RentalListing" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRentalListing : IAny<RentalListing> {

    private IAny<Property>           _property;
    private IAny<decimal>            _monthlyRent;

    private AnyProperty _propertyRecipe;
    private AnyDecimal  _monthlyRentRecipe;
    private AnyDateOnly _availableFromRecipe;
    private AnyDateOnly _availableUntilRecipe;

    private DateOnly? _availableFromPin;
    private DateOnly? _availableUntilPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRentalListing()
        : this(property:       AnyValidProperty(),
               monthlyRent:    AnyValidMonthlyRent(),
               availableFrom:  AnyValidAvailableFrom(),
               availableUntil: AnyValidAvailableUntil()) { }

    private static AnyProperty AnyValidProperty() {
        // Rated A to D, so it is never banned from letting; the window is then drawn inside the diagnosis's validity (see Generate).
        return new AnyProperty().WithDiagnosis(diagnosis => diagnosis.Lettable());
    }

    private static AnyDecimal AnyValidMonthlyRent() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyDateOnly AnyValidAvailableFrom() {
        return Any.DateOnly();
    }

    private static AnyDateOnly AnyValidAvailableUntil() {
        return Any.DateOnly();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRentalListing(AnyProperty property,
                             AnyDecimal  monthlyRent,
                             AnyDateOnly availableFrom,
                             AnyDateOnly availableUntil) {
        _property                       = property;
        _propertyRecipe                 = property;
        _monthlyRent                    = monthlyRent;
        _monthlyRentRecipe              = monthlyRent;
        _availableFromRecipe            = availableFrom;
        _availableUntilRecipe           = availableUntil;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRentalListing(AnyRentalListing from) {
        _property                       = from._property;
        _propertyRecipe                 = from._propertyRecipe;
        _monthlyRent                    = from._monthlyRent;
        _monthlyRentRecipe              = from._monthlyRentRecipe;
        _availableFromRecipe            = from._availableFromRecipe;
        _availableFromPin               = from._availableFromPin;
        _availableUntilRecipe           = from._availableUntilRecipe;
        _availableUntilPin              = from._availableUntilPin;
    }

    /// <summary>Draws <c>availableFrom</c> and <c>availableUntil</c> together, <c>availableFrom</c> always before <c>availableUntil</c> as the constructor requires, each from its current recipe and pin.</summary>
    /// <summary>Pins <c>property</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRentalListing WithProperty(Property value) {
        AnyRentalListing copy = new AnyRentalListing(this);

        copy._property = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>property</c> is drawn from — <c>AnyValidProperty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyProperty, AnyProperty&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRentalListing WithProperty(Func<AnyProperty, AnyProperty> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRentalListing copy = new AnyRentalListing(this);

        copy._propertyRecipe = configure(_propertyRecipe);
        copy._property       = copy._propertyRecipe;

        return copy;
    }

    /// <summary>Pins <c>monthlyRent</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRentalListing WithMonthlyRent(decimal value) {
        AnyRentalListing copy = new AnyRentalListing(this);

        copy._monthlyRent = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>monthlyRent</c> is drawn from — <c>AnyValidMonthlyRent()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRentalListing WithMonthlyRent(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRentalListing copy = new AnyRentalListing(this);

        copy._monthlyRentRecipe = configure(_monthlyRentRecipe);
        copy._monthlyRent       = copy._monthlyRentRecipe;

        return copy;
    }

    /// <summary>Pins <c>availableFrom</c> to a fixed value, which bounds <c>availableUntil</c>; its recipe is kept for a later refinement.</summary>
    public AnyRentalListing WithAvailableFrom(DateOnly value) {
        AnyRentalListing copy = new AnyRentalListing(this);

        copy._availableFromPin               = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>availableFrom</c> is drawn from — <c>AnyValidAvailableFrom()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>availableUntil</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRentalListing WithAvailableFrom(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRentalListing copy = new AnyRentalListing(this);

        copy._availableFromRecipe            = configure(_availableFromRecipe);
        copy._availableFromPin               = null;

        return copy;
    }

    /// <summary>Pins <c>availableUntil</c> to a fixed value, which bounds <c>availableFrom</c>; its recipe is kept for a later refinement.</summary>
    public AnyRentalListing WithAvailableUntil(DateOnly value) {
        AnyRentalListing copy = new AnyRentalListing(this);

        copy._availableUntilPin              = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>availableUntil</c> is drawn from — <c>AnyValidAvailableUntil()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>availableFrom</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRentalListing WithAvailableUntil(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRentalListing copy = new AnyRentalListing(this);

        copy._availableUntilRecipe           = configure(_availableUntilRecipe);
        copy._availableUntilPin              = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RentalListing" />.</summary>
    public RentalListing Generate() {
        Property property = _property.Generate();

        // CanBeLetOn(availableFrom) requires the diagnosis to be valid that day: the start is drawn inside its validity.
        AnyDateOnly from = _availableFromPin is null ? _availableFromRecipe.Between(property.Diagnosis.IssuedOn, property.Diagnosis.ValidUntil.AddDays(-1)) : _availableFromRecipe;

        (DateOnly, DateOnly) window = _availableFromPin is { } availableFrom ? (availableFrom, _availableUntilPin ?? _availableUntilRecipe.After(availableFrom).Generate())
                                      : _availableUntilPin is { } availableUntil ? (from.Before(availableUntil).Generate(), availableUntil)
                                      : Any.Pair(from, _availableUntilRecipe).Having.Item1BeforeItem2().Generate();

        (DateOnly start, DateOnly end) = window;

        return new RentalListing(property,
                                 _monthlyRent.Generate(),
                                 start,
                                 end);
    }

}
