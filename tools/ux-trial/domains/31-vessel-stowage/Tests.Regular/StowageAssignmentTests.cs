using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowageAssignmentTests {

    [Fact]
    public void An_assignment_keeps_its_container_and_its_slot() {
        // Arrange
        SlotPosition anySlot = Any.SlotPosition().Generate();
        Container anyContainer = Any.Container().WithSize(anySlot.AcceptedSize).Generate();

        // Act
        StowageAssignment assignment = new StowageAssignment(anyContainer, anySlot);

        // Assert
        Assert.Same(anyContainer, assignment.Container);
        Assert.Same(anySlot, assignment.Slot);
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 7)]
    [InlineData(ContainerSize.FortyFoot, 8)]
    public void A_box_goes_in_a_bay_that_takes_its_size(ContainerSize size, int bay) {
        // Arrange
        Container anyContainer = Any.Container().WithSize(size).Generate();
        SlotPosition slot      = Any.SlotPosition().WithBay(bay).Generate();

        // Act
        StowageAssignment assignment = new StowageAssignment(anyContainer, slot);

        // Assert
        Assert.Equal(bay, assignment.Slot.Bay);
    }

    [Theory]
    [InlineData(ContainerSize.TwentyFoot, 8)]
    [InlineData(ContainerSize.FortyFoot, 7)]
    public void A_box_in_a_bay_that_takes_the_other_size_is_refused(ContainerSize size, int bay) {
        // Arrange
        Container anyContainer = Any.Container().WithSize(size).Generate();
        SlotPosition slot      = Any.SlotPosition().WithBay(bay).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("slot", () => new StowageAssignment(anyContainer, slot));

        // Assert
        Assert.Contains($"Bay {bay} takes", refusal.Message);
    }

    [Fact]
    public void A_missing_container_or_slot_is_refused() {
        // Arrange
        Container anyContainer = Any.Container().Generate();
        SlotPosition anySlot   = Any.SlotPosition().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("container", () => new StowageAssignment(null!, anySlot));
        Assert.Throws<ArgumentNullException>("slot", () => new StowageAssignment(anyContainer, null!));
    }

    [Fact]
    public void A_drawn_assignment_puts_the_box_in_a_bay_that_takes_its_size() {
        // Arrange
        StowageAssignment anyAssignment = Any.StowageAssignment().Generate();

        // Assert
        Assert.Equal(anyAssignment.Container.Size, anyAssignment.Slot.AcceptedSize);
    }

}
