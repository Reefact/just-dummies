using JustDummies;

using Xunit;

namespace Booking.Domain.Tests;

public sealed class RoomTests {

    [Fact]
    public void A_room_keeps_everything_it_was_given() {
        // Arrange
        RoomNumber number = new RoomNumber("412");
        Money      rate   = new Money(150m, "EUR");

        // Act
        Room room = new Room(number, RoomType.Suite, 3, rate);

        // Assert
        Assert.Same(number, room.Number);
        Assert.Equal(RoomType.Suite, room.Type);
        Assert.Equal(3, room.Capacity);
        Assert.Same(rate, room.NightlyRate);
    }

    [Theory]
    [InlineData(RoomType.Single, 1)]
    [InlineData(RoomType.Double, 1)]
    [InlineData(RoomType.Double, 2)]
    [InlineData(RoomType.Twin, 1)]
    [InlineData(RoomType.Twin, 2)]
    [InlineData(RoomType.Suite, 1)]
    [InlineData(RoomType.Suite, 4)]
    public void A_room_sleeps_at_most_what_its_type_allows(RoomType type, int capacity) {
        // Arrange
        AnyRoom rooms = Any.Room().WithType(type).WithCapacity(capacity);

        // Act
        Room room = rooms.Generate();

        // Assert
        Assert.Equal(capacity, room.Capacity);
    }

    [Theory]
    [InlineData(RoomType.Single, 2)]
    [InlineData(RoomType.Double, 3)]
    [InlineData(RoomType.Twin, 3)]
    [InlineData(RoomType.Suite, 5)]
    public void A_room_cannot_sleep_more_than_its_type_allows(RoomType type, int capacity) {
        // Arrange
        AnyRoom rooms = Any.Room().WithType(type).WithCapacity(capacity);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => rooms.Generate());

        // Assert
        Assert.Equal("capacity", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_room_sleeps_at_least_one(int capacity) {
        // Arrange
        AnyRoom rooms = Any.Room().WithCapacity(capacity);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => rooms.Generate());

        // Assert
        Assert.Equal("capacity", refusal.ParamName);
    }

    [Fact]
    public void A_type_that_is_not_declared_is_refused() {
        // Arrange
        AnyRoom rooms = Any.Room().WithType((RoomType)99).WithCapacity(1);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => rooms.Generate());

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

    [Fact]
    public void An_absent_number_is_refused_as_null() {
        // Arrange
        AnyRoom rooms = Any.Room().WithNumber((RoomNumber)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => rooms.Generate());

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Fact]
    public void An_absent_nightly_rate_is_refused_as_null() {
        // Arrange
        AnyRoom rooms = Any.Room().WithNightlyRate((Money)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => rooms.Generate());

        // Assert
        Assert.Equal("nightlyRate", refusal.ParamName);
    }

    [Fact]
    public void A_free_room_is_refused() {
        // Arrange
        AnyRoom rooms = Any.Room().WithNightlyRate(Any.Money().WithAmount(0m).Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => rooms.Generate());

        // Assert
        Assert.Equal("nightlyRate", refusal.ParamName);
    }

    [Fact]
    public void A_rate_of_one_cent_is_not_free() {
        // Arrange
        Money oneCent = Any.Money().WithAmount(0.01m).Generate();

        // Act
        Room room = Any.Room().WithNightlyRate(oneCent).Generate();

        // Assert
        Assert.Equal(0.01m, room.NightlyRate.Amount);
    }

    [Fact]
    public void The_type_is_checked_before_the_capacity() {
        // Arrange
        AnyRoom rooms = Any.Room().WithType((RoomType)99).WithCapacity(0);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => rooms.Generate());

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

    [Fact]
    public void The_capacity_is_checked_before_the_rate() {
        // Arrange
        AnyRoom rooms = Any.Room().WithType(RoomType.Single).WithCapacity(2).WithNightlyRate(Any.Money().WithAmount(0m).Generate());

        // Act
        Exception refusal = Assert.ThrowsAny<Exception>(() => rooms.Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

}
