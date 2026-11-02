using JustDummies;

using Xunit;

namespace RealEstate.Domain.Tests;

public sealed class RentalListingTests {

    private static readonly DateOnly DiagnosisIssuedOn = new DateOnly(2024, 6, 1);

    // A property that can be let on any day its diagnosis is valid: rated A to D, so never banned.
    private static Property AnyPropertyLetThroughout() {
        return Any.Property()
                  .WithDiagnosis(diagnosis => diagnosis.WithIssuedOn(DiagnosisIssuedOn).WithRating(rating => rating.OneOf(EnergyRating.A, EnergyRating.B, EnergyRating.C, EnergyRating.D)))
                  .Generate();
    }

    [Fact]
    public void Keeps_the_property_the_rent_and_the_window_it_was_built_from() {
        // Arrange
        Property property = AnyPropertyLetThroughout();

        // Act
        RentalListing listing = new RentalListing(property, 850.50m, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        // Assert
        Assert.Same(property, listing.Property);
        Assert.Equal(850.50m, listing.MonthlyRent);
        Assert.Equal(new DateOnly(2025, 1, 1), listing.AvailableFrom);
        Assert.Equal(new DateOnly(2025, 12, 31), listing.AvailableUntil);
    }

    [Fact]
    public void Refuses_a_missing_property() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new RentalListing(null!, 850.50m, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)));

        // Assert
        Assert.Equal("property", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-850.50)]
    [InlineData(-0.005)] // not in cents either, but not above zero is reported first
    public void Refuses_a_rent_that_is_not_above_zero(decimal rent) {
        // Arrange
        Property property = AnyPropertyLetThroughout();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RentalListing(property, rent, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)));

        // Assert
        Assert.Equal("monthlyRent", exception.ParamName);
    }

    [Theory]
    [InlineData(850.005)]
    [InlineData(0.001)]
    [InlineData(850.501)]
    public void Refuses_a_rent_that_is_not_in_cents(decimal rent) {
        // Arrange
        Property property = AnyPropertyLetThroughout();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RentalListing(property, rent, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)));

        // Assert
        Assert.Equal("monthlyRent", exception.ParamName);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(850.50)]
    [InlineData(99_999.99)]
    public void Accepts_a_rent_from_one_cent_up(decimal rent) {
        // Arrange
        Property property = AnyPropertyLetThroughout();

        // Act
        RentalListing listing = new RentalListing(property, rent, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        // Assert
        Assert.Equal(rent, listing.MonthlyRent);
    }

    [Theory]
    [InlineData("2025-06-01", "2025-06-01")] // a window of no length
    [InlineData("2025-06-02", "2025-06-01")] // ending the day before it starts
    [InlineData("2026-01-01", "2025-01-01")] // ending a year before
    public void Refuses_a_window_that_does_not_end_after_it_starts(string from, string until) {
        // Arrange
        Property property = AnyPropertyLetThroughout();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RentalListing(property, 850.50m, DateOnly.Parse(from), DateOnly.Parse(until)));

        // Assert
        Assert.Equal("availableUntil", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_window_of_a_single_day() {
        // Arrange
        Property property = AnyPropertyLetThroughout();

        // Act
        RentalListing listing = new RentalListing(property, 850.50m, new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 2));

        // Assert
        Assert.Equal(new DateOnly(2025, 6, 2), listing.AvailableUntil);
    }

    [Fact]
    public void Refuses_a_property_that_the_letting_calendar_bans_on_the_first_day_of_the_window() {
        // Arrange
        Property bannedGHousing = Any.Property()
                                     .WithKind(PropertyKind.House)
                                     .WithDiagnosis(diagnosis => diagnosis.WithRating(EnergyRating.G).WithIssuedOn(new DateOnly(2022, 1, 1)))
                                     .Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new RentalListing(bannedGHousing, 850.50m, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void Refuses_a_property_whose_diagnosis_has_expired_on_the_first_day_of_the_window() {
        // Arrange
        Property property = Any.Property()
                               .WithDiagnosis(diagnosis => diagnosis.WithRating(rating => rating.OneOf(EnergyRating.A, EnergyRating.B)).WithIssuedOn(new DateOnly(2022, 1, 1)))
                               .Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new RentalListing(property, 850.50m, new DateOnly(2032, 1, 1), new DateOnly(2033, 1, 1)));
    }

    [Fact]
    public void Only_checks_the_first_day_of_the_window_against_the_calendar() {
        // Arrange: a G is let until 2024-12-31 and banned from the next day
        Property gHousing = Any.Property()
                               .WithKind(PropertyKind.Apartment)
                               .WithDiagnosis(diagnosis => diagnosis.WithRating(EnergyRating.G).WithIssuedOn(new DateOnly(2022, 1, 1)))
                               .Generate();

        // Act
        RentalListing listing = new RentalListing(gHousing, 850.50m, new DateOnly(2024, 12, 31), new DateOnly(2026, 12, 31));

        // Assert
        Assert.Equal(new DateOnly(2026, 12, 31), listing.AvailableUntil);
    }

    [Theory]
    [InlineData(650.00, 30.00, 21.67)]   // 21.6666...
    [InlineData(900.00, 45.00, 20.00)]
    [InlineData(1234.56, 12.34, 100.05)] // 100.0453...
    [InlineData(100.04, 8.00, 12.50)]    // 12.505: the even cent is 12.50
    [InlineData(100.20, 8.00, 12.52)]    // 12.525: the even cent is 12.52
    public void Divides_the_rent_by_the_living_area_to_the_cent(decimal rent, double livingArea, decimal rentPerSquareMeter) {
        // Arrange
        RentalListing listing = Any.RentalListing().WithMonthlyRent(rent).WithProperty(property => property.WithLivingArea(new Surface(livingArea))).Generate();

        // Act
        decimal actual = listing.RentPerSquareMeter;

        // Assert
        Assert.Equal(rentPerSquareMeter, actual);
    }

    [Theory]
    [InlineData(PropertyKind.House)]
    [InlineData(PropertyKind.Apartment)]
    public void Caps_the_deposit_at_one_month_of_rent_for_housing(PropertyKind kind) {
        // Arrange
        RentalListing listing = Any.RentalListing().WithMonthlyRent(1234.56m).WithProperty(property => property.WithKind(kind)).Generate();

        // Act
        decimal deposit = listing.MaximumDeposit;

        // Assert
        Assert.Equal(1234.56m, deposit);
    }

    [Fact]
    public void Caps_the_deposit_at_three_months_of_rent_for_commercial_premises() {
        // Arrange
        RentalListing listing = Any.RentalListing().WithMonthlyRent(1234.56m).WithProperty(property => property.WithKind(PropertyKind.Commercial)).Generate();

        // Act
        decimal deposit = listing.MaximumDeposit;

        // Assert
        Assert.Equal(3703.68m, deposit);
    }

}
