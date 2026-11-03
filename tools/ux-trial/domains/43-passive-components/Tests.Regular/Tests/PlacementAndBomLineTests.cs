namespace PassiveComponents.Tests.Regular;

public sealed class PlacementTests {

    [Fact]
    public void A_placed_part_is_both_designed_and_fitted() {
        Part part = Any.Part().Resistor().Generate();

        Placement placement = new Placement(Specs.Designator("R7"), part);

        Assert.Equal("R7", placement.Designator.Value);
        Assert.Same(part, placement.DesignedPart);
        Assert.Same(part, placement.FittedPart);
        Assert.False(placement.IsSubstituted);
    }

    [Theory]
    [InlineData("C1", ComponentKind.Inductor)]
    [InlineData("C1", ComponentKind.Resistor)]
    [InlineData("L1", ComponentKind.Capacitor)]
    [InlineData("L1", ComponentKind.Resistor)]
    [InlineData("R1", ComponentKind.Capacitor)]
    [InlineData("R1", ComponentKind.Inductor)]
    public void The_letter_of_the_designator_must_name_the_kind_of_the_part(string designator, ComponentKind kind) {
        Part anyPart = Any.Part().WithKind(kind).Generate();

        Assert.Throws<ArgumentException>(() => new Placement(Specs.Designator(designator), anyPart));
    }

    [Theory]
    [InlineData("C1", ComponentKind.Capacitor)]
    [InlineData("L1", ComponentKind.Inductor)]
    [InlineData("R1", ComponentKind.Resistor)]
    public void The_letter_of_the_designator_matching_the_kind_is_accepted(string designator, ComponentKind kind) {
        Part anyPart = Any.Part().WithKind(kind).Generate();

        Assert.NotNull(new Placement(Specs.Designator(designator), anyPart));
    }

    [Fact]
    public void A_placement_needs_a_designator_and_a_part() {
        Part anyPart = Any.Part().Generate();
        ReferenceDesignator anyDesignator = Any.ReferenceDesignator().OfKind(anyPart.Kind).Generate();

        Assert.Throws<ArgumentNullException>(() => new Placement(null!, anyPart));
        Assert.Throws<ArgumentNullException>(() => new Placement(anyDesignator, null!));
    }

    [Fact]
    public void A_drawn_placement_is_consistent() {
        Placement anyPlacement = Any.Placement().Generate();

        Assert.Equal(anyPlacement.DesignedPart.Kind, anyPlacement.Designator.Kind);
    }

    [Fact]
    public void It_is_written_with_its_designator_and_part_number() {
        Part part = Any.Part().Resistor().WithPartNumber("RC-10K").Generate();

        Assert.Equal("R3: RC-10K", new Placement(Specs.Designator("R3"), part).ToString());
    }

}

public sealed class BomLineTests {

    [Fact]
    public void A_line_lists_the_designators_of_its_part_in_order() {
        Part part = Any.Part().Resistor().Generate();

        BomLine line = new BomLine(part, [Specs.Designator("R10"), Specs.Designator("R2"), Specs.Designator("R33")]);

        Assert.Equal(new[] { "R2", "R10", "R33" }, line.Designators.Select(d => d.Value));
        Assert.Same(part, line.Part);
    }

    [Fact]
    public void Its_quantity_is_the_number_of_designators() {
        BomLine line = Any.BomLine().WithPart(Any.Part().Capacitor().Generate())
                          .WithDesignators([Specs.Designator("C1"), Specs.Designator("C2"), Specs.Designator("C3"), Specs.Designator("C4")]).Generate();

        Assert.Equal(4, line.Quantity);
    }

    [Fact]
    public void Its_extended_cost_is_the_unit_cost_times_the_quantity() {
        Part part = Any.Part().Resistor().WithUnitCost(0.0123m).Generate();

        BomLine line = new BomLine(part, [Specs.Designator("R1"), Specs.Designator("R2"), Specs.Designator("R3")]);

        Assert.Equal(0.0369m, line.ExtendedCost);
    }

    [Fact]
    public void A_free_part_costs_nothing_however_many_are_fitted() {
        Part part = Any.Part().Resistor().WithUnitCost(0m).Generate();

        BomLine line = new BomLine(part, [Specs.Designator("R1"), Specs.Designator("R2")]);

        Assert.Equal(0m, line.ExtendedCost);
    }

    [Fact]
    public void A_line_lists_at_least_one_designator() {
        Part anyPart = Any.Part().Generate();

        Assert.Throws<ArgumentException>(() => new BomLine(anyPart, []));
    }

    [Fact]
    public void A_line_lists_no_null_designator() {
        Part part = Any.Part().Resistor().Generate();

        Assert.Throws<ArgumentException>(() => new BomLine(part, [Specs.Designator("R1"), null!]));
    }

    [Fact]
    public void A_line_lists_each_designator_once() {
        Part part = Any.Part().Resistor().Generate();

        Assert.Throws<ArgumentException>(() => new BomLine(part, [Specs.Designator("R1"), Specs.Designator("R2"), Specs.Designator("R1")]));
    }

    [Fact]
    public void Every_designator_must_be_of_the_kind_of_the_part() {
        Part part = Any.Part().Resistor().Generate();

        Assert.Throws<ArgumentException>(() => new BomLine(part, [Specs.Designator("R1"), Specs.Designator("C2")]));
    }

    [Fact]
    public void A_line_needs_a_part_and_designators() {
        Part part = Any.Part().Resistor().Generate();

        Assert.Throws<ArgumentNullException>(() => new BomLine(null!, [Specs.Designator("R1")]));
        Assert.Throws<ArgumentNullException>(() => new BomLine(part, null!));
    }

    [Fact]
    public void The_designators_of_a_line_are_a_snapshot() {
        Part part = Any.Part().Resistor().Generate();
        List<ReferenceDesignator> designators = [Specs.Designator("R2"), Specs.Designator("R1")];

        BomLine line = new BomLine(part, designators);
        designators.Add(Specs.Designator("R3"));

        Assert.Equal(2, line.Quantity);
        Assert.Equal("R1", line.Designators[0].Value);
    }

    [Fact]
    public void It_is_written_with_its_quantity_part_number_and_designators() {
        Part part = Any.Part().Resistor().WithPartNumber("RC-10K").Generate();

        BomLine line = new BomLine(part, [Specs.Designator("R2"), Specs.Designator("R1")]);

        Assert.Equal("2 × RC-10K: R1, R2", line.ToString());
    }

}
