namespace Freight.Domain.Tests;

public sealed class ShipmentComputationTests {

    private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_estimated_arrival_is_the_arrival_of_the_last_leg() {
        // Arrange
        IReadOnlyList<Leg> anyLegs = new AnyItinerary().WithLegCount(3).Generate();
        Shipment           shipment = Any.Shipment().WithLegs(anyLegs).Generate();

        // Act
        DateTimeOffset estimatedArrival = shipment.EstimatedArrival;

        // Assert
        Assert.Equal(anyLegs[2].Arrival, estimatedArrival);
        Assert.Equal(anyLegs[2].Arrival.Offset, estimatedArrival.Offset);
    }

    [Fact]
    public void The_planned_transit_time_covers_the_layovers() {
        // Arrange: 5 days at sea, 2 days in port, 10 days at sea.
        Leg first  = Any.Leg().WithDeparture(Start).WithArrival(Start.AddDays(5)).Generate();
        Leg second = Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival.AddDays(2)).WithArrival(first.Arrival.AddDays(12)).Generate();
        Shipment shipment = Any.Shipment().WithLegs(new[] { first, second }).Generate();

        // Act
        TimeSpan transit = shipment.PlannedTransitTime;

        // Assert
        Assert.Equal(TimeSpan.FromDays(17), transit);
    }

    [Fact]
    public void The_planned_transit_time_of_a_single_leg_is_its_duration() {
        // Arrange
        Leg      leg      = Any.Leg().WithDeparture(Start).WithArrival(Start.AddHours(30)).Generate();
        Shipment shipment = Any.Shipment().WithLegs(new[] { leg }).Generate();

        // Act & Assert
        Assert.Equal(leg.Duration, shipment.PlannedTransitTime);
        Assert.Equal(TimeSpan.FromHours(30), shipment.PlannedTransitTime);
    }

    [Theory]
    [InlineData(10_000, 5.0, 10.0)]
    [InlineData(2_000, 20.0, 20.0)]
    [InlineData(12_500, 12.5, 12.5)]
    [InlineData(500, 0.25, 0.5)]
    public void The_revenue_tons_are_the_greater_of_the_metric_tons_and_the_cubic_metres(double kilograms, double cubicMetres, double expected) {
        // Arrange
        Shipment shipment = Any.Shipment().WithContainerType(ContainerType.Dry20)
                               .WithGrossWeight(new Weight(kilograms))
                               .WithVolume(new Volume(cubicMetres))
                               .Generate();

        // Act
        double revenueTons = shipment.RevenueTons;

        // Assert
        Assert.Equal(expected, revenueTons, 10);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 16.6, 0.5)]
    [InlineData(ContainerType.Dry40, 67.7, 1.0)]
    [InlineData(ContainerType.HighCube40, 19.1, 0.25)]
    [InlineData(ContainerType.Reefer40, 29.65, 0.5)]
    public void The_volume_utilisation_is_the_volume_over_the_capacity_of_the_type(ContainerType type, double cubicMetres, double expected) {
        // Arrange
        Shipment shipment = Any.Shipment().WithContainerType(type).WithVolume(new Volume(cubicMetres)).Generate();

        // Act
        double utilisation = shipment.VolumeUtilisation;

        // Assert
        Assert.Equal(expected, utilisation, 10);
    }

    [Fact]
    public void The_estimated_arrival_can_be_read_in_another_offset_without_moving_in_time() {
        // Arrange
        Leg      leg      = Any.Leg().WithDeparture(Start).WithArrival(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)).Generate();
        Shipment shipment = Any.Shipment().WithLegs(new[] { leg }).Generate();

        // Act
        DateTimeOffset local = shipment.EstimatedArrivalAt(TimeSpan.FromMinutes(330));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(330), local.Offset);
        Assert.Equal(new DateTime(2026, 3, 10, 17, 30, 0), local.DateTime);
        Assert.Equal(shipment.EstimatedArrival, local);
    }

    [Fact]
    public void An_offset_beyond_fourteen_hours_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyShipment.EstimatedArrivalAt(TimeSpan.FromHours(15)));
    }

    [Theory]
    [InlineData("10000", 5.0, "25.50", "255.00")]
    [InlineData("2000", 20.0, "10.00", "200.00")]
    [InlineData("1005", 0.5, "1.00", "1.01")]
    [InlineData("2500", 0.5, "0.01", "0.03")]
    [InlineData("1234", 0.5, "9999.99", "12339.99")]
    public void The_quote_is_the_rate_times_the_revenue_tons_rounded_to_cents_away_from_zero(string kilograms, double cubicMetres, string rate, string expected) {
        // Arrange
        decimal  perRevenueTon = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture);
        Shipment shipment      = Any.Shipment().WithContainerType(ContainerType.Dry20)
                                    .WithGrossWeight(new Weight(double.Parse(kilograms, System.Globalization.CultureInfo.InvariantCulture)))
                                    .WithVolume(new Volume(cubicMetres))
                                    .Generate();
        FreightRate anyRate = Any.FreightRate().WithPerRevenueTon(perRevenueTon).Generate();

        // Act
        decimal quote = shipment.QuoteWith(anyRate);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), quote);
    }

    [Fact]
    public void The_currency_of_the_rate_plays_no_part_in_the_quote() {
        // Arrange
        Shipment shipment = Any.Shipment().Generate();
        decimal  anyAmount = Any.Decimal().Between(0.01m, 10_000m).WithScale(2).Generate();
        string   anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();
        string   anyOtherCurrency = Any.StringMatching("[A-Z]{3}").DifferentFrom(anyCurrency).Generate();

        // Act
        decimal inFirst  = shipment.QuoteWith(new FreightRate(anyAmount, anyCurrency));
        decimal inSecond = shipment.QuoteWith(new FreightRate(anyAmount, anyOtherCurrency));

        // Assert
        Assert.Equal(inFirst, inSecond);
    }

    [Fact]
    public void A_quote_has_at_most_two_decimals() {
        // Arrange
        Shipment    anyShipment = Any.Shipment().Generate();
        FreightRate anyRate     = Any.FreightRate().Generate();

        // Act
        decimal quote = anyShipment.QuoteWith(anyRate);

        // Assert
        Assert.Equal(decimal.Round(quote, 2), quote);
    }

    [Fact]
    public void A_missing_rate_is_refused() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyShipment.QuoteWith(null!));

        // Assert
        Assert.Equal("rate", refusal.ParamName);
    }

}
