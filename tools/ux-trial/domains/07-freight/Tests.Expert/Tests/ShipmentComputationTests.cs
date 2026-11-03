using System.Globalization;

using Freight.Domain;
using Freight.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class ShipmentComputationTests {

    private static readonly DateTimeOffset FirstDeparture = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private static Shipment ShipmentLoadedWith(double kilograms, double cubicMetres) {
        return Any.Shipment().WithGrossWeight(new Weight(kilograms)).WithVolume(new Volume(cubicMetres)).Generate();
    }

    private static Shipment ShipmentArrivingAt(DateTimeOffset arrival) {
        Leg leg = Any.Leg().WithDeparture(arrival.AddDays(-10)).WithArrival(arrival).Generate();

        return Any.Shipment().WithLegs([leg]).Generate();
    }

    [Fact]
    public void The_estimated_arrival_is_the_arrival_of_the_last_leg() {
        Leg first = Any.Leg().WithDeparture(FirstDeparture).WithArrival(FirstDeparture.AddDays(10)).Generate();
        Leg last  = Any.Leg().WithOrigin(first.Destination).WithDeparture(FirstDeparture.AddDays(12)).WithArrival(FirstDeparture.AddDays(20).AddHours(6)).Generate();

        Shipment shipment = Any.Shipment().WithLegs([first, last]).Generate();

        Assert.Equal(new DateTimeOffset(2026, 3, 21, 14, 0, 0, TimeSpan.Zero), shipment.EstimatedArrival);
    }

    [Fact]
    public void The_planned_transit_time_runs_from_the_first_departure_to_the_last_arrival_layovers_included() {
        Leg first = Any.Leg().WithDeparture(FirstDeparture).WithArrival(FirstDeparture.AddDays(10)).Generate();
        Leg last  = Any.Leg().WithOrigin(first.Destination).WithDeparture(FirstDeparture.AddDays(12)).WithArrival(FirstDeparture.AddDays(20).AddHours(6)).Generate();

        Shipment shipment = Any.Shipment().WithLegs([first, last]).Generate();

        Assert.Equal(TimeSpan.FromDays(20) + TimeSpan.FromHours(6), shipment.PlannedTransitTime);
    }

    [Fact]
    public void The_planned_transit_time_is_that_of_the_single_leg_when_there_is_only_one() {
        Leg only = Any.Leg().WithDeparture(FirstDeparture).WithArrival(FirstDeparture.AddDays(33)).Generate();

        Shipment shipment = Any.Shipment().WithLegs([only]).Generate();

        Assert.Equal(TimeSpan.FromDays(33), shipment.PlannedTransitTime);
    }

    [Fact]
    public void The_planned_transit_time_is_measured_in_instants_across_time_zones() {
        DateTimeOffset departureFromShanghai = new DateTimeOffset(2026, 3, 1, 20, 0, 0, TimeSpan.FromHours(8));    // 12:00 UTC
        DateTimeOffset arrivalAtLosAngeles   = new DateTimeOffset(2026, 3, 15, 5, 0, 0, TimeSpan.FromHours(-8));   // 13:00 UTC
        Leg            crossing              = Any.Leg().WithDeparture(departureFromShanghai).WithArrival(arrivalAtLosAngeles).Generate();

        Shipment shipment = Any.Shipment().WithLegs([crossing]).Generate();

        Assert.Equal(TimeSpan.FromDays(14) + TimeSpan.FromHours(1), shipment.PlannedTransitTime);
    }

    [Fact]
    public void The_estimated_arrival_can_be_read_in_another_offset_without_moving_the_instant() {
        DateTimeOffset arrival  = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
        Shipment       shipment = ShipmentArrivingAt(arrival);

        DateTimeOffset inKolkata = shipment.EstimatedArrivalAt(new TimeSpan(5, 30, 0));

        Assert.Equal(new TimeSpan(5, 30, 0), inKolkata.Offset);
        Assert.Equal(new DateTime(2026, 3, 10, 17, 30, 0), inKolkata.DateTime);
        Assert.Equal(arrival, inKolkata);
    }

    [Theory]
    [InlineData(20_000, 10, 20)]       // weight governs: 20 t against 10 m3
    [InlineData(5_000, 30, 30)]        // volume governs: 5 t against 30 m3
    [InlineData(12_500, 12.5, 12.5)]   // a tie
    [InlineData(26_500, 76.4, 76.4)]   // both at the highest a container may carry
    public void Revenue_tons_are_the_greater_of_metric_tons_and_cubic_metres(double kilograms, double cubicMetres, double expectedRevenueTons) {
        Shipment shipment = ShipmentLoadedWith(kilograms, cubicMetres);

        Assert.Equal(expectedRevenueTons, shipment.RevenueTons, precision: 9);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 16.6, 0.5)]
    [InlineData(ContainerType.Dry40, 67.7, 1)]
    [InlineData(ContainerType.HighCube40, 19.1, 0.25)]
    [InlineData(ContainerType.Reefer40, 29.65, 0.5)]
    public void Volume_utilisation_is_the_volume_over_the_capacity_of_the_container_type(ContainerType type, double cubicMetres, double expectedUtilisation) {
        Shipment shipment = Any.Shipment().WithContainerType(type).WithVolume(new Volume(cubicMetres)).Generate();

        Assert.Equal(expectedUtilisation, shipment.VolumeUtilisation, precision: 9);
    }

    [Theory]
    [InlineData("100", 20_000, 10, "2000.00")]     // 20 revenue tons
    [InlineData("100", 5_000, 30, "3000.00")]      // 30 revenue tons
    [InlineData("12.34", 20_000, 10, "246.80")]
    [InlineData("0.01", 1_000, 12.5, "0.13")]      // 0.125 is a midpoint: rounded away from zero, not to even
    [InlineData("0.05", 1_000, 12.5, "0.63")]      // 0.625 is a midpoint: away from zero gives 0.63, to even would give 0.62
    [InlineData("10.05", 1_000, 12.345, "124.07")] // 124.06725
    [InlineData("9999.99", 26_500, 76.4, "763999.24")]
    public void A_quote_is_the_rate_per_revenue_ton_times_the_revenue_tons_rounded_to_cents(string rate, double kilograms, double cubicMetres, string expectedQuote) {
        FreightRate freightRate = Any.FreightRate().WithPerRevenueTon(decimal.Parse(rate, CultureInfo.InvariantCulture)).Generate();
        Shipment    shipment    = ShipmentLoadedWith(kilograms, cubicMetres);

        decimal quote = shipment.QuoteWith(freightRate);

        Assert.Equal(decimal.Parse(expectedQuote, CultureInfo.InvariantCulture), quote);
    }

    [Fact]
    public void The_currency_of_the_rate_plays_no_part_in_the_quote() {
        Shipment    shipment = ShipmentLoadedWith(20_000, 10);
        FreightRate inEuros  = new FreightRate(100m, "EUR");
        FreightRate inYen    = new FreightRate(100m, "JPY");

        Assert.Equal(shipment.QuoteWith(inEuros), shipment.QuoteWith(inYen));
    }

    [Fact]
    public void A_quote_without_a_rate_is_refused_as_missing() {
        Shipment anyShipment = Any.Shipment().Generate();

        Assert.Throws<ArgumentNullException>(() => anyShipment.QuoteWith(null!));
    }

}
