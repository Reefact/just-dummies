using JustDummies;

using Manufacturing.Domain;
using Manufacturing.Tests.Discovery.Arbitrary;

using Xunit;

namespace Manufacturing.Tests.Discovery;

public sealed class BatchCodeTests {

    [Fact]
    public void A_code_exposes_its_plant_and_its_week() {
        // Act
        BatchCode code = new BatchCode("LYN-2614-0042");

        // Assert
        Assert.Equal("LYN-2614-0042", code.Value);
        Assert.Equal("LYN", code.Plant);
        Assert.Equal(14, code.Week);
    }

    [Theory]
    [InlineData("LYN-2601-0001", 1)]
    [InlineData("LYN-2609-0001", 9)]
    [InlineData("LYN-2610-0001", 10)]
    [InlineData("LYN-2649-0001", 49)]
    [InlineData("LYN-2650-0001", 50)]
    [InlineData("LYN-2653-0001", 53)]
    public void The_week_is_read_as_a_number_from_01_to_53(string text, int expectedWeek) {
        // Act
        BatchCode code = new BatchCode(text);

        // Assert
        Assert.Equal(expectedWeek, code.Week);
    }

    [Theory]
    [InlineData("LYN-2600-0001")]   // week 00
    [InlineData("LYN-2654-0001")]   // week 54
    [InlineData("LYN-2699-0001")]   // week 99
    [InlineData("lyn-2614-0001")]   // plant in lower case
    [InlineData("LY-2614-0001")]    // plant too short
    [InlineData("LYNX-2614-0001")]  // plant too long
    [InlineData("LY1-2614-0001")]   // plant not made of letters
    [InlineData("LYN2614-0001")]    // missing separator
    [InlineData("LYN-2614_0001")]   // wrong separator
    [InlineData("LYN-261-0001")]    // year and week too short
    [InlineData("LYN-26140-0001")]  // year and week too long
    [InlineData("LYN-2614-001")]    // sequence too short
    [InlineData("LYN-2614-00001")]  // sequence too long
    [InlineData("LYN-2614-ABCD")]   // sequence not numeric
    [InlineData(" LYN-2614-0001")]  // leading space
    [InlineData("LYN-2614-0001 ")]  // trailing space
    public void A_malformed_code_is_refused_with_a_format_error(string text) {
        Assert.Throws<FormatException>(() => new BatchCode(text));
    }

    [Fact(Skip = "domain defect: the regex ends in $, which .NET matches before a final line break, so 'LYN-2614-0042\n' is accepted and stored with its newline, literal input so no seed")]
    public void A_code_ending_with_a_line_break_is_refused() {
        // 'domain defect: the regex ends in `$`, which .NET matches before a final line break' -- see NOTES.discovery.md
        string text = "LYN-2614-0042\n";

        Assert.Throws<FormatException>(() => new BatchCode(text));
    }

    [Fact]
    public void A_missing_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new BatchCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_code_is_refused_as_a_blank_argument_rather_than_a_malformed_one(string text) {
        Assert.Throws<ArgumentException>(() => new BatchCode(text));
    }

    [Fact]
    public void Two_codes_made_from_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.BatchCode().Generate().Value;

        // Act
        BatchCode first  = new BatchCode(anyText);
        BatchCode second = new BatchCode(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_codes_made_from_different_texts_are_different() {
        // Arrange
        string    anyText      = Any.BatchCode().Generate().Value;
        BatchCode anyCode      = new BatchCode(anyText);
        BatchCode anyOtherCode = new BatchCode(Any.StringMatching(@"[A-Z]{3}-\d{2}(0[1-9]|[1-4]\d|5[0-3])-\d{4}").DifferentFrom(anyText).Generate());

        // Assert
        Assert.NotEqual(anyCode, anyOtherCode);
    }

    [Fact]
    public void A_code_is_not_equal_to_nothing_or_to_a_plain_string() {
        // Arrange
        BatchCode anyCode = Any.BatchCode().Generate();

        // Assert
        Assert.False(anyCode.Equals(null));
        Assert.False(anyCode.Equals((object)anyCode.Value));
    }

    [Fact]
    public void A_code_prints_as_its_text() {
        // Arrange
        BatchCode anyCode = Any.BatchCode().Generate();

        // Act
        string text = anyCode.ToString();

        // Assert
        Assert.Equal(anyCode.Value, text);
    }

}
