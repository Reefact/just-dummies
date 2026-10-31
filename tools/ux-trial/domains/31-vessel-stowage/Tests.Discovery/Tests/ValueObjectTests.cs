using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Discovery.Tests;

public sealed class ValueObjectTests {

    [Fact]
    public void A_container_number_with_a_wrong_check_digit_is_refused() {
        // Arrange
        ContainerId anyId           = Any.ContainerId().Generate();
        char        wrongCheckDigit = (char)('0' + (anyId.Value[10] - '0' + 1) % 10);
        string      tampered        = anyId.Value[..10] + wrongCheckDigit;

        // Act
        ArgumentException? refusal = Record.Exception(() => new ContainerId(tampered)) as ArgumentException;

        // Assert
        Assert.NotNull(refusal);
        Assert.Contains("Check digit", refusal.Message);
    }

    [Fact]
    public void A_known_iso_6346_container_number_is_accepted() {
        // Act
        ContainerId id = new ContainerId("CSQU3054383");

        // Assert
        Assert.Equal("CSQU3054383", id.Value);
    }

    [Fact]
    public void A_un_locode_cannot_use_the_digit_1_in_its_location_part() {
        // Act
        Exception? refusal = Record.Exception(() => new PortCode("NLRT1"));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_rotation_refuses_a_port_called_twice() {
        // Arrange
        List<PortCode> anyPorts = Any.ListOf(Any.PortCode()).WithCount(2).Distinct().Generate();
        PortCode[]     ports    = [anyPorts[0], anyPorts[1], anyPorts[0]];

        // Act
        Exception? refusal = Record.Exception(() => new Rotation(ports));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_rotation_gives_the_call_order_of_its_ports() {
        // Arrange
        List<PortCode> anyPorts    = Any.ListOf(Any.PortCode()).WithCount(4).Distinct().Generate();
        Rotation       anyRotation = Any.Rotation().WithPorts(anyPorts).Generate();

        // Act
        int index = anyRotation.IndexOf(anyPorts[2]);

        // Assert
        Assert.Equal(2, index);
    }

    [Fact]
    public void An_odd_row_lies_to_starboard_by_whole_row_widths() {
        // Arrange
        SlotPosition anySlotInRow3 = Any.SlotPosition().WithRow(3).Generate();

        // Act
        double offset = anySlotInRow3.TransverseOffsetMetres;

        // Assert
        Assert.Equal(4.88, offset, precision: 10);
    }

    [Fact]
    public void An_even_row_lies_to_port() {
        // Arrange
        SlotPosition anySlotInRow4 = Any.SlotPosition().WithRow(4).Generate();

        // Act
        int index = anySlotInRow4.TransverseIndex;

        // Assert
        Assert.Equal(-2, index);
    }

    [Fact]
    public void The_lowest_deck_tier_is_the_first_level_of_the_deck_section() {
        // Arrange
        SlotPosition anyLowestDeckSlot = Any.SlotPosition().WithTier(82).Generate();

        // Act
        SlotPosition? below = anyLowestDeckSlot.Below();

        // Assert
        Assert.True(anyLowestDeckSlot.IsOnDeck);
        Assert.True(anyLowestDeckSlot.IsLowestOfSection);
        Assert.Null(below);
    }

    [Fact]
    public void An_odd_bay_takes_twenty_foot_containers() {
        // Arrange
        int          anyOddBay = Any.Int32().Between(0, 49).As(n => 2 * n + 1).Generate();
        SlotPosition anySlot   = Any.SlotPosition().WithBay(anyOddBay).Generate();

        // Act
        ContainerSize accepted = anySlot.AcceptedSize;

        // Assert
        Assert.Equal(ContainerSize.TwentyFoot, accepted);
    }

    [Fact]
    public void Slots_one_row_apart_in_the_same_bay_and_section_are_adjacent() {
        // Arrange
        int anyBay  = Any.Int32().Between(1, 99).Generate();
        int anyTier = Any.Int32().OneOf(2, 4, 6, 82, 84, 86).Generate();
        SlotPosition centreline = new SlotPosition(anyBay, 0, anyTier);
        SlotPosition starboard  = new SlotPosition(anyBay, 1, anyTier);

        // Act
        bool adjacent = centreline.IsAdjacentTo(starboard);

        // Assert
        Assert.True(adjacent);
    }

    [Fact]
    public void The_same_row_and_tier_in_another_bay_is_not_adjacent() {
        // Arrange
        SlotPosition anySlot  = Any.SlotPosition().WithBay(b => b.LessThan(99)).Generate();
        SlotPosition nextBay  = new SlotPosition(anySlot.Bay + 1, anySlot.Row, anySlot.Tier);

        // Act
        bool adjacent = anySlot.IsAdjacentTo(nextBay);

        // Assert
        Assert.False(adjacent);
    }

}
