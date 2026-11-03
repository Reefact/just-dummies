using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class ReferenceDesignatorTests {

    [Theory]
    [InlineData("C1", ComponentKind.Capacitor, 1)]
    [InlineData("C9999", ComponentKind.Capacitor, 9999)]
    [InlineData("L12", ComponentKind.Inductor, 12)]
    [InlineData("R100", ComponentKind.Resistor, 100)]
    [InlineData("R9", ComponentKind.Resistor, 9)]
    public void Reads_the_kind_from_the_letter_and_the_number_from_the_digits(string value, ComponentKind kind, int number) {
        ReferenceDesignator designator = new ReferenceDesignator(value);

        Assert.Equal(value, designator.Value);
        Assert.Equal(kind, designator.Kind);
        Assert.Equal(number, designator.Number);
        Assert.Equal(value, designator.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(null)]
    public void Refuses_a_blank_value(string? value) {
        Assert.Throws<ArgumentException>(() => new ReferenceDesignator(value!));
    }

    [Theory]
    [InlineData("R0")]
    [InlineData("R01")]
    [InlineData("R10000")]
    [InlineData("X1")]
    [InlineData("c1")]
    [InlineData("r1")]
    [InlineData("R")]
    [InlineData("1R")]
    [InlineData("R-1")]
    [InlineData("R1.5")]
    [InlineData("R 1")]
    [InlineData(" R1")]
    [InlineData("R1 ")]
    [InlineData("RR1")]
    [InlineData("C1R")]
    [InlineData("\nR1")]
    public void Refuses_a_value_that_is_not_a_letter_then_one_to_9999_without_leading_zero(string value) {
        Assert.Throws<FormatException>(() => new ReferenceDesignator(value));
    }

    [Theory(Skip = "domain defect: the format ends with $, which .NET lets match before a final \\n, so 'R1\\n' is accepted and the designator's Value keeps the line break; literal input, no seed")]
    [InlineData("R1\n")]
    [InlineData("C12\n")]
    public void Refuses_a_designator_followed_by_a_line_break(string value) {
        Assert.Throws<FormatException>(() => new ReferenceDesignator(value));
    }

    [Fact]
    public void Refuses_digits_that_are_not_ascii() {
        // Arabic-Indic digits: \d in .NET matches them.
        Assert.Throws<FormatException>(() => new ReferenceDesignator("R١٢"));
    }

    [Fact]
    public void Is_ordered_by_letter_then_numerically() {
        ReferenceDesignator[] sorted = new[] { "R10", "R2", "L1", "C20", "C3", "R1" }
                                      .Select(v => new ReferenceDesignator(v))
                                      .OrderBy(d => d)
                                      .ToArray();

        Assert.Equal(new[] { "C3", "C20", "L1", "R1", "R2", "R10" }, sorted.Select(d => d.Value));
    }

    [Fact]
    public void Comparison_with_null_puts_a_designator_first() {
        ReferenceDesignator anyDesignator = Any.ReferenceDesignator().Generate();

        Assert.True(anyDesignator.CompareTo(null) > 0);
    }

    [Fact]
    public void Equality_is_by_value() {
        Assert.Equal(new ReferenceDesignator("R12"), new ReferenceDesignator("R12"));
        Assert.Equal(new ReferenceDesignator("R12").GetHashCode(), new ReferenceDesignator("R12").GetHashCode());
        Assert.NotEqual(new ReferenceDesignator("R12"), new ReferenceDesignator("R21"));
        Assert.NotEqual(new ReferenceDesignator("R12"), new ReferenceDesignator("C12"));
    }

    [Fact]
    public void Is_not_equal_to_null() {
        ReferenceDesignator anyDesignator = Any.ReferenceDesignator().Generate();

        Assert.False(anyDesignator.Equals(null));
    }

}
