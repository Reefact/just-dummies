using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class StowageAssignmentTests {

    [Fact]
    public void A_twenty_foot_box_can_be_assigned_to_an_odd_bay() {
        // Arrange
        Container    container = Any.Container().OfTwentyFeet().Generate();
        SlotPosition slot      = Any.SlotPosition().WithBay(b => b.Odd()).Generate();

        // Act
        StowageAssignment assignment = new StowageAssignment(container, slot);

        // Assert
        Assert.Same(container, assignment.Container);
        Assert.Same(slot, assignment.Slot);
    }

    [Fact]
    public void A_forty_foot_box_can_be_assigned_to_an_even_bay() {
        // Arrange
        Container    container = Any.Container().OfFortyFeet().Generate();
        SlotPosition slot      = Any.SlotPosition().WithBay(b => b.Even()).Generate();

        // Act
        StowageAssignment assignment = new StowageAssignment(container, slot);

        // Assert
        Assert.Same(container, assignment.Container);
        Assert.Same(slot, assignment.Slot);
    }

    [Fact]
    public void A_twenty_foot_box_cannot_be_assigned_to_an_even_bay() {
        // Arrange
        Container    container = Any.Container().OfTwentyFeet().Generate();
        SlotPosition slot      = Any.SlotPosition().WithBay(b => b.Even()).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StowageAssignment(container, slot));

        // Assert
        Assert.Equal("slot", refusal.ParamName);
        Assert.Contains($"Bay {slot.Bay} takes FortyFoot", refusal.Message);
    }

    [Fact]
    public void A_forty_foot_box_cannot_be_assigned_to_an_odd_bay() {
        // Arrange
        Container    container = Any.Container().OfFortyFeet().Generate();
        SlotPosition slot      = Any.SlotPosition().WithBay(b => b.Odd()).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StowageAssignment(container, slot));

        // Assert
        Assert.Equal("slot", refusal.ParamName);
        Assert.Contains($"Bay {slot.Bay} takes TwentyFoot", refusal.Message);
    }

    [Fact]
    public void A_missing_container_or_slot_is_refused() {
        // Arrange
        Container    anyContainer = Any.Container().OfTwentyFeet().Generate();
        SlotPosition anySlot      = Any.SlotPosition().Accepting(ContainerSize.TwentyFoot).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new StowageAssignment(null!, anySlot));
        Assert.Throws<ArgumentNullException>(() => new StowageAssignment(anyContainer, null!));
    }

}
