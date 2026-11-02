using JustDummies;

using Xunit;

namespace Booking.Domain.Tests.Regular;

public sealed class RoomTests {

    private static AnyMoney AnyRate() => Arbitrary.Money().WithAmount(a => a.Between(0.01m, 10_000m));

    [Fact]
    public void A_room_keeps_its_number_its_type_its_capacity_and_its_rate() {
        // Arrange
        RoomNumber number = new RoomNumber("412");
        Money      rate   = new Money(89.90m, "EUR");

        // Act
        Room room = new Room(number, RoomType.Double, 2, rate);

        // Assert
        Assert.Same(number, room.Number);
        Assert.Equal(RoomType.Double, room.Type);
        Assert.Equal(2, room.Capacity);
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
    public void A_capacity_up_to_the_maximum_of_the_type_is_accepted(RoomType type, int capacity) {
        // Arrange
        RoomNumber anyNumber = Arbitrary.RoomNumber().Generate();
        Money      anyRate   = AnyRate().Generate();

        // Act
        Room room = new Room(anyNumber, type, capacity, anyRate);

        // Assert
        Assert.Equal(capacity, room.Capacity);
    }

    [Theory]
    [InlineData(RoomType.Single, 2)]
    [InlineData(RoomType.Double, 3)]
    [InlineData(RoomType.Twin, 3)]
    [InlineData(RoomType.Suite, 5)]
    [InlineData(RoomType.Suite, 100)]
    public void A_capacity_above_the_maximum_of_the_type_is_refused(RoomType type, int capacity) {
        // Arrange
        RoomNumber anyNumber = Arbitrary.RoomNumber().Generate();
        Money      anyRate   = AnyRate().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Room(anyNumber, type, capacity, anyRate));

        // Assert
        Assert.Equal("capacity", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_capacity_below_one_is_refused(int capacity) {
        // Arrange
        RoomNumber anyNumber = Arbitrary.RoomNumber().Generate();
        RoomType   anyType   = Any.Enum<RoomType>().Generate();
        Money      anyRate   = AnyRate().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Room(anyNumber, anyType, capacity, anyRate));

        // Assert
        Assert.Equal("capacity", exception.ParamName);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(99)]
    public void An_undefined_room_type_is_refused(int type) {
        // Arrange
        RoomNumber anyNumber = Arbitrary.RoomNumber().Generate();
        Money      anyRate   = AnyRate().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Room(anyNumber, (RoomType)type, 1, anyRate));

        // Assert
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void A_null_number_is_refused() {
        // Arrange
        Money anyRate = AnyRate().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Room(null!, RoomType.Single, 1, anyRate));
    }

    [Fact]
    public void A_null_rate_is_refused() {
        // Arrange
        RoomNumber anyNumber = Arbitrary.RoomNumber().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Room(anyNumber, RoomType.Single, 1, null!));
    }

    [Fact]
    public void A_free_room_is_refused() {
        // Arrange
        RoomNumber anyNumber   = Arbitrary.RoomNumber().Generate();
        Money      anyZeroRate = Arbitrary.Money().WithAmount(0m).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Room(anyNumber, RoomType.Single, 1, anyZeroRate));

        // Assert
        Assert.Equal("nightlyRate", exception.ParamName);
    }

    [Fact]
    public void A_rate_of_one_cent_is_enough() {
        // Arrange
        RoomNumber anyNumber = Arbitrary.RoomNumber().Generate();

        // Act
        Room room = new Room(anyNumber, RoomType.Single, 1, new Money(0.01m, "EUR"));

        // Assert
        Assert.Equal(0.01m, room.NightlyRate.Amount);
    }

    [Fact]
    public void The_generator_only_draws_rooms_whose_capacity_fits_their_type() {
        // Act
        for (int i = 0; i < 300; i++) {
            Room anyRoom = Arbitrary.Room().Generate();

            Assert.InRange(anyRoom.Capacity, 1, anyRoom.Type == RoomType.Suite ? 4 : anyRoom.Type == RoomType.Single ? 1 : 2);
            Assert.True(anyRoom.NightlyRate.Amount > 0m);
        }
    }

    [Fact]
    public void The_generator_reaches_every_room_type() {
        // Act
        HashSet<RoomType> seen = new HashSet<RoomType>();
        for (int i = 0; i < 300; i++) {
            seen.Add(Arbitrary.Room().Generate().Type);
        }

        // Assert
        Assert.Equal(4, seen.Count);
    }

}
