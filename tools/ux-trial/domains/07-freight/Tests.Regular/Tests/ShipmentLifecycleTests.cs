namespace Freight.Domain.Tests;

public sealed class ShipmentLifecycleTests {

    private static readonly DateTimeOffset Noon = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset AnyInstant() {
        return Any.DateTimeOffset().Between(Noon.AddYears(-5), Noon.AddYears(5)).Generate();
    }

    private static Shipment ShipmentIn(ShipmentStatus status, DateTimeOffset departedAt) {
        Shipment shipment = Any.Shipment().Generate();
        switch (status) {
            case ShipmentStatus.Booked:
                break;
            case ShipmentStatus.InTransit:
                shipment.Depart(departedAt);
                break;
            case ShipmentStatus.Delivered:
                shipment.Depart(departedAt);
                shipment.Deliver(departedAt.AddDays(Any.Int32().Between(1, 30).Generate()));
                break;
            case ShipmentStatus.Cancelled:
                shipment.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return shipment;
    }

    // ---- IsLateAt ----

    [Theory]
    [InlineData(ShipmentStatus.Booked)]
    [InlineData(ShipmentStatus.InTransit)]
    public void An_open_shipment_is_late_once_the_estimated_arrival_has_passed(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status, AnyInstant());
        TimeSpan anyDelay = Any.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromDays(400)).Generate();

        // Act
        bool late = shipment.IsLateAt(shipment.EstimatedArrival + anyDelay);

        // Assert
        Assert.True(late);
    }

    [Theory]
    [InlineData(ShipmentStatus.Booked)]
    [InlineData(ShipmentStatus.InTransit)]
    public void An_open_shipment_is_not_late_exactly_at_the_estimated_arrival(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status, AnyInstant());

        // Act
        bool late = shipment.IsLateAt(shipment.EstimatedArrival);

        // Assert
        Assert.False(late);
    }

    [Theory]
    [InlineData(ShipmentStatus.Booked)]
    [InlineData(ShipmentStatus.InTransit)]
    public void An_open_shipment_is_not_late_before_the_estimated_arrival(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status, AnyInstant());
        TimeSpan anyMargin = Any.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromDays(400)).Generate();

        // Act
        bool late = shipment.IsLateAt(shipment.EstimatedArrival - anyMargin);

        // Assert
        Assert.False(late);
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void A_closed_shipment_is_never_late(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status, AnyInstant());
        TimeSpan anyDelay = Any.TimeSpan().Between(TimeSpan.FromDays(1), TimeSpan.FromDays(4_000)).Generate();

        // Act
        bool late = shipment.IsLateAt(shipment.EstimatedArrival + anyDelay);

        // Assert
        Assert.False(late);
    }

    [Fact]
    public void Lateness_is_judged_on_instants() {
        // Arrange: it is 23:30 UTC on the local clock of a -01:00 port, i.e. 00:30 UTC the next day.
        Leg leg = Any.Leg()
                     .WithDeparture(Noon)
                     .WithArrival(new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero))
                     .Generate();
        Shipment shipment = Any.Shipment().WithLegs(new[] { leg }).Generate();
        DateTimeOffset now = new DateTimeOffset(2026, 3, 9, 23, 30, 0, TimeSpan.FromHours(-1));

        // Act & Assert
        Assert.True(shipment.IsLateAt(now));
    }

    // ---- Depart ----

    [Fact]
    public void A_booked_shipment_departs() {
        // Arrange
        Shipment       shipment = Any.Shipment().Generate();
        DateTimeOffset anyAt    = AnyInstant();

        // Act
        shipment.Depart(anyAt);

        // Assert
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal(anyAt, shipment.DepartedAt);
        Assert.Null(shipment.DeliveredAt);
    }

    [Fact]
    public void The_departure_is_not_compared_with_the_first_leg() {
        // Arrange
        Shipment shipment = Any.Shipment().Generate();
        TimeSpan anyGap   = Any.TimeSpan().Between(TimeSpan.FromDays(1), TimeSpan.FromDays(3_000)).Generate();

        // Act
        shipment.Depart(shipment.Legs[0].Departure - anyGap);

        // Assert
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
    }

    [Theory]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void Only_a_booked_shipment_departs(ShipmentStatus status) {
        // Arrange
        DateTimeOffset anyDeparture = AnyInstant();
        Shipment       shipment     = ShipmentIn(status, anyDeparture);
        DateTimeOffset? departedBefore = shipment.DepartedAt;

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => shipment.Depart(AnyInstant()));

        // Assert
        Assert.Contains(status.ToString(), refusal.Message);
        Assert.Equal(status, shipment.Status);
        Assert.Equal(departedBefore, shipment.DepartedAt);
    }

    // ---- Deliver ----

    [Fact]
    public void An_in_transit_shipment_is_delivered() {
        // Arrange
        DateTimeOffset anyDeparture = AnyInstant();
        Shipment       shipment     = ShipmentIn(ShipmentStatus.InTransit, anyDeparture);
        DateTimeOffset anyAt        = anyDeparture + Any.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromDays(400)).Generate();

        // Act
        shipment.Deliver(anyAt);

        // Assert
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(anyAt, shipment.DeliveredAt);
        Assert.Equal(anyDeparture, shipment.DepartedAt);
    }

    [Fact]
    public void A_shipment_can_be_delivered_the_instant_it_departed() {
        // Arrange
        DateTimeOffset anyDeparture = AnyInstant();
        Shipment       shipment     = ShipmentIn(ShipmentStatus.InTransit, anyDeparture);

        // Act
        shipment.Deliver(anyDeparture);

        // Assert
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(anyDeparture, shipment.DeliveredAt);
    }

    [Fact]
    public void A_shipment_cannot_be_delivered_before_it_departed() {
        // Arrange
        DateTimeOffset anyDeparture = AnyInstant();
        Shipment       shipment     = ShipmentIn(ShipmentStatus.InTransit, anyDeparture);
        TimeSpan       anyEarlier   = Any.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromDays(400)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => shipment.Deliver(anyDeparture - anyEarlier));

        // Assert
        Assert.Equal("at", refusal.ParamName);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Null(shipment.DeliveredAt);
    }

    [Fact]
    public void Delivery_is_compared_with_the_departure_as_instants() {
        // Arrange: 10:30 on the clock of a +01:00 port is 09:30 UTC, before the 10:00 UTC departure.
        Shipment shipment = Any.Shipment().Generate();
        shipment.Depart(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => shipment.Deliver(new DateTimeOffset(2026, 3, 1, 10, 30, 0, TimeSpan.FromHours(1))));
    }

    [Theory]
    [InlineData(ShipmentStatus.Booked)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void Only_an_in_transit_shipment_is_delivered(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status, AnyInstant());

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => shipment.Deliver(AnyInstant()));

        // Assert
        Assert.Contains(status.ToString(), refusal.Message);
        Assert.Equal(status, shipment.Status);
    }

    [Fact]
    public void The_status_is_checked_before_the_delivery_time() {
        // Arrange
        Shipment shipment = Any.Shipment().Generate();

        // Act & Assert: a booked shipment has no departure to compare with, and says why it cannot be delivered.
        Assert.Throws<InvalidOperationException>(() => shipment.Deliver(DateTimeOffset.MinValue));
    }

    // ---- Cancel ----

    [Fact]
    public void A_booked_shipment_is_cancelled() {
        // Arrange
        Shipment shipment = Any.Shipment().Generate();

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
    public void Only_a_booked_shipment_is_cancelled(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status, AnyInstant());

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => shipment.Cancel());

        // Assert
        Assert.Contains(status.ToString(), refusal.Message);
        Assert.Equal(status, shipment.Status);
    }

    // ---- Terminal states ----

    [Theory]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public void A_closed_shipment_has_no_outgoing_transition(ShipmentStatus status) {
        // Arrange
        Shipment shipment = ShipmentIn(status, AnyInstant());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => shipment.Depart(AnyInstant()));
        Assert.Throws<InvalidOperationException>(() => shipment.Deliver(AnyInstant()));
        Assert.Throws<InvalidOperationException>(() => shipment.Cancel());
        Assert.Equal(status, shipment.Status);
    }

    [Fact]
    public void A_shipment_goes_through_its_whole_life() {
        // Arrange
        Shipment       shipment = Any.Shipment().Generate();
        DateTimeOffset departed = AnyInstant();
        DateTimeOffset arrived  = departed.AddDays(21);

        // Act
        shipment.Depart(departed);
        shipment.Deliver(arrived);

        // Assert
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(departed, shipment.DepartedAt);
        Assert.Equal(arrived, shipment.DeliveredAt);
    }

}
