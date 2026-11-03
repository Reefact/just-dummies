using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class PlacementTests {

    [Theory]
    [InlineData(ComponentKind.Capacitor)]
    [InlineData(ComponentKind.Inductor)]
    [InlineData(ComponentKind.Resistor)]
    public void A_part_is_placed_on_a_designator_of_its_own_kind_and_is_both_designed_and_fitted_there(ComponentKind kind) {
        // Arrange: the assertion is about these very objects being kept, so they are named for what they are, not any...
        ReferenceDesignator designator = Any.ReferenceDesignator().WithKind(kind).Generate();
        Part                part       = Any.Part().WithKind(kind).Generate();

        // Act
        Placement placement = new Placement(designator, part);

        // Assert
        Assert.Same(designator, placement.Designator);
        Assert.Same(part, placement.DesignedPart);
        Assert.Same(part, placement.FittedPart);
        Assert.False(placement.IsSubstituted);
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor, ComponentKind.Inductor)]
    [InlineData(ComponentKind.Capacitor, ComponentKind.Resistor)]
    [InlineData(ComponentKind.Inductor, ComponentKind.Capacitor)]
    [InlineData(ComponentKind.Inductor, ComponentKind.Resistor)]
    [InlineData(ComponentKind.Resistor, ComponentKind.Capacitor)]
    [InlineData(ComponentKind.Resistor, ComponentKind.Inductor)]
    public void A_designator_refuses_a_part_of_another_kind(ComponentKind designatorKind, ComponentKind partKind) {
        // Arrange
        ReferenceDesignator anyDesignator = Any.ReferenceDesignator().WithKind(designatorKind).Generate();
        Part                anyPart       = Any.Part().WithKind(partKind).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Placement(anyDesignator, anyPart));

        // Assert: the message names both sides
        Assert.Contains(anyDesignator.Value, refusal.Message);
        Assert.Contains(anyPart.PartNumber, refusal.Message);
    }

    [Fact]
    public void A_placement_needs_a_designator_and_a_part() {
        // Arrange
        ReferenceDesignator anyDesignator = Any.ReferenceDesignator().Generate();
        Part                anyPart       = Any.Part().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Placement(null!, anyPart));
        Assert.Throws<ArgumentNullException>(() => new Placement(anyDesignator, null!));
    }

    [Fact]
    public void A_placement_reads_as_its_designator_and_part_number() {
        // Arrange
        Placement placement = new Placement(new ReferenceDesignator("R12"), Any.Part().WithKind(ComponentKind.Resistor).WithPartNumber("RC0603FR-07100KL").Generate());

        // Act & Assert
        Assert.Equal("R12: RC0603FR-07100KL", placement.ToString());
    }

}

public sealed class BomLineTests {

    [Fact]
    public void A_line_holds_a_part_and_every_designator_it_is_fitted_on() {
        // Arrange
        Part resistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();
        ReferenceDesignator[] designators = [new ReferenceDesignator("R3"), new ReferenceDesignator("R1")];

        // Act
        BomLine line = new BomLine(resistor, designators);

        // Assert
        Assert.Same(resistor, line.Part);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public void The_designators_are_listed_in_designator_order_whatever_order_they_came_in() {
        // Arrange
        Part anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        // Act
        BomLine line = new BomLine(anyResistor, [new ReferenceDesignator("R10"), new ReferenceDesignator("R2"), new ReferenceDesignator("R101"), new ReferenceDesignator("R1")]);

        // Assert
        Assert.Equal(["R1", "R2", "R10", "R101"], line.Designators.Select(d => d.Value));
    }

    [Fact]
    public void The_extended_cost_is_the_unit_cost_times_the_quantity() {
        // Arrange
        BomLine line = Any.BomLine().WithPart(p => p.WithKind(ComponentKind.Resistor).WithUnitCost(0.0125m)).WithQuantity(3).Generate();

        // Act & Assert
        Assert.Equal(3, line.Quantity);
        Assert.Equal(0.0375m, line.ExtendedCost);
    }

    [Fact]
    public void A_free_part_has_a_free_line() {
        // Arrange
        BomLine line = Any.BomLine().WithPart(p => p.WithUnitCost(0m)).Generate();

        // Act & Assert
        Assert.Equal(0m, line.ExtendedCost);
    }

    [Fact]
    public void A_line_needs_at_least_one_designator() {
        // Arrange
        Part anyPart = Any.Part().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BomLine(anyPart, []));
    }

    [Fact]
    public void A_line_lists_no_null_designator() {
        // Arrange
        Part anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BomLine(anyResistor, [new ReferenceDesignator("R1"), null!]));
    }

    [Fact]
    public void A_line_lists_each_designator_once() {
        // Arrange
        Part anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BomLine(anyResistor, [new ReferenceDesignator("R1"), new ReferenceDesignator("R2"), new ReferenceDesignator("R1")]));
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor, "R1")]
    [InlineData(ComponentKind.Inductor, "C1")]
    [InlineData(ComponentKind.Resistor, "L1")]
    public void Every_designator_of_a_line_is_of_the_kind_of_its_part(ComponentKind partKind, string stray) {
        // Arrange
        Part anyPart = Any.Part().WithKind(partKind).Generate();
        ReferenceDesignator anyOfItsKind = Any.ReferenceDesignator().WithKind(partKind).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new BomLine(anyPart, [anyOfItsKind, new ReferenceDesignator(stray)]));

        // Assert: the stray one is named
        Assert.Contains(stray, refusal.Message);
    }

    [Fact]
    public void A_line_needs_a_part_and_designators() {
        // Arrange
        Part anyPart = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BomLine(null!, [new ReferenceDesignator("R1")]));
        Assert.Throws<ArgumentNullException>(() => new BomLine(anyPart, null!));
    }

    [Fact]
    public void A_line_does_not_follow_the_collection_it_was_made_from() {
        // Arrange
        Part anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();
        List<ReferenceDesignator> designators = [new ReferenceDesignator("R1")];
        BomLine line = new BomLine(anyResistor, designators);

        // Act
        designators.Add(new ReferenceDesignator("R2"));

        // Assert
        Assert.Equal(1, line.Quantity);
    }

    [Fact]
    public void The_designators_of_a_line_cannot_be_changed_by_the_caller() {
        // Arrange
        BomLine anyLine = Any.BomLine().Generate();

        // Act & Assert
        Assert.True(((System.Collections.IList)anyLine.Designators).IsReadOnly);
    }

    [Fact]
    public void A_line_reads_as_its_quantity_part_number_and_designators() {
        // Arrange
        Part part = Any.Part().WithKind(ComponentKind.Resistor).WithPartNumber("RC0603FR-07100KL").Generate();
        BomLine line = new BomLine(part, [new ReferenceDesignator("R2"), new ReferenceDesignator("R1")]);

        // Act & Assert
        Assert.Equal("2 × RC0603FR-07100KL: R1, R2", line.ToString());
    }

}
