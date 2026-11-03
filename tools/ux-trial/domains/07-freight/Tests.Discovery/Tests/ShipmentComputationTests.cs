using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class ShipmentComputationTests {

    /// <summary>A shipment of the given cargo; the container type is arbitrary, and every cargo used here fits all of them.</summary>
    private static Shipment CarryingKilosAndCubicMetres(double kilograms, double cubicMetres) {
        return Any.Shipment().WithGrossWeight(new Weight(kilograms)).WithVolume(new Volume(cubicMetres)).Generate();
    }

    private static Shipment WithLegs(params Leg[] legs) {
        return Any.Shipment().WithLegs(legs).Generate();
    }

    [Fact]
    public void The_estimated_arrival_is_the_arrival_of_the_last_leg() {
        // Arrange
        Shipment shipment = WithLegs(Calendar.Leg("FRLEH", "GBSOU", Calendar.At(1), Calendar.At(3)),
                                     Calendar.Leg("GBSOU", "CNSHA", Calendar.At(5), Calendar.At(20)));

        // Assert
        Assert.Equal(Calendar.At(20), shipment.EstimatedArrival);
    }

    [Fact]
    public void The_planned_transit_time_runs_from_the_first_departure_to_the_last_arrival_waits_included() {
        // Arrange
        Shipment shipment = WithLegs(Calendar.Leg("FRLEH", "GBSOU", Calendar.At(1), Calendar.At(3)),
                                     Calendar.Leg("GBSOU", "CNSHA", Calendar.At(5), Calendar.At(20)));

        // Assert
        Assert.Equal(TimeSpan.FromDays(19), shipment.PlannedTransitTime);
    }

    [Fact]
    public void The_estimated_arrival_can_be_expressed_with_the_offset_of_the_destination() {
        // Arrange
        Shipment shipment = WithLegs(Calendar.LeHavreToShanghai());

        // Act
        DateTimeOffset local = shipment.EstimatedArrivalAt(TimeSpan.FromHours(8));

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), local.Offset);
        Assert.Equal(shipment.EstimatedArrival, local);
        Assert.Equal(8, local.Hour);
    }

    [Fact]
    public void Revenue_tons_follow_the_weight_when_the_cargo_is_dense() {
        Assert.Equal(10.0, CarryingKilosAndCubicMetres(10_000, 5).RevenueTons);
    }

    [Fact]
    public void Revenue_tons_follow_the_volume_when_the_cargo_is_light() {
        Assert.Equal(30.0, CarryingKilosAndCubicMetres(2_000, 30).RevenueTons);
    }

    [Fact]
    public void Revenue_tons_are_that_value_when_weight_and_volume_balance() {
        Assert.Equal(5.0, CarryingKilosAndCubicMetres(5_000, 5).RevenueTons);
    }

    [Theory]
    [InlineData(ContainerType.Dry20, 16.6, 0.5)]
    [InlineData(ContainerType.Dry40, 33.85, 0.5)]
    [InlineData(ContainerType.HighCube40, 76.4, 1.0)]
    [InlineData(ContainerType.Reefer40, 14.825, 0.25)]
    public void Volume_utilisation_is_the_volume_over_the_capacity_of_the_type(ContainerType type, double cubicMetres, double expected) {
        // Arrange
        Shipment shipment = Any.Shipment().WithContainerType(type).WithVolume(new Volume(cubicMetres)).Generate();

        // Assert
        Assert.Equal(expected, shipment.VolumeUtilisation, 10);
    }

    [Fact]
    public void A_quote_is_the_rate_times_the_revenue_tons_when_weight_decides() {
        // Arrange
        Shipment    shipment = CarryingKilosAndCubicMetres(10_000, 5);
        FreightRate rate     = Any.FreightRate().WithPerRevenueTon(100m).Generate();

        // Assert
        Assert.Equal(1_000.00m, shipment.QuoteWith(rate));
    }

    [Fact]
    public void A_quote_is_the_rate_times_the_revenue_tons_when_volume_decides() {
        // Arrange
        Shipment    shipment = CarryingKilosAndCubicMetres(2_000, 30);
        FreightRate rate     = Any.FreightRate().WithPerRevenueTon(3.33m).Generate();

        // Assert: 3.33 x 30 = 99.90
        Assert.Equal(99.90m, shipment.QuoteWith(rate));
    }

    [Theory]
    [InlineData("0.01", 0.03)]   // 0.01 x 2.5 = 0.025 -> 0.03
    [InlineData("0.03", 0.08)]   // 0.03 x 2.5 = 0.075 -> 0.08
    [InlineData("0.02", 0.05)]   // 0.02 x 2.5 = 0.050 -> 0.05
    public void A_quote_is_rounded_to_cents_with_midpoints_going_away_from_zero(string rateText, double expected) {
        // Arrange
        decimal     perRevenueTon = decimal.Parse(rateText, System.Globalization.CultureInfo.InvariantCulture);
        Shipment    shipment      = CarryingKilosAndCubicMetres(2_500, 1);   // exactly 2.5 revenue tons
        FreightRate rate          = Any.FreightRate().WithPerRevenueTon(perRevenueTon).Generate();

        // Assert
        Assert.Equal((decimal)expected, shipment.QuoteWith(rate));
    }

    [Fact]
    public void The_currency_of_the_rate_plays_no_part_in_the_quote() {
        // Arrange
        Shipment shipment = CarryingKilosAndCubicMetres(10_000, 5);

        // Act
        decimal inEuros = shipment.QuoteWith(new FreightRate(42.5m, "EUR"));
        decimal inYen   = shipment.QuoteWith(new FreightRate(42.5m, "JPY"));

        // Assert
        Assert.Equal(inEuros, inYen);
    }

    [Fact]
    public void Quoting_with_a_missing_rate_is_refused_with_an_argument_null_exception() {
        // Arrange
        Shipment anyShipment = Any.Shipment().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyShipment.QuoteWith(null!));
    }

}
