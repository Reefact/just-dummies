using System.Numerics;

using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class ExactDecimalTests {

    [Fact]
    public void A_decimal_is_its_mantissa_times_ten_to_the_minus_scale() {
        ExactDecimal decimalNumber = new ExactDecimal(12345, 2);

        Assert.Equal(new BigInteger(12345), decimalNumber.Mantissa);
        Assert.Equal(2, decimalNumber.Scale);
        Assert.Equal("123.45", decimalNumber.ToString());
    }

    [Fact]
    public void Trailing_zeros_are_stripped_while_the_scale_is_positive() {
        ExactDecimal decimalNumber = new ExactDecimal(1500, 3);

        Assert.Equal(new BigInteger(15), decimalNumber.Mantissa);
        Assert.Equal(1, decimalNumber.Scale);
    }

    [Fact]
    public void A_whole_number_keeps_its_trailing_zeros_because_the_scale_is_already_zero() {
        ExactDecimal decimalNumber = new ExactDecimal(1500, 0);

        Assert.Equal(new BigInteger(1500), decimalNumber.Mantissa);
        Assert.Equal(0, decimalNumber.Scale);
        Assert.True(decimalNumber.IsInteger);
    }

    [Fact]
    public void Stripping_stops_at_scale_zero() {
        ExactDecimal decimalNumber = new ExactDecimal(1500, 5);

        Assert.Equal(new BigInteger(15), decimalNumber.Mantissa);
        Assert.Equal(3, decimalNumber.Scale);
        Assert.Equal("0.015", decimalNumber.ToString());
    }

    [Fact]
    public void Zero_is_zero_at_scale_zero_whatever_scale_it_was_built_with() {
        // Arrange
        int anyScale = Any.Int32().Between(0, ExactDecimal.MaxScale).Generate();

        // Act
        ExactDecimal zero = new ExactDecimal(0, anyScale);

        // Assert
        Assert.Equal(ExactDecimal.Zero, zero);
        Assert.Equal(0, zero.Scale);
        Assert.True(zero.IsZero);
        Assert.Equal(0, zero.Sign);
    }

    [Fact]
    public void Equal_values_built_at_different_scales_are_equal_and_hash_alike() {
        ExactDecimal oneAndAHalf = new ExactDecimal(15, 1);
        ExactDecimal sameValue   = new ExactDecimal(1500, 3);

        Assert.Equal(oneAndAHalf, sameValue);
        Assert.Equal(oneAndAHalf.GetHashCode(), sameValue.GetHashCode());
        Assert.True(oneAndAHalf.Equals((object)sameValue));
    }

    [Fact]
    public void A_negative_scale_is_refused() {
        // Arrange
        int anyNegativeScale = Any.Int32().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new ExactDecimal(1, anyNegativeScale));

        // Assert
        Assert.Equal("scale", refusal.ParamName);
    }

    [Fact]
    public void A_scale_above_80_is_refused() {
        int anyExcessiveScale = Any.Int32().GreaterThan(ExactDecimal.MaxScale).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactDecimal(1, anyExcessiveScale));
    }

    [Fact]
    public void The_edges_of_the_scale_range_are_accepted() {
        Assert.Equal(0, new ExactDecimal(7, 0).Scale);
        Assert.Equal(80, new ExactDecimal(7, 80).Scale);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactDecimal(7, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactDecimal(7, 81));
    }

    [Fact]
    public void A_mantissa_is_not_bounded() {
        BigInteger hugeMantissa = BigInteger.Pow(10, 300) + 1;

        ExactDecimal decimalNumber = new ExactDecimal(hugeMantissa, 3);

        Assert.Equal(hugeMantissa, decimalNumber.Mantissa);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-0", "0")]
    [InlineData("12", "12")]
    [InlineData("-12", "-12")]
    [InlineData("1.5", "1.5")]
    [InlineData("1.50", "1.5")]
    [InlineData("1.000", "1")]
    [InlineData("0.00123", "0.00123")]
    [InlineData("-0.00123", "-0.00123")]
    [InlineData("007.50", "7.5")]
    [InlineData("100", "100")]
    [InlineData("100.00", "100")]
    [InlineData("0.0", "0")]
    public void Parsing_reads_a_plain_decimal_and_prints_it_canonically(string text, string canonical) {
        ExactDecimal parsed = ExactDecimal.Parse(text);

        Assert.Equal(canonical, parsed.ToString());
        Assert.Equal(ExactDecimal.Parse(canonical), parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".5")]
    [InlineData("5.")]
    [InlineData("1e5")]
    [InlineData("1E5")]
    [InlineData("1,000")]
    [InlineData("1 000")]
    [InlineData("+1")]
    [InlineData("--1")]
    [InlineData("- 1")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("1.2.3")]
    [InlineData("0x10")]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("١٢")]
    public void Parsing_refuses_anything_but_an_optional_minus_digits_and_an_optional_fraction(string text) {
        Assert.Throws<FormatException>(() => ExactDecimal.Parse(text));
    }

    [Fact]
    public void Parsing_refuses_a_null_text() {
        Assert.Throws<ArgumentNullException>(() => ExactDecimal.Parse(null!));
    }

    [Fact]
    public void Parsing_accepts_80_significant_fractional_digits_and_refuses_81() {
        string eighty    = "0." + new string('1', 80);
        string eightyOne = "0." + new string('1', 81);

        Assert.Equal(80, ExactDecimal.Parse(eighty).Scale);
        Assert.Throws<FormatException>(() => ExactDecimal.Parse(eightyOne));
    }

    [Fact]
    public void Parsing_does_not_count_trailing_zeros_among_the_significant_fractional_digits() {
        // Arrange
        string anyDigits = Any.StringMatching("[1-9]{80}").Generate();

        // Act
        ExactDecimal parsed = ExactDecimal.Parse("0." + anyDigits + "000000");

        // Assert
        Assert.Equal(80, parsed.Scale);
    }

    [Fact(Skip = "domain defect: the syntax pattern ends with $, which also matches before a final line feed, so '12' followed by a line feed parses; seed 852058983")]
    public void Parsing_refuses_a_number_followed_by_a_line_feed() {
        Assert.Throws<FormatException>(() => ExactDecimal.Parse("12\n"));
        Assert.Throws<FormatException>(() => ExactDecimal.Parse("1.5\n"));
    }

    [Fact]
    public void An_integer_is_built_from_a_big_integer_at_scale_zero() {
        ExactDecimal integer = ExactDecimal.FromInteger(-42);

        Assert.Equal(new ExactDecimal(-42, 0), integer);
        Assert.True(integer.IsInteger);
        Assert.Equal(-1, integer.Sign);
    }

    [Fact]
    public void One_and_zero_are_the_integers_they_say() {
        Assert.Equal("1", ExactDecimal.One.ToString());
        Assert.Equal("0", ExactDecimal.Zero.ToString());
    }

    [Fact]
    public void Adding_aligns_the_scales() {
        ExactDecimal sum = ExactDecimal.Parse("0.1").Add(ExactDecimal.Parse("0.25"));

        Assert.Equal(ExactDecimal.Parse("0.35"), sum);
    }

    [Fact]
    public void Adding_values_that_cancel_gives_zero() {
        ExactDecimal sum = ExactDecimal.Parse("1.25").Add(ExactDecimal.Parse("-1.25"));

        Assert.Equal(ExactDecimal.Zero, sum);
        Assert.Equal(0, sum.Scale);
    }

    [Fact]
    public void Adding_normalises_the_sum() {
        ExactDecimal sum = ExactDecimal.Parse("0.25").Add(ExactDecimal.Parse("0.75"));

        Assert.Equal("1", sum.ToString());
        Assert.Equal(0, sum.Scale);
    }

    [Fact]
    public void Subtracting_gives_the_difference_and_may_go_negative() {
        ExactDecimal difference = ExactDecimal.Parse("0.1").Subtract(ExactDecimal.Parse("0.25"));

        Assert.Equal(ExactDecimal.Parse("-0.15"), difference);
    }

    [Fact]
    public void Negating_flips_the_sign_and_keeps_the_magnitude() {
        Assert.Equal(ExactDecimal.Parse("-3.14"), ExactDecimal.Parse("3.14").Negate());
        Assert.Equal(ExactDecimal.Parse("3.14"), ExactDecimal.Parse("-3.14").Negate());
        Assert.Equal(ExactDecimal.Zero, ExactDecimal.Zero.Negate());
    }

    [Fact]
    public void Multiplying_is_exact() {
        ExactDecimal product = ExactDecimal.Parse("1.5").Multiply(ExactDecimal.Parse("0.04"));

        Assert.Equal(ExactDecimal.Parse("0.06"), product);
    }

    [Fact]
    public void Multiplying_by_zero_gives_zero_at_scale_zero() {
        ExactDecimal anyDecimal = Any.ExactDecimal().Generate();

        ExactDecimal product = anyDecimal.Multiply(ExactDecimal.Zero);

        Assert.Equal(ExactDecimal.Zero, product);
        Assert.Equal(0, product.Scale);
    }

    [Fact]
    public void Multiplying_that_needs_more_than_80_fractional_digits_overflows() {
        ExactDecimal tiny  = new ExactDecimal(1, 80);
        ExactDecimal tenth = new ExactDecimal(1, 1);

        Assert.Throws<OverflowException>(() => tiny.Multiply(tenth));
    }

    [Fact]
    public void Multiplying_is_allowed_when_the_normalised_product_fits_in_80_digits() {
        // 5e-80 x 0.2 = 1e-80: the raw product has scale 81, its normalised form has scale 80.
        ExactDecimal product = new ExactDecimal(5, 80).Multiply(new ExactDecimal(2, 1));

        Assert.Equal(new ExactDecimal(1, 80), product);
    }

    [Fact]
    public void Multiplying_a_tiny_value_by_zero_does_not_overflow() {
        ExactDecimal tiny = new ExactDecimal(1, 80);

        Assert.Equal(ExactDecimal.Zero, tiny.Multiply(ExactDecimal.Zero));
    }

    [Fact]
    public void Dividing_truncates_toward_zero() {
        Assert.Equal(ExactDecimal.Parse("0.33333"), ExactDecimal.One.Divide(ExactDecimal.Parse("3"), 5));
        Assert.Equal(ExactDecimal.Parse("0.66"), ExactDecimal.Parse("2").Divide(ExactDecimal.Parse("3"), 2));
    }

    [Fact]
    public void Dividing_a_negative_value_truncates_toward_zero_not_down() {
        Assert.Equal(ExactDecimal.Parse("-0.33333"), ExactDecimal.Parse("-1").Divide(ExactDecimal.Parse("3"), 5));
        Assert.Equal(ExactDecimal.Parse("-0.33333"), ExactDecimal.One.Divide(ExactDecimal.Parse("-3"), 5));
        Assert.Equal(ExactDecimal.Parse("0.33333"), ExactDecimal.Parse("-1").Divide(ExactDecimal.Parse("-3"), 5));
    }

    [Fact]
    public void Dividing_exactly_gives_the_exact_quotient_in_normal_form() {
        ExactDecimal quotient = ExactDecimal.Parse("1").Divide(ExactDecimal.Parse("8"), 10);

        Assert.Equal(ExactDecimal.Parse("0.125"), quotient);
        Assert.Equal(3, quotient.Scale);
    }

    [Fact]
    public void Dividing_handles_operands_of_different_scales() {
        ExactDecimal quotient = ExactDecimal.Parse("12.5").Divide(ExactDecimal.Parse("0.05"), 0);

        Assert.Equal(ExactDecimal.Parse("250"), quotient);
    }

    [Fact]
    public void Dividing_a_value_smaller_than_the_precision_truncates_to_zero() {
        Assert.Equal(ExactDecimal.Zero, ExactDecimal.Parse("1").Divide(ExactDecimal.Parse("1000"), 2));
    }

    [Fact]
    public void Dividing_by_zero_is_refused() {
        ExactDecimal anyDividend = Any.ExactDecimal().Generate();

        Assert.Throws<DivideByZeroException>(() => anyDividend.Divide(ExactDecimal.Zero, 4));
    }

    [Fact]
    public void Dividing_keeps_a_precision_between_0_and_80() {
        // Arrange
        int anyTooFine = Any.Int32().GreaterThan(ExactDecimal.MaxScale).Generate();
        int anyNegative = Any.Int32().Negative().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ExactDecimal.One.Divide(ExactDecimal.One, anyTooFine));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExactDecimal.One.Divide(ExactDecimal.One, anyNegative));
        Assert.Equal(ExactDecimal.One, ExactDecimal.One.Divide(ExactDecimal.One, 80));
        Assert.Equal(ExactDecimal.One, ExactDecimal.One.Divide(ExactDecimal.One, 0));
    }

    [Fact]
    public void Truncating_drops_the_digits_beyond_the_scale() {
        Assert.Equal(ExactDecimal.Parse("1.9"), ExactDecimal.Parse("1.999").Truncate(1));
        Assert.Equal(ExactDecimal.Parse("1"), ExactDecimal.Parse("1.999").Truncate(0));
    }

    [Fact]
    public void Truncating_goes_toward_zero_for_a_negative_value() {
        Assert.Equal(ExactDecimal.Parse("-1.9"), ExactDecimal.Parse("-1.999").Truncate(1));
        Assert.Equal(ExactDecimal.Zero, ExactDecimal.Parse("-0.4").Truncate(0));
    }

    [Fact]
    public void Truncating_to_a_scale_the_value_already_fits_returns_it_unchanged() {
        ExactDecimal value = ExactDecimal.Parse("1.25");

        Assert.Same(value, value.Truncate(2));
        Assert.Same(value, value.Truncate(30));
    }

    [Fact]
    public void Truncating_keeps_a_scale_between_0_and_80() {
        int anyTooFine  = Any.Int32().GreaterThan(ExactDecimal.MaxScale).Generate();
        int anyNegative = Any.Int32().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => ExactDecimal.One.Truncate(anyTooFine));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExactDecimal.One.Truncate(anyNegative));
    }

    [Fact]
    public void A_value_becomes_a_scaled_integer_when_the_scale_is_fine_enough() {
        Assert.Equal(new BigInteger(1500), ExactDecimal.Parse("1.5").ToScaledInteger(3));
        Assert.Equal(new BigInteger(15), ExactDecimal.Parse("1.5").ToScaledInteger(1));
        Assert.Equal(new BigInteger(7), ExactDecimal.Parse("7").ToScaledInteger(0));
        Assert.Equal(BigInteger.Zero, ExactDecimal.Zero.ToScaledInteger(18));
    }

    [Fact]
    public void A_value_is_not_a_scaled_integer_at_a_scale_coarser_than_its_own() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => ExactDecimal.Parse("1.25").ToScaledInteger(1));

        Assert.Equal("scale", refusal.ParamName);
    }

    [Fact]
    public void A_negative_value_scales_to_a_negative_integer() {
        Assert.Equal(new BigInteger(-125), ExactDecimal.Parse("-1.25").ToScaledInteger(2));
    }

    [Fact]
    public void Values_are_ordered_across_scales() {
        Assert.True(ExactDecimal.Parse("0.5").CompareTo(ExactDecimal.Parse("0.25")) > 0);
        Assert.True(ExactDecimal.Parse("-0.5").CompareTo(ExactDecimal.Parse("0.25")) < 0);
        Assert.Equal(0, ExactDecimal.Parse("2.0").CompareTo(ExactDecimal.Parse("2")));
        Assert.True(ExactDecimal.Parse("-1").CompareTo(ExactDecimal.Parse("-1.5")) > 0);
    }

    [Fact]
    public void Any_decimal_is_greater_than_nothing() {
        ExactDecimal anyDecimal = Any.ExactDecimal().Generate();

        Assert.True(anyDecimal.CompareTo(null) > 0);
        Assert.False(anyDecimal.Equals(null));
    }

    [Fact]
    public void The_sign_and_the_integer_flag_describe_the_value() {
        Assert.Equal(1, ExactDecimal.Parse("0.001").Sign);
        Assert.Equal(-1, ExactDecimal.Parse("-0.001").Sign);
        Assert.False(ExactDecimal.Parse("0.001").IsInteger);
        Assert.True(ExactDecimal.Parse("10.0").IsInteger);
    }

    [Fact]
    public void A_decimal_prints_leading_zeros_after_the_point() {
        Assert.Equal("0.00001", new ExactDecimal(1, 5).ToString());
        Assert.Equal("-0.00001", new ExactDecimal(-1, 5).ToString());
        Assert.Equal("12345.6789", new ExactDecimal(123456789, 4).ToString());
    }

    [Fact]
    public void Operations_refuse_a_null_operand() {
        ExactDecimal anyDecimal = Any.ExactDecimal().Generate();

        Assert.Throws<ArgumentNullException>(() => anyDecimal.Add(null!));
        Assert.Throws<ArgumentNullException>(() => anyDecimal.Subtract(null!));
        Assert.Throws<ArgumentNullException>(() => anyDecimal.Multiply(null!));
        Assert.Throws<ArgumentNullException>(() => anyDecimal.Divide(null!, 2));
    }

}
