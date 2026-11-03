using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class PlacementTests {

    [Fact]
    public void A_new_placement_fits_the_part_it_was_designed_with() {
        Part                anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();
        ReferenceDesignator r12         = TestParts.Designator("R12");

        Placement placement = new Placement(r12, anyResistor);

        Assert.Equal(r12, placement.Designator);
        Assert.Same(anyResistor, placement.DesignedPart);
        Assert.Same(anyResistor, placement.FittedPart);
        Assert.False(placement.IsSubstituted);
    }

    [Theory]
    [InlineData("C1", ComponentKind.Inductor)]
    [InlineData("C1", ComponentKind.Resistor)]
    [InlineData("L1", ComponentKind.Capacitor)]
    [InlineData("L1", ComponentKind.Resistor)]
    [InlineData("R1", ComponentKind.Capacitor)]
    [InlineData("R1", ComponentKind.Inductor)]
    public void Refuses_a_part_of_another_kind_than_the_designator_letter(string designator, ComponentKind partKind) {
        Part anyPart = Any.Part().WithKind(partKind).Generate();

        Assert.Throws<ArgumentException>(() => new Placement(TestParts.Designator(designator), anyPart));
    }

    [Fact]
    public void Refuses_null() {
        Part anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        Assert.Throws<ArgumentNullException>(() => new Placement(null!, anyResistor));
        Assert.Throws<ArgumentNullException>(() => new Placement(TestParts.Designator("R1"), null!));
    }

    [Fact]
    public void Is_shown_with_its_designator_and_part_number() {
        Part resistor = TestParts.Designed().Generate();

        Assert.Equal("R7: RES-100", new Placement(TestParts.Designator("R7"), resistor).ToString());
    }

}

public sealed class BomLineTests {

    [Fact]
    public void Lists_the_part_and_its_designators_in_order() {
        Part anyCapacitor = Any.Part().WithKind(ComponentKind.Capacitor).Generate();

        BomLine line = new BomLine(anyCapacitor, new[] { "C10", "C2", "C1" }.Select(TestParts.Designator));

        Assert.Same(anyCapacitor, line.Part);
        Assert.Equal(new[] { "C1", "C2", "C10" }, line.Designators.Select(d => d.Value));
        Assert.Equal(3, line.Quantity);
    }

    [Fact]
    public void The_extended_cost_is_the_unit_cost_times_the_quantity() {
        Part resistor = Any.Part().WithKind(ComponentKind.Resistor).WithUnitCost(0.0125m).Generate();

        BomLine line = new BomLine(resistor, new[] { "R1", "R2", "R3" }.Select(TestParts.Designator));

        Assert.Equal(0.0375m, line.ExtendedCost);
    }

    [Fact]
    public void A_free_part_costs_nothing_whatever_the_quantity() {
        Part freeResistor = Any.Part().WithKind(ComponentKind.Resistor).WithUnitCost(0m).Generate();

        BomLine line = new BomLine(freeResistor, new[] { "R1", "R2" }.Select(TestParts.Designator));

        Assert.Equal(0m, line.ExtendedCost);
    }

    [Fact]
    public void Needs_at_least_one_designator() {
        Part anyPart = Any.Part().Generate();

        Assert.Throws<ArgumentException>(() => new BomLine(anyPart, Array.Empty<ReferenceDesignator>()));
    }

    [Fact]
    public void Refuses_a_null_designator() {
        Part anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        Assert.Throws<ArgumentException>(() => new BomLine(anyResistor, new ReferenceDesignator?[] { TestParts.Designator("R1"), null }!));
    }

    [Fact]
    public void Refuses_a_repeated_designator() {
        Part anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        Assert.Throws<ArgumentException>(() => new BomLine(anyResistor, new[] { "R1", "R2", "R1" }.Select(TestParts.Designator)));
    }

    [Fact]
    public void Refuses_a_designator_of_another_kind() {
        Part anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        Assert.Throws<ArgumentException>(() => new BomLine(anyResistor, new[] { "R1", "C2" }.Select(TestParts.Designator)));
    }

    [Fact]
    public void Refuses_null_arguments() {
        Part anyPart = Any.Part().Generate();

        Assert.Throws<ArgumentNullException>(() => new BomLine(null!, new[] { TestParts.Designator("R1") }));
        Assert.Throws<ArgumentNullException>(() => new BomLine(anyPart, null!));
    }

    [Fact]
    public void Is_shown_with_its_quantity_part_number_and_designators() {
        Part resistor = Any.Part().WithKind(ComponentKind.Resistor).WithPartNumber("RES-4K7").Generate();

        BomLine line = new BomLine(resistor, new[] { "R3", "R1" }.Select(TestParts.Designator));

        Assert.Equal("2 × RES-4K7: R1, R3", line.ToString());
    }

}
