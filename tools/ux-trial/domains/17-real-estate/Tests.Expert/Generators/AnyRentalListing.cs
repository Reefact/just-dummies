// Scaffolded by `any` (JustDummies), then rewritten by hand: `property.CanBeLetOn(availableFrom)` ties the dates
// to the property's diagnosis window, which no per-parameter recipe can honour. The property is drawn first,
// with a rating that is never banned (A to D), and the availability window is derived from its diagnosis.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>A generator of arbitrary <see cref="RentalListing" /> values: a property that can be let, on a window it is valid for.</summary>
public sealed partial class AnyRentalListing : IAny<RentalListing> {

    private IAny<Property> _property;
    private IAny<decimal>  _monthlyRent;

    private AnyProperty _propertyRecipe;
    private AnyDecimal  _monthlyRentRecipe;

    public AnyRentalListing()
        : this(property:    AnyValidProperty(),
               monthlyRent: AnyValidMonthlyRent()) { }

    private static AnyProperty AnyValidProperty() {
        // Ratings A to D are never banned from letting, so every diagnosis window of such a property can host a listing.
        return new AnyProperty().WithDiagnosis(diagnosis => diagnosis.WithRating(rating => rating.OneOf(EnergyRating.A, EnergyRating.B, EnergyRating.C, EnergyRating.D)));
    }

    private static AnyDecimal AnyValidMonthlyRent() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private AnyRentalListing(AnyProperty property, AnyDecimal monthlyRent) {
        _property          = property;
        _propertyRecipe    = property;
        _monthlyRent       = monthlyRent;
        _monthlyRentRecipe = monthlyRent;
    }

    private AnyRentalListing(AnyRentalListing from) {
        _property          = from._property;
        _propertyRecipe    = from._propertyRecipe;
        _monthlyRent       = from._monthlyRent;
        _monthlyRentRecipe = from._monthlyRentRecipe;
    }

    public AnyRentalListing WithProperty(Property value) {
        AnyRentalListing copy = new AnyRentalListing(this);

        copy._property = Any.Fixed(value);

        return copy;
    }

    public AnyRentalListing WithProperty(Func<AnyProperty, AnyProperty> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRentalListing copy = new AnyRentalListing(this);

        copy._propertyRecipe = configure(_propertyRecipe);
        copy._property       = copy._propertyRecipe;

        return copy;
    }

    public AnyRentalListing WithMonthlyRent(decimal value) {
        AnyRentalListing copy = new AnyRentalListing(this);

        copy._monthlyRent = Any.Fixed(value);

        return copy;
    }

    public AnyRentalListing WithMonthlyRent(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRentalListing copy = new AnyRentalListing(this);

        copy._monthlyRentRecipe = configure(_monthlyRentRecipe);
        copy._monthlyRent       = copy._monthlyRentRecipe;

        return copy;
    }

    public RentalListing Generate() {
        Property property = _property.Generate();

        // The window starts while the diagnosis is valid and ends after it starts.
        DateOnly availableFrom  = Any.DateOnly().Between(property.Diagnosis.IssuedOn, property.Diagnosis.ValidUntil.AddDays(-2)).Generate();
        DateOnly availableUntil = Any.DateOnly().Between(availableFrom.AddDays(1), DateOnly.MaxValue).Generate();

        return new RentalListing(property, _monthlyRent.Generate(), availableFrom, availableUntil);
    }

}
