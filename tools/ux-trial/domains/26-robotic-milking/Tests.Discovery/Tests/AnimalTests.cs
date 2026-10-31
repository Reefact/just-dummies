using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery.Tests;

[Reproducible]
public sealed class AnimalTests {

    [Fact]
    public void A_cow_with_four_blind_quarters_cannot_be_registered() {
        // Arrange
        AnyAnimal animal = Any.Animal().WithBlindQuarters(Enum.GetValues<UdderQuarter>());

        // Act
        Action act = () => animal.Generate();

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(act);
        Assert.Equal("blindQuarters", error.ParamName);
    }

    [Fact]
    public void A_blind_quarter_is_not_milkable() {
        // Arrange
        AnyAnimal animal = Any.Animal().WithBlindQuarters([UdderQuarter.LeftRear]);

        // Act
        IReadOnlyList<UdderQuarter> milkable = animal.Generate().MilkableQuarters;

        // Assert
        Assert.Equal([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.RightRear], milkable);
    }

}
