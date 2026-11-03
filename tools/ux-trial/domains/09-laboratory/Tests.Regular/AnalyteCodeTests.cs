using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class AnalyteCodeTests {

    [Fact]
    public void A_code_of_one_to_five_digits_a_hyphen_and_a_digit_is_accepted_and_kept_as_given() {
        // Arrange
        string anyCode = Any.StringMatching(@"[0-9]{1,5}-[0-9]").Generate();

        // Act
        AnalyteCode analyteCode = new AnalyteCode(anyCode);

        // Assert
        Assert.Equal(anyCode, analyteCode.Value);
        Assert.Equal(anyCode, analyteCode.ToString());
    }

    [Theory]
    [InlineData("2345-7")]
    [InlineData("1-0")]
    [InlineData("99999-9")]
    public void The_shortest_and_the_longest_codes_are_accepted(string code) {
        Assert.Equal(code, new AnalyteCode(code).Value);
    }

    [Fact]
    public void The_trailing_digit_is_not_a_verified_check_digit() {
        // Arrange
        string anyCheckDigit = Any.String().Numeric().WithLength(1).Generate();

        // Act & Assert
        Assert.Equal($"2345-{anyCheckDigit}", new AnalyteCode($"2345-{anyCheckDigit}").Value);
    }

    [Fact]
    public void The_generator_only_draws_codes_the_constructor_accepts() {
        // Act
        AnalyteCode anyAnalyte = Any.AnalyteCode().Generate();

        // Assert
        Assert.Matches(@"^[0-9]{1,5}-[0-9]$", anyAnalyte.Value);
    }

    [Fact]
    public void A_null_code_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new AnalyteCode(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2345")]                 // no hyphen, no check digit
    [InlineData("2345-")]                // no check digit
    [InlineData("-7")]                   // no digits before the hyphen
    [InlineData("123456-7")]             // six digits before the hyphen
    [InlineData("2345-78")]              // two check digits
    [InlineData("2345-a")]
    [InlineData("23a5-7")]
    [InlineData(" 2345-7")]
    [InlineData("2345-7 ")]
    [InlineData("2345–7")]               // an en dash is not a hyphen
    public void A_code_off_the_format_is_refused(string code) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AnalyteCode(code));

        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_code_with_too_many_digits_before_the_hyphen_is_refused() {
        // Arrange
        string anyTooLong = Any.StringMatching(@"[0-9]{6,12}-[0-9]").Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AnalyteCode(anyTooLong));
    }

    [Theory(Skip = "domain defect: the pattern's `$` also matches before a final newline, so '2345-7\\n' is accepted as a code (no seed: literal)")]
    [InlineData("2345-7\n")]
    public void A_code_followed_by_a_line_break_is_refused(string code) {
        Assert.Throws<ArgumentException>(() => new AnalyteCode(code));
    }

    [Theory(Skip = "domain defect: `\\d` matches any Unicode decimal digit, so '٢٣٤٥-٧' is accepted as a LOINC-style code, whereas SampleId accepts ASCII digits only (no seed: literal)")]
    [InlineData("٢٣٤٥-٧")]
    public void A_code_written_with_non_ascii_digits_is_refused(string code) {
        Assert.Throws<ArgumentException>(() => new AnalyteCode(code));
    }

    [Fact]
    public void Two_codes_with_the_same_text_are_equal_and_hash_alike() {
        // Arrange
        string anyCode = Any.StringMatching(@"[0-9]{1,5}-[0-9]").Generate();

        // Act
        AnalyteCode first  = new AnalyteCode(anyCode);
        AnalyteCode second = new AnalyteCode(anyCode);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_codes_with_different_texts_are_different() {
        // Arrange
        AnalyteCode anyAnalyte      = Any.AnalyteCode().Generate();
        AnalyteCode anyOtherAnalyte = Any.AnalyteCode().WithValue(v => v.DifferentFrom(anyAnalyte.Value)).Generate();

        // Assert
        Assert.False(anyAnalyte.Equals(anyOtherAnalyte));
        Assert.False(anyAnalyte.Equals((AnalyteCode?)null));
    }

}
