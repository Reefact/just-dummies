namespace Warehouse.Domain.Tests.Regular;

public sealed class Gs1Tests {

    // Reference values published by GS1: the digit is what the test is about, so each one is a literal.
    [Theory]
    [InlineData("400638133393", 1)]
    [InlineData("978030640615", 7)]
    [InlineData("37610425002123456", 9)]
    [InlineData("1", 7)]
    [InlineData("0", 0)]
    [InlineData("00000000000", 0)]
    public void The_check_digit_follows_the_mod_10_weights_3_and_1_from_the_right(string digitsWithoutCheck, int expected) {
        // Act
        int checkDigit = Gs1.ComputeCheckDigit(digitsWithoutCheck);

        // Assert
        Assert.Equal(expected, checkDigit);
    }

    [Theory]
    [InlineData("4006381333931")]
    [InlineData("9780306406157")]
    [InlineData("376104250021234569")]
    public void A_code_carrying_its_check_digit_is_valid(string code) {
        // Act
        bool valid = Gs1.HasValidCheckDigit(code);

        // Assert
        Assert.True(valid);
    }

    [Theory]
    [InlineData("4006381333930")]
    [InlineData("4006381333932")]
    [InlineData("4006381333939")]
    [InlineData("9780306406158")]
    public void A_code_carrying_another_check_digit_is_invalid(string code) {
        // Act
        bool valid = Gs1.HasValidCheckDigit(code);

        // Assert
        Assert.False(valid);
    }

    [Fact]
    public void A_single_mistyped_digit_is_always_detected() {
        // Arrange: the digit swapped for every other digit in turn, at every position
        const string valid = "4006381333931";

        for (int position = 0; position < valid.Length; position++) {
            for (char replacement = '0'; replacement <= '9'; replacement++) {
                if (replacement == valid[position]) { continue; }

                char[] altered = valid.ToCharArray();
                altered[position] = replacement;

                // Act
                bool stillValid = Gs1.HasValidCheckDigit(new string(altered));

                // Assert
                Assert.False(stillValid, $"{new string(altered)} slipped through");
            }
        }
    }

    [Fact]
    public void A_code_made_of_the_digits_and_the_computed_check_digit_is_valid() {
        // Arrange
        string anyDigits = Any.StringMatching("[0-9]{1,30}").Generate();

        // Act
        bool valid = Gs1.HasValidCheckDigit(anyDigits + Gs1.ComputeCheckDigit(anyDigits));

        // Assert
        Assert.True(valid);
    }

    [Fact]
    public void An_empty_string_has_no_check_digit_to_read() {
        // Act
        Exception? thrown = Record.Exception(() => Gs1.HasValidCheckDigit(""));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(thrown);
    }

}
