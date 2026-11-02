using JustDummies;

using Xunit;

namespace Aviation.Domain.Tests.Regular;

public sealed class RouteTests {

    [Fact]
    public void A_route_keeps_its_airports_and_its_distance() {
        // Arrange
        IataAirportCode anyOrigin      = Arbitrary.IataAirportCode().Generate();
        IataAirportCode anyDestination = Arbitrary.IataAirportCode().WithValue(v => v.DifferentFrom(anyOrigin.Value)).Generate();

        // Act
        Route route = new Route(anyOrigin, anyDestination, 1200);

        // Assert
        Assert.Same(anyOrigin, route.Origin);
        Assert.Same(anyDestination, route.Destination);
        Assert.Equal(1200, route.DistanceKm);
    }

    [Fact]
    public void A_null_origin_is_refused() {
        // Arrange
        IataAirportCode anyDestination = Arbitrary.IataAirportCode().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Route(null!, anyDestination, 1200));
    }

    [Fact]
    public void A_null_destination_is_refused() {
        // Arrange
        IataAirportCode anyOrigin = Arbitrary.IataAirportCode().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Route(anyOrigin, null!, 1200));
    }

    [Fact]
    public void A_route_between_the_same_airport_is_refused() {
        // Arrange
        IataAirportCode anyAirport = Arbitrary.IataAirportCode().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Route(anyAirport, anyAirport, 1200));
    }

    [Fact]
    public void Two_distinct_instances_of_the_same_airport_are_still_the_same_airport() {
        // Arrange
        IataAirportCode anyOrigin      = Arbitrary.IataAirportCode().Generate();
        IataAirportCode sameAsOrigin   = new IataAirportCode(anyOrigin.Value);

        // Assert
        Assert.Throws<ArgumentException>(() => new Route(anyOrigin, sameAsOrigin, 1200));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(51)]
    [InlineData(20_000)]
    [InlineData(19_999)]
    public void A_distance_within_50_to_20000_km_is_accepted(int distanceKm) {
        // Arrange
        IataAirportCode anyOrigin      = Arbitrary.IataAirportCode().Generate();
        IataAirportCode anyDestination = Arbitrary.IataAirportCode().WithValue(v => v.DifferentFrom(anyOrigin.Value)).Generate();

        // Act
        Route route = new Route(anyOrigin, anyDestination, distanceKm);

        // Assert
        Assert.Equal(distanceKm, route.DistanceKm);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(20_001)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_distance_outside_50_to_20000_km_is_refused(int distanceKm) {
        // Arrange
        IataAirportCode anyOrigin      = Arbitrary.IataAirportCode().Generate();
        IataAirportCode anyDestination = Arbitrary.IataAirportCode().WithValue(v => v.DifferentFrom(anyOrigin.Value)).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Route(anyOrigin, anyDestination, distanceKm));
    }

    [Fact]
    public void Reversing_a_route_swaps_the_airports_and_keeps_the_distance() {
        // Arrange
        Route anyRoute = Arbitrary.Route().WithDistanceKm(430).Generate();

        // Act
        Route reversed = anyRoute.Reversed();

        // Assert
        Assert.Equal(anyRoute.Destination, reversed.Origin);
        Assert.Equal(anyRoute.Origin, reversed.Destination);
        Assert.Equal(430, reversed.DistanceKm);
    }

    [Fact]
    public void Reversing_a_route_leaves_the_original_untouched() {
        // Arrange
        Route anyRoute = Arbitrary.Route().Generate();
        IataAirportCode origin      = anyRoute.Origin;
        IataAirportCode destination = anyRoute.Destination;

        // Act
        Route reversed = anyRoute.Reversed();

        // Assert
        Assert.NotSame(anyRoute, reversed);
        Assert.Same(origin, anyRoute.Origin);
        Assert.Same(destination, anyRoute.Destination);
    }

    [Fact]
    public void Reversing_a_route_twice_gives_back_the_same_airports() {
        // Arrange
        Route anyRoute = Arbitrary.Route().Generate();

        // Act
        Route reversedTwice = anyRoute.Reversed().Reversed();

        // Assert
        Assert.Equal(anyRoute.Origin, reversedTwice.Origin);
        Assert.Equal(anyRoute.Destination, reversedTwice.Destination);
        Assert.Equal(anyRoute.DistanceKm, reversedTwice.DistanceKm);
    }

}
