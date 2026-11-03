using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class Gs1Tests {

    [Theory]
    [InlineData("400638133393", 1)]
    [InlineData("629104150021", 3)]
    [InlineData("501234567890", 0)]
    [InlineData("34012345000000001", 7)]
    [InlineData("12345678901234567", 5)]
    public void The_check_digit_follows_the_gs1_mod_10_rule(string digitsWithoutCheck, int expected) {
        // Act
        int checkDigit = Gs1.ComputeCheckDigit(digitsWithoutCheck);

        // Assert
        Assert.Equal(expected, checkDigit);
    }

    [Theory]
    [InlineData("4006381333931")]
    [InlineData("6291041500213")]
    [InlineData("5012345678900")]
    [InlineData("340123450000000017")]
    public void A_code_ending_with_the_right_check_digit_is_valid(string digits) {
        Assert.True(Gs1.HasValidCheckDigit(digits));
    }

    [Theory]
    [InlineData("4006381333932")]
    [InlineData("6291041500210")]
    [InlineData("340123450000000010")]
    public void A_code_ending_with_a_wrong_check_digit_is_invalid(string digits) {
        Assert.False(Gs1.HasValidCheckDigit(digits));
    }

    [Fact]
    public void An_empty_string_has_no_check_digit_to_read() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Gs1.HasValidCheckDigit(string.Empty));
    }

    [Fact]
    public void A_single_digit_is_its_own_check_digit_only_when_it_is_zero() {
        // The body is empty: the sum is zero and so is the check digit.
        Assert.True(Gs1.HasValidCheckDigit("0"));
        Assert.False(Gs1.HasValidCheckDigit("5"));
    }

    [Fact]
    public void Characters_are_read_as_c_minus_zero_without_validation() {
        // 'A' - '0' = 17: weight 3 -> 51 -> (10 - 1) % 10 = 9
        Assert.Equal(9, Gs1.ComputeCheckDigit("A"));
    }

}
