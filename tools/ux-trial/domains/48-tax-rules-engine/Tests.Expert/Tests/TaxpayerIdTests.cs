using JustDummies;

using TaxRules.Domain;

using Xunit;

namespace TaxRules.Tests;

public sealed class TaxpayerIdTests {

    // Check digits worked out by hand with the Luhn algorithm, not with the domain's own function.
    [Theory]
    [InlineData("123456782")]
    [InlineData("100000009")]
    [InlineData("999999998")]
    [InlineData("400000014")]
    [InlineData("739273985")]
    public void Nine_digits_whose_last_is_the_luhn_check_of_the_first_eight_are_accepted(string value) {
        Assert.Equal(value, new TaxpayerId(value).Value);
    }

    [Theory]
    [InlineData("123456780")]
    [InlineData("123456781")]
    [InlineData("123456783")]
    [InlineData("100000000")]
    [InlineData("739273986")]
    public void A_wrong_check_digit_is_refused_as_an_argument_problem(string value) {
        Assert.Throws<ArgumentException>(() => new TaxpayerId(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678")]
    [InlineData("1234567820")]
    [InlineData("12345678A")]
    [InlineData("12345 782")]
    [InlineData("-23456782")]
    [InlineData("12345678٢")]
    public void Anything_but_nine_ascii_digits_is_refused_as_a_format_problem(string value) {
        Assert.Throws<FormatException>(() => new TaxpayerId(value));
    }

    [Fact]
    public void A_leading_zero_is_refused_as_a_format_problem_even_when_the_check_digit_is_right() {
        // 000000000 and 012345674 pass the Luhn sum; the shape is judged first.
        Assert.Throws<FormatException>(() => new TaxpayerId("000000000"));
        Assert.Throws<FormatException>(() => new TaxpayerId("012345674"));
    }

    [Fact]
    public void A_missing_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TaxpayerId(null!));
    }

    [Theory]
    [InlineData("12345678", 2)]
    [InlineData("10000000", 9)]
    [InlineData("99999999", 8)]
    [InlineData("40000001", 4)]
    [InlineData("73927398", 5)]
    [InlineData("00000000", 0)]
    public void The_check_digit_is_the_luhn_digit_of_the_first_eight(string firstEight, int expected) {
        Assert.Equal(expected, TaxpayerId.CheckDigitFor(firstEight));
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("1234567A")]
    [InlineData("")]
    public void A_check_digit_is_computed_for_eight_digits_only(string notEightDigits) {
        Assert.Throws<ArgumentException>(() => TaxpayerId.CheckDigitFor(notEightDigits));
    }

    [Fact]
    public void A_check_digit_for_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => TaxpayerId.CheckDigitFor(null!));
    }

    [Fact]
    public void Two_numbers_of_the_same_digits_are_equal_and_a_number_is_shown_as_its_digits() {
        Assert.Equal(new TaxpayerId("123456782"), new TaxpayerId("123456782"));
        Assert.Equal(new TaxpayerId("123456782").GetHashCode(), new TaxpayerId("123456782").GetHashCode());
        Assert.NotEqual(new TaxpayerId("123456782"), new TaxpayerId("100000009"));
        Assert.False(new TaxpayerId("123456782").Equals((object)"123456782"));
        Assert.Equal("123456782", new TaxpayerId("123456782").ToString());
    }

}
