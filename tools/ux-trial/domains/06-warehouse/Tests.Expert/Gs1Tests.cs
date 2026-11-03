using Warehouse.Domain;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class Gs1Tests {

    // Reference vectors taken from published GS1 keys: the check digit is the point of every test here,
    // so the digits are literals and nothing is drawn.

    [Theory]
    [InlineData("400638133393", 1)]
    [InlineData("590123412345", 7)]
    [InlineData("978020137962", 4)]
    [InlineData("37610425002123456", 9)]
    public void The_check_digit_of_a_published_GS1_key_is_the_one_printed_on_it(string digitsWithoutCheck, int expected) {
        // Act
        int checkDigit = Gs1.ComputeCheckDigit(digitsWithoutCheck);

        // Assert
        Assert.Equal(expected, checkDigit);
    }

    [Theory]
    [InlineData("978020137960")]
    [InlineData("000000000000")]
    public void A_weighted_sum_that_is_a_multiple_of_ten_gives_a_check_digit_of_zero_not_ten(string digitsWithoutCheck) {
        // Act
        int checkDigit = Gs1.ComputeCheckDigit(digitsWithoutCheck);

        // Assert
        Assert.Equal(0, checkDigit);
    }

    [Fact]
    public void Weights_alternate_three_and_one_starting_from_the_rightmost_digit() {
        // Act
        int onlyRightmostDigitCounts    = Gs1.ComputeCheckDigit("000000000001");   // 1 × 3 = 3  -> 7
        int onlySecondFromRightCounts   = Gs1.ComputeCheckDigit("000000000010");   // 1 × 1 = 1  -> 9
        int onlyThirdFromRightCounts    = Gs1.ComputeCheckDigit("000000000100");   // 1 × 3 = 3  -> 7

        // Assert
        Assert.Equal(7, onlyRightmostDigitCounts);
        Assert.Equal(9, onlySecondFromRightCounts);
        Assert.Equal(7, onlyThirdFromRightCounts);
    }

    [Theory]
    [InlineData("4006381333931")]
    [InlineData("9780201379600")]
    [InlineData("376104250021234569")]
    public void A_key_that_ends_with_its_check_digit_is_valid(string key) {
        // Act
        bool valid = Gs1.HasValidCheckDigit(key);

        // Assert
        Assert.True(valid);
    }

    [Theory]
    [InlineData("4006381333930")]
    [InlineData("4006381333932")]
    [InlineData("9780201379601")]
    [InlineData("376104250021234560")]
    public void A_key_that_ends_with_another_digit_is_invalid(string key) {
        // Act
        bool valid = Gs1.HasValidCheckDigit(key);

        // Assert
        Assert.False(valid);
    }

    [Fact]
    public void An_empty_string_is_refused_by_the_slice_rather_than_reported_invalid() {
        // Act
        Exception? thrown = Record.Exception(() => Gs1.HasValidCheckDigit(""));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(thrown);
    }

}
