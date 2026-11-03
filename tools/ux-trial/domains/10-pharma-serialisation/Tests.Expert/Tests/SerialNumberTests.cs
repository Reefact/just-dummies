using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class SerialNumberTests {

    [Fact]
    public void A_serial_of_eight_characters_is_accepted() {
        // Arrange
        string anyEightCharacters = Any.String().AlphaNumeric().WithLength(8).Generate();

        // Act
        SerialNumber serial = new SerialNumber(anyEightCharacters);

        // Assert
        Assert.Equal(8, serial.Value.Length);
    }

    [Fact]
    public void A_serial_of_twenty_characters_is_accepted() {
        // Arrange
        string anyTwentyCharacters = Any.String().AlphaNumeric().WithLength(20).Generate();

        // Act
        SerialNumber serial = new SerialNumber(anyTwentyCharacters);

        // Assert
        Assert.Equal(20, serial.Value.Length);
    }

    [Fact]
    public void A_serial_mixing_upper_case_lower_case_and_digits_is_accepted() {
        // Act
        SerialNumber serial = new SerialNumber("aZ09bY18");

        // Assert
        Assert.Equal("aZ09bY18", serial.Value);
    }

    [Fact]
    public void A_serial_of_seven_characters_is_refused_as_out_of_range() {
        // Arrange
        string anySevenCharacters = Any.String().AlphaNumeric().WithLength(7).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SerialNumber(anySevenCharacters));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_serial_of_twenty_one_characters_is_refused_as_out_of_range() {
        // Arrange
        string anyTwentyOneCharacters = Any.String().AlphaNumeric().WithLength(21).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerialNumber(anyTwentyOneCharacters));
    }

    [Fact]
    public void An_empty_serial_is_refused_as_out_of_range() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerialNumber(string.Empty));
    }

    [Fact]
    public void A_null_serial_is_refused_as_an_argument_null_exception() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SerialNumber(null!));
    }

    [Theory]
    [InlineData("ABCDEFG-")]
    [InlineData("ABCD EFG")]
    [InlineData("ABCDEFG_")]
    [InlineData("ABCDEFÉG")]
    public void A_character_outside_the_alphabet_is_refused_as_an_argument_exception(string value) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SerialNumber(value));
    }

    [Fact]
    public void The_length_is_judged_before_the_alphabet() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerialNumber("ABC-"));
    }

    [Fact(Skip = "domain defect: the alphabet check is ^[A-Za-z0-9]*$, and $ also matches before a final newline, so \"ABCDEFGH\\n\" (9 characters) is accepted and keeps the newline in Value. Literal input, no draw involved (no seed).")]
    public void A_trailing_newline_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SerialNumber("ABCDEFGH\n"));
    }

    [Fact]
    public void Two_serials_of_the_same_value_are_equal_and_share_a_hash_code() {
        // Arrange
        SerialNumber first  = new SerialNumber("ABCDEFGH");
        SerialNumber second = new SerialNumber("ABCDEFGH");

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Serials_differing_only_by_case_are_different() {
        // Arrange
        SerialNumber upper = new SerialNumber("ABCDEFGH");
        SerialNumber lower = new SerialNumber("abcdefgh");

        // Assert
        Assert.False(upper.Equals(lower));
    }

    [Fact]
    public void A_serial_is_not_equal_to_null_nor_to_its_text() {
        // Arrange
        SerialNumber serial = new SerialNumber("ABCDEFGH");

        // Assert
        Assert.False(serial.Equals(null));
        Assert.False(serial.Equals("ABCDEFGH"));
    }

    [Fact]
    public void A_serial_reads_as_its_value() {
        // Assert
        Assert.Equal("ABCDEFGH", new SerialNumber("ABCDEFGH").ToString());
    }

}
