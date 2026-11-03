using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class AnimalTests {

    [Fact]
    public void A_cow_keeps_what_she_was_registered_with() {
        // Arrange
        AnimalId                  anyId        = Any.AnimalId().Generate();
        DateTimeOffset            lastMilkedAt = new DateTimeOffset(2026, 3, 14, 5, 30, 0, TimeSpan.Zero);
        UdderQuarter[]            blind        = [UdderQuarter.LeftRear];

        // Act
        Animal animal = new Animal(anyId, lactationNumber: 3, daysInMilk: 120, blind, lastMilkedAt);

        // Assert
        Assert.Same(anyId, animal.Id);
        Assert.Equal(3, animal.LactationNumber);
        Assert.Equal(120, animal.DaysInMilk);
        Assert.Equal(lastMilkedAt, animal.LastMilkedAt);
    }

    [Fact]
    public void A_cow_never_milked_by_the_robot_has_no_last_milking() {
        // Act
        Animal anyNeverMilkedCow = Any.Animal().WithoutLastMilkedAt().Generate();

        // Assert
        Assert.Null(anyNeverMilkedCow.LastMilkedAt);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(19)]
    [InlineData(20)]
    public void A_lactation_number_from_one_to_twenty_is_accepted(int lactationNumber) {
        Animal animal = Any.Animal().WithLactationNumber(lactationNumber).Generate();

        Assert.Equal(lactationNumber, animal.LactationNumber);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(int.MaxValue)]
    public void A_lactation_number_outside_one_to_twenty_is_refused(int lactationNumber) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Animal().WithLactationNumber(lactationNumber).Generate());

        Assert.Equal("lactationNumber", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(998)]
    [InlineData(999)]
    public void Days_in_milk_from_zero_to_nine_hundred_ninety_nine_are_accepted(int daysInMilk) {
        Animal animal = Any.Animal().WithDaysInMilk(daysInMilk).Generate();

        Assert.Equal(daysInMilk, animal.DaysInMilk);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void Days_in_milk_outside_zero_to_nine_hundred_ninety_nine_are_refused(int daysInMilk) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Animal().WithDaysInMilk(daysInMilk).Generate());

        Assert.Equal("daysInMilk", refusal.ParamName);
    }

    [Fact]
    public void A_cow_without_blind_quarter_has_four_milkable_quarters() {
        // Act
        Animal anySoundCow = Any.Animal().WithBlindQuarters(Array.Empty<UdderQuarter>()).Generate();

        // Assert
        Assert.Empty(anySoundCow.BlindQuarters);
        Assert.Equal(4, anySoundCow.MilkableQuarters.Count);
        Assert.All(Enum.GetValues<UdderQuarter>(), q => Assert.True(anySoundCow.IsMilkable(q)));
    }

    [Fact]
    public void Blind_quarters_are_left_out_of_the_milkable_ones() {
        // Act
        Animal anyCow = Any.Animal().WithBlindQuarters([UdderQuarter.LeftFront, UdderQuarter.RightRear]).Generate();

        // Assert
        Assert.Equal([UdderQuarter.RightFront, UdderQuarter.LeftRear], anyCow.MilkableQuarters);
        Assert.False(anyCow.IsMilkable(UdderQuarter.LeftFront));
        Assert.False(anyCow.IsMilkable(UdderQuarter.RightRear));
        Assert.True(anyCow.IsMilkable(UdderQuarter.RightFront));
        Assert.True(anyCow.IsMilkable(UdderQuarter.LeftRear));
    }

    [Fact]
    public void A_cow_can_have_three_blind_quarters_if_one_is_left_to_milk() {
        // Act
        Animal anyCow = Any.Animal().WithBlindQuarters([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear]).Generate();

        // Assert
        Assert.Equal([UdderQuarter.RightRear], anyCow.MilkableQuarters);
    }

    [Fact]
    public void A_cow_with_four_blind_quarters_is_refused() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.Animal().WithBlindQuarters(Enum.GetValues<UdderQuarter>()).Generate());

        Assert.Equal("blindQuarters", refusal.ParamName);
    }

    [Fact]
    public void A_blind_quarter_declared_twice_counts_once() {
        // Act
        Animal anyCow = Any.Animal()
                           .WithBlindQuarters([UdderQuarter.LeftFront, UdderQuarter.LeftFront, UdderQuarter.LeftFront, UdderQuarter.LeftFront])
                           .Generate();

        // Assert
        Assert.Equal([UdderQuarter.LeftFront], anyCow.BlindQuarters);
        Assert.Equal(3, anyCow.MilkableQuarters.Count);
    }

    [Fact]
    public void An_unknown_udder_quarter_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Animal().WithBlindQuarters([(UdderQuarter)42]).Generate());

        Assert.Equal("blindQuarters", refusal.ParamName);
    }

    [Fact]
    public void A_missing_id_is_refused() {
        Assert.Throws<ArgumentNullException>(
            () => new Animal(null!, lactationNumber: 2, daysInMilk: 10, Array.Empty<UdderQuarter>(), lastMilkedAt: null));
    }

    [Fact]
    public void A_missing_list_of_blind_quarters_is_refused() {
        // Arrange
        AnimalId anyId = Any.AnimalId().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Animal(anyId, lactationNumber: 2, daysInMilk: 10, null!, lastMilkedAt: null));
    }

    [Fact]
    public void Blind_quarters_cannot_be_changed_through_the_collection_the_cow_was_built_from() {
        // Arrange
        List<UdderQuarter> blind = [UdderQuarter.LeftFront];
        Animal             anyCow = Any.Animal().WithBlindQuarters(blind).Generate();

        // Act
        blind.Add(UdderQuarter.RightFront);

        // Assert
        Assert.Equal([UdderQuarter.LeftFront], anyCow.BlindQuarters);
    }

}
