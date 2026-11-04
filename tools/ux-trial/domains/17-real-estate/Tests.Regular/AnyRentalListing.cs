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
        // A property that may be let on any date of the availability window: never banned (A to D), valid for years after it opens.
        return new AnyProperty().Lettable();
    }

    private static AnyDecimal AnyValidMonthlyRent() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyDateOnly AnyValidAvailableFrom() {
        return Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2030, 12, 31));
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

    /// <summary>Draws <c>availableFrom</c> and <c>availableUntil</c>, <c>availableFrom</c> always before <c>availableUntil</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (DateOnly AvailableFrom, DateOnly AvailableUntil) AvailableFromAndAvailableUntil() {
        if (_availableFromPin is { } availableFrom) { return (availableFrom, _availableUntilPin ?? _availableUntilRecipe.After(availableFrom).Generate()); }
        if (_availableUntilPin is { } availableUntil) { return (_availableFromRecipe.Before(availableUntil).Generate(), availableUntil); }

        return Any.Pair(_availableFromRecipe, _availableUntilRecipe).Having.Item1BeforeItem2().Generate();
    }

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
        (DateOnly availableFrom, DateOnly availableUntil) = AvailableFromAndAvailableUntil();

        return new RentalListing(_property.Generate(),
                                 _monthlyRent.Generate(),
                                 availableFrom,
                                 availableUntil);
    }

}
