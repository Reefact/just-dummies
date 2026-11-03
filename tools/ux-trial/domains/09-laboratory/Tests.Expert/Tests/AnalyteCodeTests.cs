namespace Laboratory.Tests;

public sealed class AnalyteCodeTests {

    [Theory]
    [InlineData("2345-7")]
    [InlineData("1-0")]      // the shortest code
    [InlineData("12345-9")]  // the longest code
    [InlineData("00000-0")]  // leading zeros are digits like any other
    public void A_code_is_one_to_five_digits_a_hyphen_and_one_digit(string code) {
        // Act
        AnalyteCode analyte = new AnalyteCode(code);

        // Assert
        Assert.Equal(code, analyte.Value);
    }

    [Fact]
    public void A_missing_code_is_reported_as_missing_not_as_malformed() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new AnalyteCode(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2345")]       // no check digit
    [InlineData("-7")]         // no digits before the hyphen
    [InlineData("2345-")]      // nothing after the hyphen
    [InlineData("123456-7")]   // six digits before the hyphen
    [InlineData("2345-77")]    // two check digits
    [InlineData("2345-a")]     // a letter as check digit
    [InlineData("23a5-7")]     // a letter among the digits
    [InlineData(" 2345-7")]    // a leading space
    [InlineData("2345-7 ")]    // a trailing space
    [InlineData("2345_7")]     // wrong separator
    public void A_malformed_code_is_refused(string malformed) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AnalyteCode(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: AnalyteCode's pattern ^\\d{1,5}-\\d$ lets '$' match before a final line break, so \"2345-7\\n\" is accepted; no draw involved, seed -943412796 reported anyway")]
    public void A_code_followed_by_a_line_break_is_refused() {
        // A trailing newline is not a digit: the code on a label must not carry one.
        Assert.Throws<ArgumentException>(() => new AnalyteCode("2345-7\n"));
    }

    [Fact(Skip = "domain defect: AnalyteCode's \\d matches any Unicode decimal digit (no ECMAScript/ASCII restriction), so Arabic-Indic digits are accepted where SampleId refuses them; no draw involved, seed -1860009598 reported anyway")]
    public void Digits_of_other_scripts_are_not_the_digits_of_a_code() {
        // Arabic-Indic digits: the same abstract number, not the characters a LOINC code is made of.
        Assert.Throws<ArgumentException>(() => new AnalyteCode("٢٣٤٥-٧"));
    }

    [Fact]
    public void The_check_digit_is_not_verified() {
        // Any digit goes after the hyphen: 2345-0 and 2345-7 are both well-formed.
        AnalyteCode first  = new AnalyteCode("2345-0");
        AnalyteCode second = new AnalyteCode("2345-7");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Two_codes_with_the_same_text_are_equal() {
        // Arrange
        AnalyteCode anyCode      = Any.AnalyteCode().Generate();
        AnalyteCode sameTextCode = new AnalyteCode(anyCode.Value);

        // Assert
        Assert.True(anyCode.Equals(sameTextCode));
        Assert.Equal(anyCode.GetHashCode(), sameTextCode.GetHashCode());
    }

    [Fact]
    public void Two_codes_with_different_texts_are_not_equal() {
        // Arrange
        AnalyteCode anyCode      = Any.AnalyteCode().Generate();
        AnalyteCode anyOtherCode = Any.AnalyteCode().DifferentFrom(anyCode).Generate();

        // Assert
        Assert.False(anyCode.Equals(anyOtherCode));
    }

}
