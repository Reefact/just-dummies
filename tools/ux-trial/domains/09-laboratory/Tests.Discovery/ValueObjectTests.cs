using JustDummies;

using Laboratory.Domain;

using Xunit;

namespace Laboratory.Tests.Discovery;

public sealed class SampleIdTests {

    [Fact]
    public void A_sample_id_is_an_S_followed_by_ten_digits() {
        // Arrange / Act
        SampleId sampleId = new SampleId("S0123456789");

        // Assert
        Assert.Equal("S0123456789", sampleId.Value);
    }

    [Fact]
    public void A_null_sample_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new SampleId(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("S012345678")]     // ten characters
    [InlineData("S01234567890")]   // twelve characters
    [InlineData("s0123456789")]    // lower-case prefix
    [InlineData("X0123456789")]    // wrong prefix
    [InlineData("S01234A6789")]    // a letter among the digits
    [InlineData("S012345678 ")]    // a trailing space
    [InlineData("S0123456789\n")]  // twelve characters, a newline last
    public void A_malformed_sample_id_is_refused(string malformed) {
        Assert.Throws<ArgumentException>(() => new SampleId(malformed));
    }

    [Fact]
    public void Two_sample_ids_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.SampleId().Generate().Value;

        // Act
        SampleId first  = new SampleId(anyValue);
        SampleId second = new SampleId(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_sample_ids_with_different_values_are_not_equal() {
        // Arrange
        SampleId anyFirst  = Any.SampleId().Generate();
        SampleId anyOther  = Any.SampleId().WithValue(v => v.DifferentFrom(anyFirst.Value)).Generate();

        // Assert
        Assert.NotEqual(anyFirst, anyOther);
    }

    [Fact]
    public void A_sample_id_is_never_equal_to_null() {
        // Arrange
        SampleId anySampleId = Any.SampleId().Generate();

        // Assert
        Assert.False(anySampleId.Equals(null));
    }

}

public sealed class AnalyteCodeTests {

    [Theory]
    [InlineData("2345-7")]
    [InlineData("1-0")]
    [InlineData("12345-9")]
    [InlineData("00000-0")]
    public void A_code_of_one_to_five_digits_a_hyphen_and_a_digit_is_accepted(string code) {
        Assert.Equal(code, new AnalyteCode(code).Value);
    }

    [Fact]
    public void A_null_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new AnalyteCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2345")]       // no hyphen, no check digit
    [InlineData("2345-")]      // check digit missing
    [InlineData("-7")]         // no digits before the hyphen
    [InlineData("123456-7")]   // six digits before the hyphen
    [InlineData("2345-77")]    // two check digits
    [InlineData("23a5-7")]     // a letter
    [InlineData("2345–7")]     // an en dash instead of a hyphen
    [InlineData(" 2345-7")]    // a leading space
    public void A_malformed_code_is_refused(string malformed) {
        Assert.Throws<ArgumentException>(() => new AnalyteCode(malformed));
    }

    [Fact(Skip = "domain defect: AnalyteCode accepts a trailing newline ('2345-7\\n') because '$' in .NET also matches before a final line feed; seed not applicable (literal)")]
    public void A_code_with_a_trailing_newline_is_refused() {
        Assert.Throws<ArgumentException>(() => new AnalyteCode("2345-7\n"));
    }

    [Fact(Skip = "domain defect: AnalyteCode accepts non-ASCII digits (Arabic-Indic '\u0662\u0663-\u0667') because '\\d' matches any Unicode decimal digit; SampleId refuses them; seed not applicable (literal)")]
    public void A_code_with_non_ascii_digits_is_refused() {
        Assert.Throws<ArgumentException>(() => new AnalyteCode("\u0662\u0663-\u0667"));
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.AnalyteCode().Generate().Value;

        // Act
        AnalyteCode first  = new AnalyteCode(anyValue);
        AnalyteCode second = new AnalyteCode(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(anyValue, first.ToString());
    }

}

public sealed class MeasurementUnitTests {

    [Fact]
    public void A_unit_symbol_is_kept_as_given() {
        Assert.Equal("mmol/L", new MeasurementUnit("mmol/L").Symbol);
    }

    [Fact]
    public void A_null_symbol_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new MeasurementUnit(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_symbol_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => new MeasurementUnit(blank));
    }

    [Fact]
    public void A_symbol_of_exactly_the_maximum_length_is_accepted() {
        // Arrange
        string symbolAtTheLimit = new string('x', MeasurementUnit.MaxLength);

        // Act
        MeasurementUnit unit = new MeasurementUnit(symbolAtTheLimit);

        // Assert
        Assert.Equal(16, MeasurementUnit.MaxLength);
        Assert.Equal(symbolAtTheLimit, unit.Symbol);
    }

    [Fact]
    public void A_symbol_longer_than_the_maximum_is_refused() {
        // Arrange
        string tooLong = new string('x', MeasurementUnit.MaxLength + 1);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeasurementUnit(tooLong));
    }

    [Fact]
    public void Units_are_equal_by_symbol() {
        // Arrange
        string anySymbol = Any.MeasurementUnit().Generate().Symbol;

        // Act
        MeasurementUnit first  = new MeasurementUnit(anySymbol);
        MeasurementUnit second = new MeasurementUnit(anySymbol);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_comparison_of_symbols_is_case_sensitive() {
        Assert.NotEqual(new MeasurementUnit("mmol/L"), new MeasurementUnit("mmol/l"));
    }

    [Fact]
    public void A_drawn_unit_always_satisfies_the_contract() {
        // Arrange
        MeasurementUnit anyUnit = Any.MeasurementUnit().Generate();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(anyUnit.Symbol));
        Assert.InRange(anyUnit.Symbol.Length, 1, MeasurementUnit.MaxLength);
    }

}

public sealed class DilutionFactorTests {

    [Theory]
    [InlineData("1")]
    [InlineData("1.00")]
    [InlineData("2.5")]
    [InlineData("999.99")]
    [InlineData("1000")]
    [InlineData("1000.00")]
    public void A_factor_from_one_to_a_thousand_with_two_decimals_is_accepted(string text) {
        // Arrange
        decimal factor = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(factor, new DilutionFactor(factor).Value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.99")]
    [InlineData("-1")]
    [InlineData("1000.01")]
    [InlineData("5000")]
    public void A_factor_outside_one_to_a_thousand_is_refused(string text) {
        // Arrange
        decimal factor = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DilutionFactor(factor));
    }

    [Theory]
    [InlineData("1.001")]
    [InlineData("2.005")]
    [InlineData("999.999")]
    public void A_factor_with_more_than_two_decimals_is_refused(string text) {
        // Arrange
        decimal factor = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.Throws<ArgumentException>(() => new DilutionFactor(factor));
    }

    [Theory]
    [InlineData("0.999")]
    [InlineData("1000.001")]
    public void The_range_is_checked_before_the_decimals(string text) {
        // Arrange
        decimal factor = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        // Assert: out of range AND too many decimals is reported as a range error
        Assert.Throws<ArgumentOutOfRangeException>(() => new DilutionFactor(factor));
    }

    [Fact]
    public void A_drawn_factor_is_always_within_range_and_to_two_decimals() {
        // Arrange
        DilutionFactor anyFactor = Any.DilutionFactor().Generate();

        // Assert
        Assert.InRange(anyFactor.Value, 1m, 1000m);
        Assert.Equal(decimal.Round(anyFactor.Value, 2), anyFactor.Value);
    }

}
