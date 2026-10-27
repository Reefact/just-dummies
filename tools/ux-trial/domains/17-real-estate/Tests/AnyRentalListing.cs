// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RentalListing --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RentalListing" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRentalListing : IAny<RentalListing> {

    private readonly IAny<Property> _property;
    private readonly IAny<decimal>  _monthlyRent;
    private readonly IAny<DateOnly> _availableFrom;
    private readonly IAny<DateOnly> _availableUntil;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRentalListing()
        : this(property:       AnyValidProperty(),
               monthlyRent:    AnyValidMonthlyRent(),
               availableFrom:  AnyValidAvailableFrom(),
               availableUntil: AnyValidAvailableUntil()) { }

    private static AnyProperty AnyValidProperty() {
        // A listing needs a property that may be let over the whole window drawn below
        // (2024-2031): a diagnosis issued 2022-2023 is still valid, and F/G homes are banned by then.
        return new AnyProperty().WithDiagnosis(d => d.WithIssuedOn(i => i.Between(new DateOnly(2022, 1, 1), new DateOnly(2023, 12, 31)))
                                                     .WithDeclaredRating(r => r.Except(EnergyRating.F, EnergyRating.G)));
    }

    private static AnyDecimal AnyValidMonthlyRent() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyDateOnly AnyValidAvailableFrom() {
        return Any.DateOnly().Between(new DateOnly(2024, 1, 1), new DateOnly(2031, 12, 31));
    }

    private static AnyDateOnly AnyValidAvailableUntil() {
        // Always after any availableFrom the base can draw: the window ends after it starts.
        return Any.DateOnly().After(new DateOnly(2031, 12, 31));
    }

    private AnyRentalListing(IAny<Property> property,
                             IAny<decimal>  monthlyRent,
                             IAny<DateOnly> availableFrom,
                             IAny<DateOnly> availableUntil) {
        _property       = property;
        _monthlyRent    = monthlyRent;
        _availableFrom  = availableFrom;
        _availableUntil = availableUntil;
    }

    /// <summary>Pins <c>property</c> to a fixed value.</summary>
    public AnyRentalListing WithProperty(Property value) {
        return new AnyRentalListing(new FixedValue<Property>(value), _monthlyRent, _availableFrom, _availableUntil);
    }

    /// <summary>Draws <c>property</c> from <c>AnyValidProperty()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRentalListing WithProperty(Func<AnyProperty, AnyProperty> configure) {
        return new AnyRentalListing(configure(AnyValidProperty()), _monthlyRent, _availableFrom, _availableUntil);
    }

    /// <summary>Pins <c>monthlyRent</c> to a fixed value.</summary>
    public AnyRentalListing WithMonthlyRent(decimal value) {
        return new AnyRentalListing(_property, new FixedValue<decimal>(value), _availableFrom, _availableUntil);
    }

    /// <summary>Draws <c>monthlyRent</c> from <c>AnyValidMonthlyRent()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRentalListing WithMonthlyRent(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyRentalListing(_property, configure(AnyValidMonthlyRent()), _availableFrom, _availableUntil);
    }

    /// <summary>Pins <c>availableFrom</c> to a fixed value.</summary>
    public AnyRentalListing WithAvailableFrom(DateOnly value) {
        return new AnyRentalListing(_property, _monthlyRent, new FixedValue<DateOnly>(value), _availableUntil);
    }

    /// <summary>Draws <c>availableFrom</c> from <c>AnyValidAvailableFrom()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRentalListing WithAvailableFrom(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyRentalListing(_property, _monthlyRent, configure(AnyValidAvailableFrom()), _availableUntil);
    }

    /// <summary>Pins <c>availableUntil</c> to a fixed value.</summary>
    public AnyRentalListing WithAvailableUntil(DateOnly value) {
        return new AnyRentalListing(_property, _monthlyRent, _availableFrom, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>availableUntil</c> from <c>AnyValidAvailableUntil()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRentalListing WithAvailableUntil(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyRentalListing(_property, _monthlyRent, _availableFrom, configure(AnyValidAvailableUntil()));
    }

    /// <summary>Produces one arbitrary <see cref="RentalListing" />.</summary>
    public RentalListing Generate() {
        return new RentalListing(_property.Generate(),
                                 _monthlyRent.Generate(),
                                 _availableFrom.Generate(),
                                 _availableUntil.Generate());
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
