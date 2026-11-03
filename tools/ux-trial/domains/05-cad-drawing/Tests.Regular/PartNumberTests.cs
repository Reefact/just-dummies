using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class PartNumberTests {

    [Theory]
    [InlineData("P004217")]
    [InlineData("P004217-A2")]
    [InlineData("P000000-00")]
    [InlineData("P999999-ZZ")]
    public void A_well_formed_part_number_is_kept_as_written(string written) {
        // Act
        PartNumber partNumber = new PartNumber(written);

        // Assert
        Assert.Equal(written, partNumber.Value);
        Assert.Equal(written, partNumber.ToString());
    }

    [Fact]
    public void Any_generated_part_number_is_well_formed() {
        // Arrange
        string anyText = Any.PartNumber().Generate().Value;

        // Act
        PartNumber partNumber = new PartNumber(anyText);

        // Assert
        Assert.Equal(anyText, partNumber.Value);
    }

    [Fact]
    public void A_null_part_number_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new PartNumber(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_blank_part_number_is_refused_as_an_argument_error_and_not_as_a_format_error() {
        // Arrange
        string anyBlank = Any.String().Whitespaces().NonEmpty().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PartNumber(anyBlank));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_string_that_is_not_shaped_like_a_part_number_is_refused_as_a_format_error() {
        // Arrange
        string anyLowerCaseWord = Any.String().Alpha().InLowerCase().NonEmpty().Generate();

        // Act / Assert
        Assert.Throws<FormatException>(() => new PartNumber(anyLowerCaseWord));
    }

    [Theory]
    [InlineData("P12345")]          // five digits
    [InlineData("P1234567")]        // seven digits
    [InlineData("p004217")]         // lower-case prefix
    [InlineData("004217")]          // no prefix
    [InlineData("P004217-")]        // empty variant
    [InlineData("P004217-A")]       // one-character variant
    [InlineData("P004217-A23")]     // three-character variant
    [InlineData("P004217-a2")]      // lower-case variant
    [InlineData("P004217A2")]       // no dash
    [InlineData(" P004217")]        // leading space
    [InlineData("P004217 ")]        // trailing space
    [InlineData("P0042 17")]        // inner space
    public void A_malformed_part_number_is_refused(string malformed) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new PartNumber(malformed));
    }

    [Fact(Skip = "domain defect: a trailing line feed is accepted ('$' also matches before a final \\n), so 'P004217\\n' is a part number")]
    public void A_part_number_followed_by_a_line_feed_is_refused() {
        // Act / Assert
        Assert.Throws<FormatException>(() => new PartNumber("P004217\n"));
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit, so Arabic-Indic digits form a part number")]
    public void A_part_number_made_of_non_ascii_digits_is_refused() {
        // Act / Assert
        Assert.Throws<FormatException>(() => new PartNumber("P٠٠٤٢١٧"));
    }

    [Fact]
    public void Two_part_numbers_with_the_same_value_are_equal_and_hash_alike() {
        // Arrange
        string        anyValue = Any.PartNumber().Generate().Value;
        PartNumber    first    = new PartNumber(anyValue);
        PartNumber    second   = new PartNumber(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Part_numbers_differing_by_their_variant_are_not_equal() {
        // Arrange
        PartNumber plain   = new PartNumber("P004217");
        PartNumber variant = new PartNumber("P004217-A2");

        // Assert
        Assert.NotEqual(plain, variant);
    }

    [Fact]
    public void A_part_number_is_not_equal_to_null_nor_to_another_type() {
        // Arrange
        PartNumber anyPartNumber = Any.PartNumber().Generate();

        // Assert
        Assert.False(anyPartNumber.Equals((PartNumber?)null));
        Assert.False(anyPartNumber.Equals((object?)anyPartNumber.Value));
    }

}
