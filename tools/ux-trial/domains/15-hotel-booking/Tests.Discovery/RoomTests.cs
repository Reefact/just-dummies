using JustDummies;

using Xunit;

namespace Booking.Domain;

public sealed class RoomTests {

    [Theory]
    [InlineData(RoomType.Single, 1)]
    [InlineData(RoomType.Double, 1)]
    [InlineData(RoomType.Double, 2)]
    [InlineData(RoomType.Twin, 2)]
    [InlineData(RoomType.Suite, 1)]
    [InlineData(RoomType.Suite, 4)]
    public void A_capacity_within_what_the_type_sleeps_is_accepted(RoomType type, int capacity) {
        // Arrange
        RoomNumber anyNumber = Any.RoomNumber().Generate();
        Money      anyRate   = Any.Room().Generate().NightlyRate;

        // Act
        Room room = new Room(anyNumber, type, capacity, anyRate);

        // Assert
        Assert.Equal(type, room.Type);
        Assert.Equal(capacity, room.Capacity);
        Assert.Same(anyNumber, room.Number);
        Assert.Same(anyRate, room.NightlyRate);
    }

    [Theory]
    [InlineData(RoomType.Single, 2)]
    [InlineData(RoomType.Double, 3)]
    [InlineData(RoomType.Twin, 3)]
    [InlineData(RoomType.Suite, 5)]
    public void A_capacity_above_what_the_type_sleeps_is_refused(RoomType type, int capacity) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Room().WithType(type).WithCapacity(capacity).Generate());

        // Assert
        Assert.Equal("capacity", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_capacity_below_one_is_refused(int capacity) {
        // Arrange
        RoomType anyType = Any.Enum<RoomType>().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Room().WithType(anyType).WithCapacity(capacity).Generate());

        // Assert
        Assert.Equal("capacity", error.ParamName);
    }

    [Fact]
    public void An_undefined_room_type_is_refused() {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Room().WithType((RoomType)99).Generate());

        Assert.Equal("type", error.ParamName);
    }

    [Fact]
    public void The_type_is_checked_before_the_capacity() {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Room().WithType((RoomType)99).WithCapacity(0).Generate());

        Assert.Equal("type", error.ParamName);
    }

    [Fact]
    public void A_free_room_is_refused() {
        // Arrange
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Room().WithNightlyRate(new Money(0m, anyCurrency)).Generate());

        // Assert
        Assert.Equal("nightlyRate", error.ParamName);
    }

    [Fact]
    public void The_capacity_is_checked_before_the_rate() {
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        ArgumentException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Room().WithCapacity(0).WithNightlyRate(new Money(0m, anyCurrency)).Generate());

        Assert.Equal("capacity", error.ParamName);
    }

    [Fact]
    public void A_one_cent_rate_is_accepted() {
        string anyCurrency = Any.StringMatching("[A-Z]{3}").Generate();

        Room room = Any.Room().WithNightlyRate(new Money(0.01m, anyCurrency)).Generate();

        Assert.Equal(0.01m, room.NightlyRate.Amount);
    }

    [Fact]
    public void A_missing_number_is_refused() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Room().WithNumber((RoomNumber)null!).Generate());

        Assert.Equal("number", error.ParamName);
    }

    [Fact]
    public void A_missing_rate_is_refused() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Room().WithNightlyRate((Money)null!).Generate());

        Assert.Equal("nightlyRate", error.ParamName);
    }

}
