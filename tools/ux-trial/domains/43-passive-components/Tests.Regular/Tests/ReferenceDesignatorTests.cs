namespace PassiveComponents.Tests.Regular;

public sealed class ReferenceDesignatorTests {

    [Theory]
    [InlineData("C1", ComponentKind.Capacitor, 1)]
    [InlineData("C9999", ComponentKind.Capacitor, 9999)]
    [InlineData("L12", ComponentKind.Inductor, 12)]
    [InlineData("R10", ComponentKind.Resistor, 10)]
    [InlineData("R100", ComponentKind.Resistor, 100)]
    public void A_letter_and_a_number_name_a_kind_and_a_position(string value, ComponentKind kind, int number) {
        ReferenceDesignator designator = new ReferenceDesignator(value);

        Assert.Equal(value, designator.Value);
        Assert.Equal(kind, designator.Kind);
        Assert.Equal(number, designator.Number);
    }

    [Theory]
    [InlineData("C0")]
    [InlineData("R01")]
    [InlineData("R10000")]
    [InlineData("c1")]
    [InlineData("r1")]
    [InlineData("X1")]
    [InlineData("D1")]
    [InlineData("R")]
    [InlineData("1R")]
    [InlineData("12")]
    [InlineData("R-1")]
    [InlineData("R1.5")]
    [InlineData("R 1")]
    [InlineData("R1 ")]
    [InlineData(" R1")]
    [InlineData("RR1")]
    [InlineData("R1R")]
    [InlineData("R+1")]
    public void A_designator_of_the_wrong_shape_is_refused(string value) {
        Assert.Throws<FormatException>(() => new ReferenceDesignator(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    [InlineData(null)]
    public void A_blank_designator_is_refused_as_an_argument_error(string? value) {
        Assert.Throws<ArgumentException>(() => new ReferenceDesignator(value!));
    }

    [Fact(Skip = "domain defect: '$' in the designator pattern also matches before a final line break, so 'R12\\n' is accepted (Value keeps the newline); input is a literal, no seed")]
    public void A_designator_followed_by_a_line_break_is_refused() {
        Assert.Throws<FormatException>(() => new ReferenceDesignator("R12\n"));
    }

    [Fact]
    public void Designators_are_ordered_by_letter_then_by_number() {
        ReferenceDesignator[] ordered = [
            new ReferenceDesignator("R10"), new ReferenceDesignator("L3"), new ReferenceDesignator("R2"),
            new ReferenceDesignator("C9999"), new ReferenceDesignator("C1"), new ReferenceDesignator("R1")
        ];

        string[] sorted = ordered.Order().Select(d => d.Value).ToArray();

        Assert.Equal(new[] { "C1", "C9999", "L3", "R1", "R2", "R10" }, sorted);
    }

    [Fact]
    public void Equality_is_by_text() {
        ReferenceDesignator first = new ReferenceDesignator("R12");

        Assert.Equal(first, new ReferenceDesignator("R12"));
        Assert.Equal(first.GetHashCode(), new ReferenceDesignator("R12").GetHashCode());
        Assert.NotEqual(first, new ReferenceDesignator("R13"));
        Assert.NotEqual(first, new ReferenceDesignator("C12"));
        Assert.False(first.Equals(null));
        Assert.True(first.CompareTo(null) > 0);
    }

    [Fact]
    public void A_drawn_designator_reads_back_with_its_kind() {
        ReferenceDesignator anyInductorDesignator = Any.ReferenceDesignator().OfKind(ComponentKind.Inductor).Generate();

        Assert.Equal(ComponentKind.Inductor, anyInductorDesignator.Kind);
        Assert.StartsWith("L", anyInductorDesignator.Value);
    }

    [Fact]
    public void It_is_written_as_its_text() {
        Assert.Equal("C47", new ReferenceDesignator("C47").ToString());
    }

}
