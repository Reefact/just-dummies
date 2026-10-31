using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Regular;

public sealed class ValueObjectTests {

    [Fact]
    public void A_reefer_does_not_accept_explosives() {
        // Arrange
        AnyContainer explosivesInAReefer = Arbitrary.Container().WithHazard(HazardClass.Explosives).WithIsReefer(true);

        // Act
        Action build = () => explosivesInAReefer.Generate();

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void A_twenty_foot_box_cannot_carry_a_forty_foot_tare() {
        // Arrange
        AnyContainer heavyTwentyFoot = Arbitrary.Container().WithSize(ContainerSize.TwentyFoot).WithTareTonnes(3.4m);

        // Act
        Action build = () => heavyTwentyFoot.Generate();

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(build);
        Assert.Equal("tareTonnes", error.ParamName);
    }

    [Fact]
    public void General_cargo_can_stand_beside_any_dangerous_goods() {
        // Arrange
        Container anyGeneralCargo = Arbitrary.Container().WithHazard(HazardClass.None).Generate();
        Container anyDangerous    = Arbitrary.Container().WithHazard(h => h.DifferentFrom(HazardClass.None)).Generate();

        // Act
        bool canNeighbour = anyGeneralCargo.CanNeighbour(anyDangerous);

        // Assert
        Assert.True(canNeighbour);
    }

    [Fact]
    public void Explosives_stand_beside_no_other_dangerous_goods() {
        // Arrange
        Container anyExplosives = Arbitrary.Container().WithHazard(HazardClass.Explosives).Generate();
        Container anyDangerous  = Arbitrary.Container().WithHazard(h => h.DifferentFrom(HazardClass.None)).Generate();

        // Act
        bool canNeighbour = anyDangerous.CanNeighbour(anyExplosives);

        // Assert
        Assert.False(canNeighbour);
    }

    [Fact]
    public void Corrosives_may_stand_beside_toxic_goods() {
        // Arrange
        Container anyCorrosives = Arbitrary.Container().WithHazard(HazardClass.Corrosives).Generate();
        Container anyToxic      = Arbitrary.Container().WithHazard(HazardClass.Toxic).Generate();

        // Act
        bool canNeighbour = anyCorrosives.CanNeighbour(anyToxic);

        // Assert
        Assert.True(canNeighbour);
    }

    [Fact]
    public void A_container_number_with_a_wrong_check_digit_is_rejected() {
        // Arrange
        const string wrongCheckDigit = "CSQU3054384"; // CSQU3054383 is the valid number

        // Act
        Action parse = () => new ContainerId(wrongCheckDigit);

        // Assert
        Assert.Throws<ArgumentException>(parse);
    }

    [Fact]
    public void The_first_rows_to_port_and_to_starboard_both_touch_the_centreline_but_not_each_other() {
        // Arrange
        SlotPosition anyCentreline = Arbitrary.SlotPosition().WithRow(0).Generate();
        SlotPosition firstStarboard = new(anyCentreline.Bay, 1, anyCentreline.Tier);
        SlotPosition firstPort      = new(anyCentreline.Bay, 2, anyCentreline.Tier);

        // Act
        bool starboardTouchesCentre = firstStarboard.IsAdjacentTo(anyCentreline);
        bool portTouchesCentre      = firstPort.IsAdjacentTo(anyCentreline);
        bool sidesTouch             = firstStarboard.IsAdjacentTo(firstPort);

        // Assert
        Assert.True(starboardTouchesCentre);
        Assert.True(portTouchesCentre);
        Assert.False(sidesTouch);
    }

    [Fact]
    public void The_top_hold_tier_does_not_touch_the_lowest_deck_tier() {
        // Arrange
        SlotPosition anyTopOfHold = Arbitrary.SlotPosition().WithTier(20).Generate();
        SlotPosition deckAbove    = new(anyTopOfHold.Bay, anyTopOfHold.Row, 82);

        // Act
        bool adjacent = anyTopOfHold.IsAdjacentTo(deckAbove);

        // Assert
        Assert.False(adjacent);
    }

}
