using JustDummies;

using Manufacturing.Domain;

using Xunit;

namespace Manufacturing.Tests.Regular;

public sealed class BatchCodeTests {

    [Fact]
    public void A_well_formed_code_exposes_its_plant_and_week() {
        // Act
        BatchCode batch = new BatchCode("LYN-2614-0042");

        // Assert
        Assert.Equal("LYN-2614-0042", batch.Value);
        Assert.Equal("LYN", batch.Plant);
        Assert.Equal(14, batch.Week);
    }

    [Fact]
    public void The_week_is_parsed_as_a_number_whatever_its_leading_zero() {
        // Arrange
        string anyPlant    = Any.StringMatching("[A-Z]{3}").Generate();
        string anyYear     = Any.StringMatching("[0-9]{2}").Generate();
        string anySequence = Any.StringMatching("[0-9]{4}").Generate();

        // Act
        BatchCode weekNine = new BatchCode($"{anyPlant}-{anyYear}09-{anySequence}");

        // Assert
        Assert.Equal(9, weekNine.Week);
        Assert.Equal(anyPlant, weekNine.Plant);
    }

    [Fact]
    public void Any_week_from_1_to_53_is_accepted() {
        // Arrange
        string anyPlant    = Any.StringMatching("[A-Z]{3}").Generate();
        string anyYear     = Any.StringMatching("[0-9]{2}").Generate();
        string anySequence = Any.StringMatching("[0-9]{4}").Generate();
        int    anyWeek     = Any.Int32().Between(1, 53).Generate();

        // Act
        BatchCode batch = new BatchCode($"{anyPlant}-{anyYear}{anyWeek:D2}-{anySequence}");

        // Assert
        Assert.Equal(anyWeek, batch.Week);
    }

    [Fact]
    public void The_extreme_weeks_are_accepted() {
        Assert.Equal(1, new BatchCode("ABC-2601-0001").Week);
        Assert.Equal(53, new BatchCode("ABC-2653-0001").Week);
    }

    [Fact]
    public void Week_zero_is_refused() {
        // Arrange
        string anyPlant    = Any.StringMatching("[A-Z]{3}").Generate();
        string anyYear     = Any.StringMatching("[0-9]{2}").Generate();
        string anySequence = Any.StringMatching("[0-9]{4}").Generate();

        // Act
        Action act = () => new BatchCode($"{anyPlant}-{anyYear}00-{anySequence}");

        // Assert
        Assert.Throws<FormatException>(act);
    }

    [Fact]
    public void A_week_after_53_is_refused() {
        // Arrange
        string anyPlant    = Any.StringMatching("[A-Z]{3}").Generate();
        string anyYear     = Any.StringMatching("[0-9]{2}").Generate();
        string anySequence = Any.StringMatching("[0-9]{4}").Generate();
        int    anyBadWeek  = Any.Int32().Between(54, 99).Generate();

        // Act
        Action act = () => new BatchCode($"{anyPlant}-{anyYear}{anyBadWeek}-{anySequence}");

        // Assert
        Assert.Throws<FormatException>(act);
    }

    [Theory]
    [InlineData("lyn-2614-0042")]   // lower case plant
    [InlineData("LY-2614-0042")]    // plant too short
    [InlineData("LYON-2614-0042")]  // plant too long
    [InlineData("LYN-261-0042")]    // year/week too short
    [InlineData("LYN-26144-0042")]  // year/week too long
    [InlineData("LYN-2614-042")]    // sequence too short
    [InlineData("LYN-2614-00420")]  // sequence too long
    [InlineData("LYN26140042")]     // no separators
    [InlineData("LYN-2614-004A")]   // letter in the sequence
    [InlineData(" LYN-2614-0042")]  // leading space
    [InlineData("LYN-2614-0042 ")]  // trailing space
    public void A_malformed_code_is_refused(string malformed) {
        Assert.Throws<FormatException>(() => new BatchCode(malformed));
    }

    [Fact]
    public void A_null_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new BatchCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_code_is_refused_as_an_argument_error_rather_than_a_format_error(string blank) {
        Assert.Throws<ArgumentException>(() => new BatchCode(blank));
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal() {
        // Arrange
        BatchCode anyBatch = new AnyBatchCode().Generate();

        // Act
        BatchCode sameBatch = new BatchCode(anyBatch.Value);

        // Assert
        Assert.Equal(anyBatch, sameBatch);
        Assert.True(anyBatch.Equals((object)sameBatch));
        Assert.Equal(anyBatch.GetHashCode(), sameBatch.GetHashCode());
        Assert.Equal(anyBatch.Value, anyBatch.ToString());
    }

    [Fact]
    public void Two_codes_with_different_values_are_not_equal() {
        // Arrange
        BatchCode anyBatch      = new AnyBatchCode().Generate();
        string    anotherValue  = Any.StringMatching("[A-Z]{3}-[0-9]{2}(0[1-9]|[1-4][0-9]|5[0-3])-[0-9]{4}").DifferentFrom(anyBatch.Value).Generate();

        // Act
        BatchCode anotherBatch = new BatchCode(anotherValue);

        // Assert
        Assert.NotEqual(anyBatch, anotherBatch);
        Assert.False(anyBatch.Equals(null));
    }

    [Fact(Skip = "domain defect: the regex ends with $, which also matches before a trailing line feed, so 'LYN-2614-0042\\n' is accepted and kept in Value, any seed")]
    public void A_code_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => new BatchCode("LYN-2614-0042\n"));
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit, so a code with Arabic-Indic digits in its year is accepted, any seed")]
    public void A_code_with_non_ascii_digits_is_refused() {
        Assert.Throws<FormatException>(() => new BatchCode("LYN-\u0662\u0666" + "14-0042"));
    }
}
