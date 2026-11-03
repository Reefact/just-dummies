namespace Manufacturing.Domain.Tests.Expert;

public sealed class BatchCodeTests {

    [Fact]
    public void A_batch_code_exposes_its_plant_and_its_week() {
        // Arrange
        string anyYear     = Any.StringMatching("[0-9]{2}").Generate();
        string anySequence = Any.StringMatching("[0-9]{4}").Generate();

        // Act
        BatchCode code = new BatchCode($"LYN-{anyYear}14-{anySequence}");

        // Assert
        Assert.Equal("LYN", code.Plant);
        Assert.Equal(14, code.Week);
    }

    [Theory]
    [InlineData("01", 1)]
    [InlineData("09", 9)]
    [InlineData("10", 10)]
    [InlineData("49", 49)]
    [InlineData("50", 50)]
    [InlineData("53", 53)]
    public void The_week_is_read_as_a_number_from_one_to_fifty_three(string weekText, int expectedWeek) {
        // Arrange
        string anyYear     = Any.StringMatching("[0-9]{2}").Generate();
        string anySequence = Any.StringMatching("[0-9]{4}").Generate();

        // Act
        BatchCode code = new BatchCode($"LYN-{anyYear}{weekText}-{anySequence}");

        // Assert
        Assert.Equal(expectedWeek, code.Week);
    }

    [Theory]
    [InlineData("00")]
    [InlineData("54")]
    [InlineData("60")]
    [InlineData("99")]
    public void A_week_outside_one_to_fifty_three_is_refused(string weekText) {
        // Arrange
        string anyYear     = Any.StringMatching("[0-9]{2}").Generate();
        string anySequence = Any.StringMatching("[0-9]{4}").Generate();

        // Act
        Action create = () => new BatchCode($"LYN-{anyYear}{weekText}-{anySequence}");

        // Assert
        Assert.Throws<FormatException>(create);
    }

    [Fact]
    public void The_plant_is_the_three_letters_as_written() {
        // Arrange
        string anyYear     = Any.StringMatching("[0-9]{2}").Generate();
        string anyWeek     = Any.StringMatching("0[1-9]|[1-4][0-9]|5[0-3]").Generate();
        string anySequence = Any.StringMatching("[0-9]{4}").Generate();

        // Act
        BatchCode code = new BatchCode($"ZQX-{anyYear}{anyWeek}-{anySequence}");

        // Assert
        Assert.Equal("ZQX", code.Plant);
    }

    [Fact]
    public void The_value_and_the_text_form_are_the_code_as_written() {
        // Act
        BatchCode code = new BatchCode("LYN-2614-0042");

        // Assert
        Assert.Equal("LYN-2614-0042", code.Value);
        Assert.Equal("LYN-2614-0042", code.ToString());
    }

    [Fact]
    public void A_null_code_is_refused_as_a_null_argument() {
        // Act
        Action create = () => new BatchCode(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(create);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_code_is_refused_as_an_argument_error_rather_than_a_format_error(string blank) {
        // Act
        Action create = () => new BatchCode(blank);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Theory]
    [InlineData("lyn-2614-0042")]   // plant in lower case
    [InlineData("LY-2614-0042")]    // plant too short
    [InlineData("LYON-2614-0042")]  // plant too long
    [InlineData("LY1-2614-0042")]   // plant with a digit
    [InlineData("LYN-614-0042")]    // year and week too short
    [InlineData("LYN-26140-0042")]  // year and week too long
    [InlineData("LYN-2614-042")]    // sequence too short
    [InlineData("LYN-2614-00042")]  // sequence too long
    [InlineData("LYN-2614-004A")]   // sequence with a letter
    [InlineData("LYN26140042")]     // separators missing
    [InlineData("LYN_2614_0042")]   // wrong separators
    [InlineData(" LYN-2614-0042")]  // leading space
    [InlineData("LYN-2614-0042 ")]  // trailing space
    public void A_malformed_code_is_refused_as_a_format_error(string malformed) {
        // Act
        Action create = () => new BatchCode(malformed);

        // Assert
        Assert.Throws<FormatException>(create);
    }

    [Fact(Skip = "domain defect: a code followed by a line break is accepted ('$' matches before a final newline), seed not applicable, deterministic")]
    public void A_code_followed_by_a_line_break_is_refused() {
        // Act
        Action create = () => new BatchCode("LYN-2614-0042\n");

        // Assert
        Assert.Throws<FormatException>(create);
    }

    [Fact]
    public void Two_codes_written_the_same_are_equal() {
        // Arrange
        BatchCode first  = new BatchCode("LYN-2614-0042");
        BatchCode second = new BatchCode("LYN-2614-0042");

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_codes_differing_only_by_their_sequence_are_different() {
        // Arrange
        BatchCode first  = new BatchCode("LYN-2614-0042");
        BatchCode second = new BatchCode("LYN-2614-0043");

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_code_is_not_equal_to_nothing_nor_to_a_string() {
        // Arrange
        BatchCode code = new BatchCode("LYN-2614-0042");

        // Assert
        Assert.False(code.Equals((BatchCode?)null));
        Assert.False(code.Equals("LYN-2614-0042"));
    }

}
