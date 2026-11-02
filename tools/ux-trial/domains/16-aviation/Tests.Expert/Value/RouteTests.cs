using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Value;

public sealed class RouteTests {

    [Fact]
    public void A_route_keeps_its_airports_and_its_distance() {
        // Arrange
        IataAirportCode paris    = new IataAirportCode("CDG");
        IataAirportCode newYork  = new IataAirportCode("JFK");

        // Act
        Route route = new Route(paris, newYork, 5_837);

        // Assert
        Assert.Same(paris, route.Origin);
        Assert.Same(newYork, route.Destination);
        Assert.Equal(5_837, route.DistanceKm);
    }

    [Fact]
    public void Reversing_a_route_swaps_its_airports_and_keeps_its_distance() {
        // Arrange
        Route parisNewYork = new Route(new IataAirportCode("CDG"), new IataAirportCode("JFK"), 5_837);

        // Act
        Route newYorkParis = parisNewYork.Reversed();

        // Assert
        Assert.Equal(new IataAirportCode("JFK"), newYorkParis.Origin);
        Assert.Equal(new IataAirportCode("CDG"), newYorkParis.Destination);
        Assert.Equal(5_837, newYorkParis.DistanceKm);
    }

    [Fact]
    public void Reversing_a_route_leaves_the_original_untouched() {
        // Arrange
        Route parisNewYork = new Route(new IataAirportCode("CDG"), new IataAirportCode("JFK"), 5_837);

        // Act
        parisNewYork.Reversed();

        // Assert
        Assert.Equal(new IataAirportCode("CDG"), parisNewYork.Origin);
        Assert.Equal(new IataAirportCode("JFK"), parisNewYork.Destination);
    }

    [Fact]
    public void A_route_between_two_airports_with_the_same_code_is_refused_even_as_distinct_instances() {
        // Arrange
        IataAirportCode anyOrigin   = Any.IataAirportCode().Generate();
        IataAirportCode sameAirport = new IataAirportCode(anyOrigin.Value);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Route().WithOrigin(anyOrigin).WithDestination(sameAirport).Generate());

        // Assert
        Assert.Equal("destination", refusal.ParamName);
    }

    [Fact]
    public void A_route_without_an_origin_is_refused() {
        // Arrange
        Route anyRoute = Any.Route().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Route(null!, anyRoute.Destination, anyRoute.DistanceKm));
    }

    [Fact]
    public void A_route_without_a_destination_is_refused() {
        // Arrange
        Route anyRoute = Any.Route().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Route(anyRoute.Origin, null!, anyRoute.DistanceKm));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(51)]
    [InlineData(19_999)]
    [InlineData(20_000)]
    public void A_distance_from_50_to_20000_km_is_accepted(int distanceKm) {
        // Act
        Route route = Any.Route().WithDistanceKm(distanceKm).Generate();

        // Assert
        Assert.Equal(distanceKm, route.DistanceKm);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(0)]
    [InlineData(-5_000)]
    [InlineData(20_001)]
    [InlineData(int.MaxValue)]
    public void A_distance_outside_50_to_20000_km_is_refused(int distanceKm) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Route().WithDistanceKm(distanceKm).Generate());

        Assert.Equal("distanceKm", refusal.ParamName);
    }

}
