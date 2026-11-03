using JustDummies;

using Xunit;

namespace Freight.Domain.Tests.Discovery;

public sealed class ShipmentLifecycleTests {

    private static readonly DateTimeOffset Departed  = Calendar.At(2);
    private static readonly DateTimeOffset Delivered = Calendar.At(35);

    /// <summary>A shipment due on day 31, brought into the requested status along the only paths the model allows.</summary>
    private static Shipment ShipmentIn(ShipmentStatus status) {
        Shipment shipment = Any.Shipment().WithLegs(new[] { Calendar.LeHavreToShanghai() }).Generate();

        switch (status) {
            case ShipmentStatus.Booked:
                break;
            case ShipmentStatus.InTransit:
                shipment.Depart(Departed);
                break;
            case ShipmentStatus.Delivered:
                shipment.Depart(Departed);
                shipment.Deliver(Delivered);
                break;
            case ShipmentStatus.Cancelled:
                shipment.Cancel();
                break;
        }

        return shipment;
    }

    [Fact]
    public void Departing_a_booked_shipment_puts_it_in_transit() {
        // Arrange
        Shipment shipment = ShipmentIn(ShipmentStatus.Booked);

        // Act
        shipment.Depart(Departed);

        // Assert
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal(Departed, shipment.DepartedAt);
        Assert.Null(shipment.DeliveredAt);
    }

    [Fact]
    public void The_actual_departure_is_not_compared_with_the_planned_one() {
        // Arrange: the first leg is planned for day 1; the shipment actually leaves almost a year earlier
        Shipment       shipment = ShipmentIn(ShipmentStatus.Booked);
        DateTimeOffset early    = Calendar.At(1).AddDays(-300);

        // Act
        shipment.Depart(early);

        // Assert
        Assert.Equal(early, shipment.DepartedAt);
    }

    [Theory]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void Only_a_booked_shipment_can_depart(ShipmentStatus status) {
        // Arrange
        Shipment                shipment    = ShipmentIn(status);
        DateTimeOffset?         departedAt  = shipment.DepartedAt;

        // Assert
        Assert.Throws<InvalidOperationException>(() => shipment.Depart(Calendar.At(3)));
        Assert.Equal(status, shipment.Status);
        Assert.Equal(departedAt, shipment.DepartedAt);
    }

    [Fact]
    public void Delivering_a_shipment_in_transit_delivers_it() {
        // Arrange
        Shipment shipment = ShipmentIn(ShipmentStatus.InTransit);

        // Act
        shipment.Deliver(Delivered);

        // Assert
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(Delivered, shipment.DeliveredAt);
        Assert.Equal(Departed, shipment.DepartedAt);
    }

    [Fact]
    public void A_shipment_can_be_delivered_at_the_very_instant_it_departed() {
        // Arrange
        Shipment shipment = ShipmentIn(ShipmentStatus.InTransit);

        // Act
        shipment.Deliver(Departed);

        // Assert
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(Departed, shipment.DeliveredAt);
    }

    [Fact]
    public void A_shipment_cannot_be_delivered_before_it_departed_and_stays_in_transit() {
        // Arrange
        Shipment shipment = ShipmentIn(ShipmentStatus.InTransit);

        // Assert
        Assert.Throws<ArgumentException>(() => shipment.Deliver(Departed.AddTicks(-1)));
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Null(shipment.DeliveredAt);
    }

    [Theory]
    [InlineData(ShipmentStatus.Booked)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void Only_a_shipment_in_transit_can_be_delivered(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status);

        // Assert
        Assert.Throws<InvalidOperationException>(() => shipment.Deliver(Calendar.At(40)));
        Assert.Equal(status, shipment.Status);
    }

    [Fact]
    public void Cancelling_a_booked_shipment_cancels_it() {
        // Arrange
        Shipment shipment = ShipmentIn(ShipmentStatus.Booked);

        // Act
        shipment.Cancel();

        // Assert
        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
        Assert.Null(shipment.DepartedAt);
        Assert.Null(shipment.DeliveredAt);
    }

    [Theory]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void Only_a_booked_shipment_can_be_cancelled(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status);

        // Assert
        Assert.Throws<InvalidOperationException>(() => shipment.Cancel());
        Assert.Equal(status, shipment.Status);
    }

    [Theory]
    [InlineData(ShipmentStatus.Booked)]
    [InlineData(ShipmentStatus.InTransit)]
    public void A_shipment_not_yet_arrived_is_late_only_after_its_estimated_arrival(ShipmentStatus status) {
        // Arrange: due on day 31
        Shipment shipment = ShipmentIn(status);

        // Assert
        Assert.False(shipment.IsLateAt(Calendar.At(31).AddTicks(-1)));
        Assert.False(shipment.IsLateAt(Calendar.At(31)));
        Assert.True(shipment.IsLateAt(Calendar.At(31).AddTicks(1)));
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void A_closed_shipment_is_never_late(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status);

        // Assert
        Assert.False(shipment.IsLateAt(Calendar.At(31).AddDays(365)));
    }

    [Fact]
    public void Lateness_compares_instants_not_clock_readings() {
        // Arrange: due 2026-03-31 00:00 UTC; "now" is 2026-03-31 01:00 at +02:00, i.e. 2026-03-30 23:00 UTC
        Shipment       shipment = ShipmentIn(ShipmentStatus.InTransit);
        DateTimeOffset now      = new DateTimeOffset(2026, 3, 31, 1, 0, 0, TimeSpan.FromHours(2));

        // Assert
        Assert.False(shipment.IsLateAt(now));
    }

}
