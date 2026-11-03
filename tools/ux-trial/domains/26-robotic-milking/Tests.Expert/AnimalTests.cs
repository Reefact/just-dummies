using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class AnimalTests {

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void The_first_and_the_twentieth_lactation_are_accepted(int lactationNumber) {
        // Act
        Animal cow = Any.Animal().WithLactationNumber(lactationNumber).Generate();

        // Assert
        Assert.Equal(lactationNumber, cow.LactationNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void A_lactation_outside_one_to_twenty_is_refused(int lactationNumber) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Animal().WithLactationNumber(lactationNumber).Generate());

        // Assert
        Assert.Equal("lactationNumber", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public void The_day_of_calving_and_the_nine_hundred_ninety_ninth_day_in_milk_are_accepted(int daysInMilk) {
        // Act
        Animal cow = Any.Animal().WithDaysInMilk(daysInMilk).Generate();

        // Assert
        Assert.Equal(daysInMilk, cow.DaysInMilk);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Days_in_milk_outside_zero_to_nine_hundred_ninety_nine_are_refused(int daysInMilk) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Animal().WithDaysInMilk(daysInMilk).Generate());

        // Assert
        Assert.Equal("daysInMilk", refusal.ParamName);
    }

    [Fact]
    public void A_cow_without_an_id_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Any.Animal().WithId((AnimalId)null!).Generate());
    }

    [Fact]
    public void A_cow_without_a_list_of_blind_quarters_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Any.Animal().WithBlindQuarters((IEnumerable<UdderQuarter>)null!).Generate());
    }

    [Fact]
    public void A_cow_keeps_its_id() {
        // Arrange
        AnimalId id = new AnimalId("FR123456789012");

        // Act
        Animal cow = Any.Animal().WithId(id).Generate();

        // Assert
        Assert.Same(id, cow.Id);
    }

    [Fact]
    public void A_cow_with_no_blind_quarter_is_milked_on_all_four_in_the_declared_order() {
        // Act
        Animal cow = Any.Animal().WithBlindQuarters(Array.Empty<UdderQuarter>()).Generate();

        // Assert
        Assert.Empty(cow.BlindQuarters);
        Assert.Equal(new[] { UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear, UdderQuarter.RightRear }, cow.MilkableQuarters);
    }

    [Fact]
    public void Blind_quarters_are_never_milkable() {
        // Act
        Animal cow = Any.Animal().WithBlindQuarters(new[] { UdderQuarter.RightFront, UdderQuarter.LeftRear }).Generate();

        // Assert
        Assert.Equal(new[] { UdderQuarter.LeftFront, UdderQuarter.RightRear }, cow.MilkableQuarters);
        Assert.False(cow.IsMilkable(UdderQuarter.RightFront));
        Assert.False(cow.IsMilkable(UdderQuarter.LeftRear));
        Assert.True(cow.IsMilkable(UdderQuarter.LeftFront));
        Assert.True(cow.IsMilkable(UdderQuarter.RightRear));
    }

    [Fact]
    public void A_cow_with_three_blind_quarters_still_has_one_to_milk() {
        // Act
        Animal cow = Any.Animal().WithBlindQuarters(new[] { UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear }).Generate();

        // Assert
        Assert.Equal(new[] { UdderQuarter.RightRear }, cow.MilkableQuarters);
    }

    [Fact]
    public void A_cow_with_four_blind_quarters_cannot_be_milked_and_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.Animal().WithBlindQuarters(Enum.GetValues<UdderQuarter>()).Generate());

        // Assert
        Assert.Equal("blindQuarters", refusal.ParamName);
    }

    [Fact]
    public void A_quarter_listed_twice_is_blind_once() {
        // Act
        Animal cow = Any.Animal().WithBlindQuarters(new[] { UdderQuarter.LeftFront, UdderQuarter.LeftFront, UdderQuarter.LeftFront, UdderQuarter.LeftFront }).Generate();

        // Assert
        Assert.Equal(new[] { UdderQuarter.LeftFront }, cow.BlindQuarters);
        Assert.Equal(3, cow.MilkableQuarters.Count);
    }

    [Fact]
    public void A_blind_quarter_the_udder_does_not_have_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Animal().WithBlindQuarters(new[] { (UdderQuarter)7 }).Generate());

        // Assert
        Assert.Equal("blindQuarters", refusal.ParamName);
    }

    [Fact]
    public void A_cow_never_milked_by_the_robot_has_no_last_milking() {
        // Act
        Animal cow = Any.Animal().WithLastMilkedAt((DateTimeOffset?)null).Generate();

        // Assert
        Assert.Null(cow.LastMilkedAt);
    }

    [Fact]
    public void A_cow_keeps_the_moment_it_was_last_milked() {
        // Arrange
        DateTimeOffset lastMilkedAt = new DateTimeOffset(2025, 11, 3, 5, 45, 0, TimeSpan.FromHours(1));

        // Act
        Animal cow = Any.Animal().WithLastMilkedAt(lastMilkedAt).Generate();

        // Assert
        Assert.Equal(lastMilkedAt, cow.LastMilkedAt);
    }

}
