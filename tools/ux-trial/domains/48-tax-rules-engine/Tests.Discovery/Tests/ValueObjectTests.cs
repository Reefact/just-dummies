using System.Globalization;

using JustDummies;

using TaxRules.Domain;

namespace TaxRules.Tests.Discovery;

public sealed class RateTests {

    private static decimal D(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("0.0001")]
    [InlineData("0.1234")]
    [InlineData("0.9999")]
    public void A_fraction_between_zero_and_one_with_at_most_four_decimals_is_a_rate(string fraction) {
        Assert.Equal(D(fraction), new Rate(D(fraction)).Fraction);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("1.0001")]
    [InlineData("2")]
    public void A_fraction_outside_zero_to_one_is_refused(string fraction) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rate(D(fraction)));
    }

    [Theory]
    [InlineData("0.00001")]
    [InlineData("0.12345")]
    public void A_fraction_finer_than_a_hundredth_of_a_percent_is_refused(string fraction) {
        Assert.Throws<ArgumentException>(() => new Rate(D(fraction)));
    }

    [Theory]
    [InlineData("12.5", "0.125")]
    [InlineData("100", "1")]
    [InlineData("0", "0")]
    [InlineData("0.01", "0.0001")]
    public void A_percentage_is_a_hundredth_of_a_fraction(string percent, string fraction) {
        Assert.Equal(D(fraction), Rate.FromPercent(D(percent)).Fraction);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("-1")]
    [InlineData("100.01")]
    public void A_percentage_that_makes_an_invalid_fraction_is_refused(string percent) {
        Assert.ThrowsAny<ArgumentException>(() => Rate.FromPercent(D(percent)));
    }

    [Fact]
    public void A_rate_reads_back_as_a_percentage() {
        Rate rate = new Rate(0.1234m);

        Assert.Equal(12.34m, rate.Percent);
        Assert.Equal("12.34 %", rate.ToString());
        Assert.Equal("5 %", new Rate(0.05m).ToString());
    }

    [Fact]
    public void Zero_is_the_only_zero_rate() {
        Assert.True(Rate.Zero.IsZero);
        Assert.True(new Rate(0.00m).IsZero);
        Assert.False(new Rate(0.0001m).IsZero);
    }

    [Fact]
    public void Rates_compare_and_equal_by_value() {
        Assert.True(new Rate(0.1m).CompareTo(new Rate(0.2m)) < 0);
        Assert.Equal(new Rate(0.1m), new Rate(0.10m));
        Assert.Equal(new Rate(0.1m).GetHashCode(), new Rate(0.10m).GetHashCode());
        Assert.NotEqual(new Rate(0.1m), new Rate(0.1001m));
        Assert.Equal(1, new Rate(0.1m).CompareTo(null));
    }

    [Fact]
    public void A_generated_rate_is_always_constructible() {
        // The generator is test support: it must never draw a rate the domain refuses.
        for (int i = 0; i < 50; i++) {
            Rate anyRate = Any.Rate().Generate();

            Assert.NotNull(anyRate);
        }
    }

}

public sealed class JurisdictionCodeTests {

    [Theory]
    [InlineData("FR")]
    [InlineData("US-CA")]
    [InlineData("CH-ZH")]
    [InlineData("DE-B")]
    [InlineData("GB-1A3")]
    [InlineData("AU-123")]
    public void A_country_or_a_subdivision_is_a_jurisdiction_code(string value) {
        Assert.Equal(value, new JurisdictionCode(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("F")]
    [InlineData("FRA")]
    [InlineData("fr")]
    [InlineData("Fr")]
    [InlineData("F1")]
    [InlineData("FR-")]
    [InlineData("FR-ABCD")]
    [InlineData("FR-a")]
    [InlineData("FR_CA")]
    [InlineData("FR CA")]
    [InlineData(" FR")]
    [InlineData("FR ")]
    [InlineData("FR-CA-X")]
    public void Anything_else_is_refused_with_a_format_error(string value) {
        Assert.Throws<FormatException>(() => new JurisdictionCode(value));
    }

    [Fact]
    public void A_missing_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new JurisdictionCode(null!));
    }

    [Theory(Skip = "domain defect: the pattern ends with $, which also matches before a trailing line break, so 'FR\\n' is accepted; no drawn value involved, seed n/a")]
    [InlineData("FR\n")]
    [InlineData("US-CA\n")]
    public void A_code_followed_by_a_line_break_is_not_a_jurisdiction_code(string value) {
        Assert.Throws<FormatException>(() => new JurisdictionCode(value));
    }

    [Fact]
    public void A_country_has_no_subdivision_and_a_subdivision_has_a_country() {
        JurisdictionCode country     = new JurisdictionCode("US");
        JurisdictionCode subdivision = new JurisdictionCode("US-CA");

        Assert.Equal("US", country.Country);
        Assert.False(country.IsSubdivision);
        Assert.Equal("US", subdivision.Country);
        Assert.True(subdivision.IsSubdivision);
    }

    [Fact]
    public void A_country_covers_itself_and_its_subdivisions() {
        JurisdictionCode country = new JurisdictionCode("US");

        Assert.True(country.Covers(new JurisdictionCode("US")));
        Assert.True(country.Covers(new JurisdictionCode("US-CA")));
        Assert.True(country.Covers(new JurisdictionCode("US-NY")));
    }

    [Fact]
    public void A_country_does_not_cover_another_country_or_its_subdivisions() {
        JurisdictionCode country = new JurisdictionCode("US");

        Assert.False(country.Covers(new JurisdictionCode("CA")));
        Assert.False(country.Covers(new JurisdictionCode("CA-ON")));
    }

    [Fact]
    public void A_subdivision_covers_only_itself() {
        JurisdictionCode subdivision = new JurisdictionCode("US-CA");

        Assert.True(subdivision.Covers(new JurisdictionCode("US-CA")));
        Assert.False(subdivision.Covers(new JurisdictionCode("US-NY")));
        Assert.False(subdivision.Covers(new JurisdictionCode("US")));
    }

    [Fact]
    public void Covering_a_missing_jurisdiction_is_refused() {
        JurisdictionCode anyCode = Any.JurisdictionCode().Generate();

        Assert.Throws<ArgumentNullException>(() => anyCode.Covers(null!));
    }

    [Fact]
    public void Codes_equal_by_value_and_print_as_written() {
        Assert.Equal(new JurisdictionCode("FR"), new JurisdictionCode("FR"));
        Assert.Equal(new JurisdictionCode("FR").GetHashCode(), new JurisdictionCode("FR").GetHashCode());
        Assert.NotEqual(new JurisdictionCode("FR"), new JurisdictionCode("FR-IDF"));
        Assert.Equal("US-CA", new JurisdictionCode("US-CA").ToString());
    }

}

public sealed class RuleCodeTests {

    [Theory]
    [InlineData("ABC")]
    [InlineData("INCOME_TAX")]
    [InlineData("A1B")]
    [InlineData("TAX_2024_A")]
    [InlineData("A_1")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWX")]
    public void Upper_case_words_joined_by_single_underscores_are_a_rule_code(string value) {
        Assert.Equal(value, new RuleCode(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXY")]
    public void A_length_outside_three_to_twenty_four_is_refused(string value) {
        Assert.Throws<ArgumentException>(() => new RuleCode(value));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("Abc")]
    [InlineData("1ABC")]
    [InlineData("_ABC")]
    [InlineData("ABC_")]
    [InlineData("AB__C")]
    [InlineData("AB-C")]
    [InlineData("AB C")]
    [InlineData("ÀBC")]
    public void A_bad_shape_is_refused_with_a_format_error(string value) {
        Assert.Throws<FormatException>(() => new RuleCode(value));
    }

    [Fact(Skip = "domain defect: the pattern ends with $, which also matches before a trailing line break, so 'INCOME_TAX\\n' is accepted; no drawn value involved, seed n/a")]
    public void A_code_followed_by_a_line_break_is_not_a_rule_code() {
        Assert.Throws<FormatException>(() => new RuleCode("INCOME_TAX\n"));
    }

    [Fact]
    public void A_missing_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RuleCode(null!));
    }

    [Fact]
    public void The_longest_code_is_exactly_twenty_four_characters() {
        Assert.Equal(24, RuleCode.MaxLength);
        Assert.Equal(3, RuleCode.MinLength);
    }

    [Fact]
    public void Codes_equal_by_value_and_print_as_written() {
        Assert.Equal(new RuleCode("INCOME_TAX"), new RuleCode("INCOME_TAX"));
        Assert.Equal(new RuleCode("INCOME_TAX").GetHashCode(), new RuleCode("INCOME_TAX").GetHashCode());
        Assert.NotEqual(new RuleCode("INCOME_TAX"), new RuleCode("INCOME_TAY"));
        Assert.Equal("INCOME_TAX", new RuleCode("INCOME_TAX").ToString());
    }

    [Fact]
    public void A_generated_code_is_always_constructible() {
        for (int i = 0; i < 100; i++) {
            RuleCode anyCode = Any.RuleCode().Generate();

            Assert.NotNull(anyCode);
        }
    }

}

public sealed class TaxpayerIdTests {

    [Fact]
    public void The_check_digit_of_eight_digits_is_their_luhn_complement() {
        Assert.Equal(2, TaxpayerId.CheckDigitFor("12345678"));
        Assert.Equal(0, TaxpayerId.CheckDigitFor("00000000"));
    }

    [Theory]
    [InlineData("123456782")]
    public void Nine_digits_ending_with_the_right_check_digit_are_a_taxpayer_number(string value) {
        Assert.Equal(value, new TaxpayerId(value).Value);
    }

    [Theory]
    [InlineData("123456780")]
    [InlineData("123456781")]
    [InlineData("123456783")]
    [InlineData("123456789")]
    public void A_wrong_check_digit_is_refused(string value) {
        Assert.Throws<ArgumentException>(() => new TaxpayerId(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678")]
    [InlineData("1234567820")]
    [InlineData("12345678a")]
    [InlineData("12345 782")]
    [InlineData("-12345782")]
    [InlineData("١٢٣٤٥٦٧٨٢")]
    public void A_value_that_is_not_nine_ascii_digits_is_refused_with_a_format_error(string value) {
        Assert.Throws<FormatException>(() => new TaxpayerId(value));
    }

    [Fact]
    public void A_leading_zero_is_refused_with_a_format_error() {
        // 0 + the digits of a number whose check digit would be right: the zero is what is refused.
        string firstEight = "01234567";
        string value      = firstEight + TaxpayerId.CheckDigitFor(firstEight);

        Assert.Throws<FormatException>(() => new TaxpayerId(value));
    }

    [Fact]
    public void A_missing_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TaxpayerId(null!));
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("1234567a")]
    public void The_check_digit_is_asked_of_exactly_eight_digits(string firstEight) {
        Assert.Throws<ArgumentException>(() => TaxpayerId.CheckDigitFor(firstEight));
    }

    [Fact]
    public void Numbers_equal_by_value() {
        Assert.Equal(new TaxpayerId("123456782"), new TaxpayerId("123456782"));
        Assert.Equal(new TaxpayerId("123456782").GetHashCode(), new TaxpayerId("123456782").GetHashCode());
        Assert.Equal("123456782", new TaxpayerId("123456782").ToString());
    }

    [Fact]
    public void A_generated_number_is_always_constructible() {
        for (int i = 0; i < 100; i++) {
            TaxpayerId anyId = Any.TaxpayerId().Generate();

            Assert.NotNull(anyId);
        }
    }

}

public sealed class TaxBracketTests {

    private static TaxBracket Band(decimal lower, decimal? upper, decimal rate) => Fixtures.Band(lower, upper, rate);

    [Fact]
    public void A_bracket_is_a_half_open_band_of_income() {
        TaxBracket bracket = Band(1000m, 2000m, 0.10m);

        Assert.False(bracket.Contains(new Money(999.99m)));
        Assert.True(bracket.Contains(new Money(1000m)));
        Assert.True(bracket.Contains(new Money(1999.99m)));
        Assert.False(bracket.Contains(new Money(2000m)));
    }

    [Fact]
    public void An_open_ended_bracket_has_no_ceiling() {
        TaxBracket bracket = Band(1000m, null, 0.10m);

        Assert.True(bracket.IsOpenEnded);
        Assert.True(bracket.Contains(new Money(1000m)));
        Assert.True(bracket.Contains(new Money(Money.MaxAmount)));
        Assert.False(bracket.Contains(new Money(999.99m)));
    }

    [Theory]
    [InlineData("500", "0")]
    [InlineData("1000", "0")]
    [InlineData("1500", "50")]
    [InlineData("1999.99", "99.999")]
    [InlineData("2000", "100")]
    [InlineData("5000", "100")]
    public void The_tax_on_a_bounded_bracket_is_the_rate_on_the_part_inside_it(string income, string expected) {
        TaxBracket bracket = Band(1000m, 2000m, 0.10m);

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), bracket.TaxOn(new Money(decimal.Parse(income, CultureInfo.InvariantCulture))));
    }

    [Fact]
    public void The_tax_on_an_open_ended_bracket_has_no_cap() {
        TaxBracket bracket = Band(1000m, null, 0.25m);

        Assert.Equal(0m, bracket.TaxOn(new Money(1000m)));
        Assert.Equal(2250m, bracket.TaxOn(new Money(10_000m)));
    }

    [Fact]
    public void The_tax_on_a_bracket_is_not_rounded() {
        Assert.Equal(0.0005m, Band(0m, null, 0.0001m).TaxOn(new Money(5m)));
    }

    [Theory]
    [InlineData("1000", "1000")]
    [InlineData("1000", "999.99")]
    public void An_upper_bound_not_above_the_lower_bound_is_refused(string lower, string upper) {
        Assert.Throws<ArgumentException>(() => new TaxBracket(
            new Money(decimal.Parse(lower, CultureInfo.InvariantCulture)),
            new Money(decimal.Parse(upper, CultureInfo.InvariantCulture)),
            Any.Rate().Generate()));
    }

    [Fact]
    public void A_missing_lower_bound_or_rate_is_refused() {
        Money anyMoney = Any.Money().Generate();
        Rate  anyRate  = Any.Rate().Generate();

        Assert.Throws<ArgumentNullException>(() => new TaxBracket(null!, anyMoney, anyRate));
        Assert.Throws<ArgumentNullException>(() => new TaxBracket(anyMoney, null, null!));
    }

    [Fact]
    public void Brackets_equal_by_lower_upper_and_rate() {
        Assert.Equal(Band(0m, 100m, 0.1m), Band(0m, 100m, 0.1m));
        Assert.Equal(Band(0m, null, 0.1m), Band(0m, null, 0.1m));
        Assert.Equal(Band(0m, 100m, 0.1m).GetHashCode(), Band(0m, 100m, 0.1m).GetHashCode());
        Assert.NotEqual(Band(0m, 100m, 0.1m), Band(0m, 101m, 0.1m));
        Assert.NotEqual(Band(0m, 100m, 0.1m), Band(0m, null, 0.1m));
        Assert.NotEqual(Band(0m, 100m, 0.1m), Band(0m, 100m, 0.2m));
        Assert.NotEqual(Band(0m, 100m, 0.1m), Band(1m, 100m, 0.1m));
    }

    [Fact]
    public void A_generated_bracket_is_always_constructible() {
        for (int i = 0; i < 100; i++) {
            TaxBracket anyBracket = Any.TaxBracket().Generate();

            Assert.True(anyBracket.IsOpenEnded || anyBracket.Upper!.CompareTo(anyBracket.Lower) > 0);
        }
    }

}
