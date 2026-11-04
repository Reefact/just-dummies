using System.Globalization;

using JustDummies;

using Xunit;

namespace TaxRules.Domain.Tests;

public sealed class TaxpayerIdTests {

    [Theory]
    [InlineData("12345678", 2)]
    [InlineData("10000000", 9)]
    [InlineData("98765432", 4)]
    [InlineData("11111111", 8)]
    [InlineData("99999999", 8)]
    [InlineData("12345679", 0)]
    public void The_check_digit_is_the_luhn_digit_of_the_first_eight(string firstEight, int expected) {
        Assert.Equal(expected, TaxpayerId.CheckDigitFor(firstEight));
    }

    [Theory]
    [InlineData("123456782")]
    [InlineData("100000009")]
    [InlineData("123456790")]
    public void A_number_with_the_right_check_digit_is_accepted(string value) {
        Assert.Equal(value, new TaxpayerId(value).Value);
    }

    [Fact]
    public void A_generated_id_is_nine_digits_not_starting_with_zero() {
        TaxpayerId anyId = new AnyTaxpayerId().Generate();

        Assert.Equal(9, anyId.Value.Length);
        Assert.NotEqual('0', anyId.Value[0]);
    }

    [Fact]
    public void A_wrong_check_digit_is_refused_as_an_argument_error() {
        // Arrange
        string anyFirstEight = Any.StringMatching("[1-9][0-9]{7}").Generate();
        int    rightDigit    = TaxpayerId.CheckDigitFor(anyFirstEight);
        int    anyWrongDigit = Any.Int32().Between(0, 9).DifferentFrom(rightDigit).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new TaxpayerId(anyFirstEight + anyWrongDigit));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Changing_one_digit_of_a_valid_number_is_always_detected() {
        // Arrange
        string anyValid     = new AnyTaxpayerId().Generate().Value;
        int    anyPosition  = Any.Int32().Between(1, 8).Generate();
        char   original     = anyValid[anyPosition];
        char   anyOtherChar = (char)('0' + Any.Int32().Between(0, 9).DifferentFrom(original - '0').Generate());
        string altered      = anyValid.Remove(anyPosition, 1).Insert(anyPosition, anyOtherChar.ToString());

        // Act
        Exception? thrown = Record.Exception(() => new TaxpayerId(altered));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Swapping_the_first_digit_for_another_non_zero_one_is_detected() {
        // Arrange
        string anyValid  = new AnyTaxpayerId().Generate().Value;
        int    anyLead   = Any.Int32().Between(1, 9).DifferentFrom(anyValid[0] - '0').Generate();
        string altered   = anyLead + anyValid.Substring(1);

        // Act
        Exception? thrown = Record.Exception(() => new TaxpayerId(altered));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678")]
    [InlineData("1234567820")]
    [InlineData("12345678a")]
    [InlineData("12345 782")]
    [InlineData("-12345678")]
    [InlineData("12345678٢")]
    public void A_value_that_is_not_nine_ascii_digits_is_refused_as_a_format_error(string value) {
        Assert.Throws<FormatException>(() => new TaxpayerId(value));
    }

    [Fact]
    public void A_leading_zero_is_refused_as_a_format_error_before_the_check_digit_is_looked_at() {
        // Arrange
        string anyBody = Any.StringMatching("0[0-9]{7}").Generate();
        string number  = anyBody + TaxpayerId.CheckDigitFor(anyBody);

        // Act
        Exception? thrown = Record.Exception(() => new TaxpayerId(number));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void A_null_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TaxpayerId(null!));
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("1234567a")]
    [InlineData("")]
    public void CheckDigitFor_demands_exactly_eight_digits(string value) {
        Assert.Throws<ArgumentException>(() => TaxpayerId.CheckDigitFor(value));
    }

    [Fact]
    public void CheckDigitFor_refuses_null() {
        Assert.Throws<ArgumentNullException>(() => TaxpayerId.CheckDigitFor(null!));
    }

    [Fact]
    public void Equality_is_by_value() {
        TaxpayerId anyId  = new AnyTaxpayerId().Generate();
        TaxpayerId sameId = new TaxpayerId(anyId.Value);

        Assert.Equal(anyId, sameId);
        Assert.Equal(anyId.GetHashCode(), sameId.GetHashCode());
        Assert.Equal(anyId.Value, anyId.ToString());
        Assert.False(anyId.Equals(null));
    }

}

public sealed class TaxBracketTests {

    private static Money M(string amount) {
        return new Money(decimal.Parse(amount, CultureInfo.InvariantCulture));
    }

    private static Rate R(string fraction) {
        return new Rate(decimal.Parse(fraction, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void A_bracket_keeps_its_bounds_and_its_rate() {
        // Arrange
        TaxBracket anyBracket = new AnyTaxBracket().Bounded().Generate();

        // Act
        TaxBracket copy = new TaxBracket(anyBracket.Lower, anyBracket.Upper, anyBracket.Rate);

        // Assert
        Assert.Equal(anyBracket, copy);
        Assert.False(copy.IsOpenEnded);
    }

    [Fact]
    public void A_bracket_without_an_upper_bound_is_open_ended() {
        TaxBracket anyBracket = new AnyTaxBracket().WithoutUpper().Generate();

        Assert.True(anyBracket.IsOpenEnded);
        Assert.Null(anyBracket.Upper);
    }

    [Fact]
    public void An_upper_bound_equal_to_the_lower_bound_is_refused() {
        Money anyBound = new AnyMoney().Generate();
        Rate  anyRate  = new AnyRate().Generate();

        Assert.Throws<ArgumentException>(() => new TaxBracket(anyBound, anyBound, anyRate));
    }

    [Fact]
    public void An_upper_bound_below_the_lower_bound_is_refused() {
        // Arrange
        (decimal anySmaller, decimal anyLarger) = Any.Pair(Any.Decimal().Between(0m, 1_000_000m).WithScale(2),
                                                                  Any.Decimal().Between(0m, 1_000_000m).WithScale(2)).Having.Item1LessThanItem2().Generate();
        Rate anyRate = new AnyRate().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new TaxBracket(new Money(anyLarger), new Money(anySmaller), anyRate));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_bracket_that_is_one_cent_wide_is_accepted() {
        TaxBracket bracket = new TaxBracket(M("100"), M("100.01"), R("0.1"));

        Assert.True(bracket.Contains(M("100")));
        Assert.False(bracket.Contains(M("100.01")));
    }

    [Fact]
    public void Null_bounds_or_rates_are_refused() {
        Assert.Throws<ArgumentNullException>(() => new TaxBracket(null!, M("1"), R("0.1")));
        Assert.Throws<ArgumentNullException>(() => new TaxBracket(M("0"), M("1"), null!));
    }

    [Fact]
    public void The_lower_bound_is_inside_and_the_upper_bound_is_outside() {
        TaxBracket bracket = new AnyTaxBracket().Bounded().Generate();

        Assert.True(bracket.Contains(bracket.Lower));
        Assert.False(bracket.Contains(bracket.Upper!));
    }

    [Fact]
    public void An_open_ended_bracket_contains_everything_from_its_lower_bound_up() {
        TaxBracket bracket = new AnyTaxBracket().WithoutUpper().Generate();

        Assert.True(bracket.Contains(bracket.Lower));
        Assert.True(bracket.Contains(new Money(Money.MaxAmount)));
    }

    [Fact]
    public void A_bracket_does_not_contain_an_income_below_its_lower_bound() {
        TaxBracket bracket = new AnyTaxBracket().WithLower(M("500")).Generate();

        Assert.False(bracket.Contains(M("499.99")));
    }

    [Theory]
    [InlineData("5000", 0)]
    [InlineData("10000", 0)]
    [InlineData("10000.01", 0.001)]
    [InlineData("15000", 500)]
    [InlineData("20000", 1000)]
    [InlineData("30000", 1000)]
    public void A_closed_bracket_taxes_only_the_slice_of_income_inside_it(string income, double expected) {
        TaxBracket bracket = new TaxBracket(M("10000"), M("20000"), R("0.1"));

        Assert.Equal((decimal)expected, bracket.TaxOn(M(income)));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("10000", 0)]
    [InlineData("12000", 1000)]
    [InlineData("100000", 45000)]
    public void An_open_ended_bracket_taxes_everything_above_its_lower_bound(string income, double expected) {
        TaxBracket bracket = new TaxBracket(M("10000"), null, R("0.5"));

        Assert.Equal((decimal)expected, bracket.TaxOn(M(income)));
    }

    [Fact]
    public void TaxOn_is_not_rounded() {
        TaxBracket bracket = new TaxBracket(M("0"), null, R("0.1234"));

        Assert.Equal(0.0617m, bracket.TaxOn(M("0.50")));
    }

    [Fact]
    public void Contains_and_TaxOn_refuse_null() {
        TaxBracket anyBracket = new AnyTaxBracket().Generate();

        Assert.Throws<ArgumentNullException>(() => anyBracket.Contains(null!));
        Assert.Throws<ArgumentNullException>(() => anyBracket.TaxOn(null!));
    }

    [Fact]
    public void Equality_compares_bounds_and_rate_including_an_absent_upper_bound() {
        Assert.Equal(new TaxBracket(M("0"), M("10"), R("0.1")), new TaxBracket(M("0"), M("10.00"), R("0.10")));
        Assert.Equal(new TaxBracket(M("0"), null, R("0.1")), new TaxBracket(M("0"), null, R("0.1")));
        Assert.NotEqual(new TaxBracket(M("0"), M("10"), R("0.1")), new TaxBracket(M("0"), null, R("0.1")));
        Assert.NotEqual(new TaxBracket(M("0"), null, R("0.1")), new TaxBracket(M("0"), M("10"), R("0.1")));
        Assert.NotEqual(new TaxBracket(M("0"), M("10"), R("0.1")), new TaxBracket(M("1"), M("10"), R("0.1")));
        Assert.NotEqual(new TaxBracket(M("0"), M("10"), R("0.1")), new TaxBracket(M("0"), M("11"), R("0.1")));
        Assert.NotEqual(new TaxBracket(M("0"), M("10"), R("0.1")), new TaxBracket(M("0"), M("10"), R("0.2")));
    }

    [Fact]
    public void Equal_brackets_have_equal_hash_codes() {
        TaxBracket anyBracket = new AnyTaxBracket().Generate();
        TaxBracket same       = new TaxBracket(anyBracket.Lower, anyBracket.Upper, anyBracket.Rate);

        Assert.Equal(anyBracket.GetHashCode(), same.GetHashCode());
    }

    [Theory]
    [InlineData("0", "10000", "0.1", "[0.00, 10000.00) at 10 %")]
    [InlineData("10000", null, "0.5", "[10000.00, ∞) at 50 %")]
    public void ToString_shows_the_band_and_the_rate(string lower, string? upper, string rate, string expected) {
        TaxBracket bracket = new TaxBracket(M(lower), upper is null ? null : M(upper), R(rate));

        Assert.Equal(expected, bracket.ToString());
    }

}
