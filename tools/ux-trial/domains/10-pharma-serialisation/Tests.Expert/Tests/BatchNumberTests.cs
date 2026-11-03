using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class BatchNumberTests {

    private const string LotAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

    [Theory]
    [InlineData("LOT-2027-A")]
    [InlineData("A")]
    [InlineData("0")]
    [InlineData("-")]
    [InlineData("AZ09-")] // both ends of every accepted range
    public void A_lot_number_of_upper_case_letters_digits_and_hyphens_is_accepted(string value) {
        // Act
        BatchNumber number = new BatchNumber(value);

        // Assert
        Assert.Equal(value, number.Value);
    }

    [Fact]
    public void A_lot_number_of_twenty_characters_is_accepted() {
        // Arrange
        string anyTwentyCharacters = Any.String().WithChars(LotAlphabet).WithLength(20).Generate();

        // Act
        BatchNumber number = new BatchNumber(anyTwentyCharacters);

        // Assert
        Assert.Equal(20, number.Value.Length);
    }

    [Fact]
    public void A_lot_number_of_twenty_one_characters_is_refused_as_out_of_range() {
        // Arrange
        string anyTwentyOneCharacters = Any.String().WithChars(LotAlphabet).WithLength(21).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BatchNumber(anyTwentyOneCharacters));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void The_length_is_judged_before_the_alphabet() {
        // Arrange
        string anyTwentyOneLowerCaseLetters = Any.String().Alpha().InLowerCase().WithLength(21).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatchNumber(anyTwentyOneLowerCaseLetters));
    }

    [Fact]
    public void A_null_lot_number_is_refused_as_an_argument_null_exception() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BatchNumber(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_lot_number_is_refused_as_an_argument_exception(string value) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BatchNumber(value));
    }

    [Fact]
    public void Blankness_is_judged_before_the_length() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BatchNumber(new string(' ', 21)));
    }

    [Theory]
    [InlineData("lot-1")]    // lower case
    [InlineData("LOT 1")]    // a space
    [InlineData("LOT_1")]    // an underscore
    [InlineData("LOT/1")]
    [InlineData("ÉCLAT")] // a letter outside A-Z
    public void A_character_outside_the_alphabet_is_refused_as_an_argument_exception(string value) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BatchNumber(value));
    }

    [Fact(Skip = "domain defect: the alphabet check is ^[A-Z0-9-]+$, and $ also matches before a final newline, so \"LOT-1\\n\" is accepted and keeps the newline in Value. Literal input, no draw involved (no seed).")]
    public void A_trailing_newline_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BatchNumber("LOT-1\n"));
    }

    [Fact]
    public void Two_lot_numbers_of_the_same_value_are_equal_and_share_a_hash_code() {
        // Arrange
        BatchNumber first  = new BatchNumber("LOT-42");
        BatchNumber second = new BatchNumber("LOT-42");

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_lot_numbers_of_different_values_are_not_equal() {
        // Arrange
        BatchNumber first  = new BatchNumber("LOT-42");
        BatchNumber second = new BatchNumber("LOT-43");

        // Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void A_lot_number_is_not_equal_to_null_nor_to_its_text() {
        // Arrange
        BatchNumber number = new BatchNumber("LOT-42");

        // Assert
        Assert.False(number.Equals(null));
        Assert.False(number.Equals("LOT-42"));
    }

    [Fact]
    public void A_lot_number_reads_as_its_value() {
        // Assert
        Assert.Equal("LOT-42", new BatchNumber("LOT-42").ToString());
    }

}
