using JustDummies;

using RealEstate.Domain;

using Xunit;

namespace RealEstate.Tests.Discovery;

public sealed class RentalListingTests {

    // A home rated C, diagnosed on 2023-01-01: valid and not banned throughout 2024.
    private static AnyProperty LettableHome() {
        return Any.Property()
                  .WithKind(kind => kind.Except(PropertyKind.Commercial))
                  .WithDiagnosis(diagnosis => diagnosis.OfRating(EnergyRating.C).WithIssuedOn(new DateOnly(2023, 1, 1)));
    }

    [Fact]
    public void A_listing_keeps_what_it_is_given() {
        // Arrange
        Property property = LettableHome().Generate();

        // Act
        RentalListing listing = new RentalListing(property, 850.50m, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        // Assert
        Assert.Same(property, listing.Property);
        Assert.Equal(850.50m, listing.MonthlyRent);
        Assert.Equal(new DateOnly(2024, 1, 1), listing.AvailableFrom);
        Assert.Equal(new DateOnly(2024, 12, 31), listing.AvailableUntil);
    }

    [Fact]
    public void A_listing_needs_a_property() {
        Assert.Throws<ArgumentNullException>(() => new RentalListing(null!, 850m, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-850")]
    public void A_rent_that_is_not_positive_is_refused(string rent) {
        // Arrange
        Property property = LettableHome().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new RentalListing(property, decimal.Parse(rent, System.Globalization.CultureInfo.InvariantCulture), new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)));
    }

    [Theory]
    [InlineData("850.001")]
    [InlineData("0.005")]
    public void A_rent_finer_than_a_cent_is_refused(string rent) {
        // Arrange
        Property property = LettableHome().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new RentalListing(property, decimal.Parse(rent, System.Globalization.CultureInfo.InvariantCulture), new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)));
    }

    [Fact]
    public void A_rent_of_one_cent_is_accepted() {
        // Arrange
        Property property = LettableHome().Generate();

        // Assert
        Assert.Equal(0.01m, new RentalListing(property, 0.01m, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)).MonthlyRent);
    }

    [Theory]
    [InlineData("2024-06-01", "2024-06-01")]   // empty window
    [InlineData("2024-06-02", "2024-06-01")]   // reversed
    public void A_window_that_does_not_end_after_it_starts_is_refused(string from, string until) {
        // Arrange
        Property property = LettableHome().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RentalListing(property, 850m, DateOnly.Parse(from), DateOnly.Parse(until)));

        // Assert
        Assert.Equal("availableUntil", exception.ParamName);
    }

    [Fact]
    public void A_window_of_a_single_day_is_accepted() {
        // Arrange
        Property property = LettableHome().Generate();

        // Assert
        Assert.Equal(new DateOnly(2024, 6, 2), new RentalListing(property, 850m, new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 2)).AvailableUntil);
    }

    [Fact]
    public void A_property_banned_from_letting_on_the_start_date_is_refused() {
        // Arrange
        Property sieve = Any.Property()
                            .WithKind(kind => kind.Except(PropertyKind.Commercial))
                            .WithDiagnosis(diagnosis => diagnosis.OfRating(EnergyRating.G).WithIssuedOn(new DateOnly(2022, 1, 1)))
                            .Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => new RentalListing(sieve, 850m, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void Only_the_start_date_is_tested_against_the_ban() {
        // Arrange: a G home may still be listed from 2024-12-31, whatever the window then spans.
        Property sieve = Any.Property()
                            .WithKind(kind => kind.Except(PropertyKind.Commercial))
                            .WithDiagnosis(diagnosis => diagnosis.OfRating(EnergyRating.G).WithIssuedOn(new DateOnly(2022, 1, 1)))
                            .Generate();

        // Act
        RentalListing listing = new RentalListing(sieve, 850m, new DateOnly(2024, 12, 31), new DateOnly(2027, 1, 1));

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 1), listing.AvailableUntil);
    }

    [Theory]
    [InlineData("2022-12-31")]   // before the diagnosis
    [InlineData("2033-01-01")]   // the day it expires
    public void A_property_without_a_valid_diagnosis_on_the_start_date_is_refused(string from) {
        // Arrange
        Property property = LettableHome().Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => new RentalListing(property, 850m, DateOnly.Parse(from), DateOnly.Parse(from).AddDays(30)));
    }

    [Fact]
    public void The_window_is_checked_before_the_property() {
        // Arrange
        Property sieve = Any.Property()
                            .WithKind(kind => kind.Except(PropertyKind.Commercial))
                            .WithDiagnosis(diagnosis => diagnosis.OfRating(EnergyRating.G).WithIssuedOn(new DateOnly(2022, 1, 1)))
                            .Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new RentalListing(sieve, 850m, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1)));
    }

    [Theory]
    [InlineData(45d, "900", "20.00")]
    [InlineData(80d, "1000", "12.50")]
    [InlineData(30d, "1000", "33.33")]
    [InlineData(30d, "2000", "66.67")]
    [InlineData(3d, "10", "3.33")]
    public void The_rent_per_square_meter_is_the_rent_over_the_area_rounded_to_the_cent(double area, string rent, string expected) {
        // Arrange
        Property      property = LettableHome().WithLivingArea(new Surface(area)).Generate();
        RentalListing listing  = Any.RentalListing().WithProperty(property).WithMonthlyRent(decimal.Parse(rent, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), listing.RentPerSquareMeter);
    }

    [Theory]
    [InlineData(PropertyKind.House, "1200")]
    [InlineData(PropertyKind.Apartment, "1200")]
    [InlineData(PropertyKind.Commercial, "3600")]
    public void The_deposit_is_capped_at_one_month_of_rent_and_three_for_commercial_premises(PropertyKind kind, string expected) {
        // Arrange
        Property      property = Any.Property().WithKind(kind).WithDiagnosis(diagnosis => diagnosis.Lettable()).Generate();
        RentalListing listing  = Any.RentalListing().WithProperty(property).WithMonthlyRent(1200m).Generate();

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), listing.MaximumDeposit);
    }

}
