using Aviation.Domain;

using JustDummies;

using Xunit;

namespace Aviation.Tests.Discovery;

public sealed class RouteTests {

    [Fact]
    public void A_route_keeps_its_airports_and_distance() {
        // Arrange
        IataAirportCode paris   = new IataAirportCode("CDG");
        IataAirportCode newYork = new IataAirportCode("JFK");

        // Act
        Route route = new Route(paris, newYork, 5837);

        // Assert
        Assert.Same(paris, route.Origin);
        Assert.Same(newYork, route.Destination);
        Assert.Equal(5837, route.DistanceKm);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(51)]
    [InlineData(19_999)]
    [InlineData(20_000)]
    public void A_distance_within_50_and_20000_km_is_accepted(int distanceKm) {
        // Arrange
        Route anyRoute = Any.Route().Generate();

        // Act
        Route route = new Route(anyRoute.Origin, anyRoute.Destination, distanceKm);

        // Assert
        Assert.Equal(distanceKm, route.DistanceKm);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(49)]
    [InlineData(20_001)]
    [InlineData(int.MaxValue)]
    public void A_distance_outside_50_to_20000_km_is_refused(int distanceKm) {
        // Arrange
        Route anyRoute = Any.Route().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Route(anyRoute.Origin, anyRoute.Destination, distanceKm));
    }

    [Fact]
    public void A_null_origin_is_refused() {
        // Arrange
        Route anyRoute = Any.Route().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Route(null!, anyRoute.Destination, anyRoute.DistanceKm));
    }

    [Fact]
    public void A_null_destination_is_refused() {
        // Arrange
        Route anyRoute = Any.Route().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Route(anyRoute.Origin, null!, anyRoute.DistanceKm));
    }

    [Fact]
    public void A_route_between_two_equal_codes_is_refused_even_through_distinct_instances() {
        // Arrange
        IataAirportCode anyOrigin      = Any.IataAirportCode().Generate();
        IataAirportCode sameAsOrigin   = new IataAirportCode(anyOrigin.Value);
        int             anyDistanceKm  = Any.Int32().Between(50, 20_000).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Route(anyOrigin, sameAsOrigin, anyDistanceKm));
    }

    [Fact]
    public void Reversing_a_route_swaps_its_airports_and_keeps_its_distance() {
        // Arrange
        IataAirportCode lyon  = new IataAirportCode("LYS");
        IataAirportCode lille = new IataAirportCode("LIL");
        Route           route = new Route(lyon, lille, 560);

        // Act
        Route reversed = route.Reversed();

        // Assert
        Assert.Equal(lille, reversed.Origin);
        Assert.Equal(lyon, reversed.Destination);
        Assert.Equal(560, reversed.DistanceKm);
        Assert.Equal(lyon, route.Origin);
    }

    [Fact]
    public void Reversing_a_route_twice_gives_back_the_same_airports() {
        // Arrange
        Route route = new Route(new IataAirportCode("LYS"), new IataAirportCode("LIL"), 560);

        // Act
        Route twice = route.Reversed().Reversed();

        // Assert
        Assert.Equal(route.Origin, twice.Origin);
        Assert.Equal(route.Destination, twice.Destination);
    }

}
