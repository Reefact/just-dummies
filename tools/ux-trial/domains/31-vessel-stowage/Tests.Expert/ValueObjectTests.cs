using JustDummies;

using VesselStowage.Domain;

using Xunit;

namespace VesselStowage.Tests.Expert;

public sealed class ValueObjectTests {

    [Fact]
    public void A_container_number_with_a_wrong_check_digit_is_refused() {
        // Arrange
        ContainerId anyId      = Any.ContainerId().Generate();
        int         checkDigit = anyId.Value[10] - '0';
        string      misread    = anyId.Value[..10] + (checkDigit + 1) % 10;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerId(misread));

        // Assert
        Assert.Contains("Check digit", refusal.Message);
    }

    [Fact]
    public void A_rotation_calls_each_port_once() {
        // Arrange
        Rotation   anyRotation  = Any.Rotation().Generate();
        PortCode[] callingTwice = [.. anyRotation.Ports, anyRotation.Ports[0]];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Rotation(callingTwice));

        // Assert
        Assert.Equal("ports", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, ContainerSize.TwentyFoot)]
    [InlineData(3, ContainerSize.TwentyFoot)]
    [InlineData(2, ContainerSize.FortyFoot)]
    [InlineData(42, ContainerSize.FortyFoot)]
    public void Odd_bays_take_twenty_foot_boxes_and_even_bays_forty_foot_boxes(int bay, ContainerSize expected) {
        // Arrange
        SlotPosition anySlotOfBay = Any.SlotPosition().WithBay(bay).Generate();

        // Act
        ContainerSize accepted = anySlotOfBay.AcceptedSize;

        // Assert
        Assert.Equal(expected, accepted);
    }

    [Fact]
    public void The_first_starboard_and_first_port_rows_touch_the_centreline_but_not_each_other() {
        // Arrange
        SlotPosition anyCentreline = Any.SlotPosition().WithRow(0).Generate();
        SlotPosition starboard     = new(anyCentreline.Bay, 1, anyCentreline.Tier);
        SlotPosition port          = new(anyCentreline.Bay, 2, anyCentreline.Tier);

        // Act
        bool starboardTouchesCentreline = starboard.IsAdjacentTo(anyCentreline);
        bool portTouchesCentreline      = port.IsAdjacentTo(anyCentreline);
        bool starboardTouchesPort       = starboard.IsAdjacentTo(port);

        // Assert
        Assert.True(starboardTouchesCentreline);
        Assert.True(portTouchesCentreline);
        Assert.False(starboardTouchesPort);
    }

}
