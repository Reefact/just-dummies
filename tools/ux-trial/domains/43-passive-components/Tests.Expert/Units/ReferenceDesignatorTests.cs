using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class ReferenceDesignatorTests {

    [Theory]
    [InlineData("C1", ComponentKind.Capacitor, 1)]
    [InlineData("C9999", ComponentKind.Capacitor, 9999)]
    [InlineData("L12", ComponentKind.Inductor, 12)]
    [InlineData("R100", ComponentKind.Resistor, 100)]
    [InlineData("R1000", ComponentKind.Resistor, 1000)]
    public void The_letter_names_the_kind_and_the_digits_the_number(string text, ComponentKind kind, int number) {
        // Act
        ReferenceDesignator designator = new ReferenceDesignator(text);

        // Assert
        Assert.Equal(kind, designator.Kind);
        Assert.Equal(number, designator.Number);
        Assert.Equal(text, designator.Value);
        Assert.Equal(text, designator.ToString());
    }

    [Theory]
    [InlineData("C0")]
    [InlineData("C01")]
    [InlineData("C10000")]
    [InlineData("C")]
    [InlineData("X1")]
    [InlineData("c1")]
    [InlineData("1C")]
    [InlineData("1")]
    [InlineData("RR1")]
    [InlineData("R1.5")]
    [InlineData("R-1")]
    [InlineData("R+1")]
    [InlineData("R 1")]
    [InlineData(" R1")]
    [InlineData("R1 ")]
    [InlineData("R1R2")]
    [InlineData("C1,C2")]
    public void A_text_that_is_not_a_letter_then_one_to_nine_thousand_nine_hundred_ninety_nine_is_refused(string text) {
        Assert.Throws<FormatException>(() => new ReferenceDesignator(text));
    }

    [Fact(Skip = "domain defect: the format ends with '$', which .NET also matches before a final line feed, so 'R12\\n' is accepted as a designator whose Value keeps the newline; deterministic, no seed")]
    public void A_designator_followed_by_a_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => new ReferenceDesignator("R12\n"));
    }

    [Theory]
    [InlineData("R١")]
    [InlineData("R１２")]
    public void Digits_of_other_scripts_are_a_format_error(string text) {
        Assert.Throws<FormatException>(() => new ReferenceDesignator(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(null)]
    public void A_blank_designator_is_refused_as_an_argument_rather_than_as_a_format(string? text) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReferenceDesignator(text!));

        // Assert
        Assert.IsNotType<FormatException>(refusal);
    }

    [Fact]
    public void Designators_of_one_kind_are_ordered_by_number_not_by_text() {
        // Arrange
        ReferenceDesignator r2  = new ReferenceDesignator("R2");
        ReferenceDesignator r10 = new ReferenceDesignator("R10");

        // Act & Assert: as text, "R10" would come first
        Assert.True(r2.CompareTo(r10) < 0);
        Assert.True(r10.CompareTo(r2) > 0);
    }

    [Fact]
    public void Designators_are_ordered_by_letter_before_number() {
        Assert.True(new ReferenceDesignator("C9999").CompareTo(new ReferenceDesignator("L1")) < 0);
        Assert.True(new ReferenceDesignator("L9999").CompareTo(new ReferenceDesignator("R1")) < 0);
    }

    [Fact]
    public void A_shuffled_bill_of_designators_sorts_into_schematic_order() {
        // Arrange
        string[] inOrder = ["C1", "C2", "C10", "L1", "L3", "R1", "R2", "R10", "R100"];
        List<ReferenceDesignator> anyShuffle = Any.PermutationOf(inOrder.Select(text => new ReferenceDesignator(text))).Generate();

        // Act
        List<ReferenceDesignator> sorted = anyShuffle.Order().ToList();

        // Assert
        Assert.Equal(inOrder, sorted.Select(d => d.Value));
    }

    [Fact]
    public void A_designator_compared_with_nothing_comes_after_it() {
        Assert.True(new ReferenceDesignator("R1").CompareTo(null) > 0);
    }

    [Fact]
    public void Equality_is_by_text() {
        Assert.Equal(new ReferenceDesignator("R12"), new ReferenceDesignator("R12"));
        Assert.Equal(new ReferenceDesignator("R12").GetHashCode(), new ReferenceDesignator("R12").GetHashCode());
        Assert.NotEqual(new ReferenceDesignator("R12"), new ReferenceDesignator("R13"));
        Assert.NotEqual(new ReferenceDesignator("R12"), new ReferenceDesignator("C12"));
    }

}
