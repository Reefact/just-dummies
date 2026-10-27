namespace RealEstate.Domain;

/// <summary>A property offered for rent over a period.</summary>
public sealed class RentalListing {

    public RentalListing(Property property, decimal monthlyRent, DateOnly availableFrom, DateOnly availableUntil) {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(monthlyRent);
        if (decimal.Round(monthlyRent, 2) != monthlyRent) {
            throw new ArgumentException("A rent is in cents.", nameof(monthlyRent));
        }
        if (availableFrom >= availableUntil) {
            throw new ArgumentException("The availability window must end after it starts.", nameof(availableUntil));
        }
        if (!property.CanBeLetOn(availableFrom)) {
            throw new InvalidOperationException("That property may not be let on that date.");
        }
        Property = property;
        MonthlyRent = monthlyRent;
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
    }

    public Property Property { get; }

    public decimal MonthlyRent { get; }

    public DateOnly AvailableFrom { get; }

    public DateOnly AvailableUntil { get; }

    public decimal RentPerSquareMeter => Math.Round(MonthlyRent / (decimal)Property.LivingArea.SquareMeters, 2);

    /// <summary>The deposit is capped at one month of rent for unfurnished housing.</summary>
    public decimal MaximumDeposit => Property.Kind == PropertyKind.Commercial ? MonthlyRent * 3 : MonthlyRent;
}
