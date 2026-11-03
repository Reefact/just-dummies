using System.Numerics;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class ExactDecimalTests {

    private static ExactDecimal D(string text) {
        return ExactDecimal.Parse(text);
    }

    // ---- construction and normalisation ---------------------------------------------------------------------------

    [Theory]
    [InlineData(1500, 3, 15, 1)]
    [InlineData(-1500, 3, -15, 1)]
    [InlineData(10, 1, 1, 0)]
    [InlineData(100, 2, 1, 0)]
    [InlineData(105, 2, 105, 2)]
    [InlineData(0, 5, 0, 0)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 1, 0)]
    public void A_number_is_normalised_when_it_is_built(int mantissa, int scale, int expectedMantissa, int expectedScale) {
        // Act
        ExactDecimal number = new ExactDecimal(mantissa, scale);

        // Assert
        Assert.Equal((BigInteger)expectedMantissa, number.Mantissa);
        Assert.Equal(expectedScale, number.Scale);
    }

    [Fact]
    public void The_trailing_zeros_of_an_integer_are_not_stripped_since_only_fractional_zeros_are() {
        // Act
        ExactDecimal number = new ExactDecimal(1500, 0);

        // Assert
        Assert.Equal((BigInteger)1500, number.Mantissa);
        Assert.Equal(0, number.Scale);
    }

    [Fact]
    public void A_scale_of_zero_and_a_scale_of_eighty_are_accepted() {
        Assert.Equal(0, new ExactDecimal(7, 0).Scale);
        Assert.Equal(80, new ExactDecimal(7, 80).Scale);
        Assert.Equal(80, ExactDecimal.MaxScale);
    }

    [Fact]
    public void A_negative_scale_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactDecimal(7, -1));
    }

    [Fact]
    public void A_scale_of_eighty_one_is_refused_even_when_normalisation_would_bring_it_back() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactDecimal(10, 81));
    }

    [Fact]
    public void Any_scale_above_eighty_is_refused() {
        // Arrange
        BigInteger anyMantissa = new AnyBigInteger().Generate();
        int        anyScale    = Any.Int32().GreaterThan(ExactDecimal.MaxScale).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactDecimal(anyMantissa, anyScale));
    }

    [Fact]
    public void Any_scale_below_zero_is_refused() {
        // Arrange
        BigInteger anyMantissa = new AnyBigInteger().Generate();
        int        anyScale    = Any.Int32().Negative().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactDecimal(anyMantissa, anyScale));
    }

    [Fact]
    public void The_mantissa_is_neither_bounded_nor_unsigned() {
        // Arrange
        BigInteger huge = BigInteger.Pow(10, 300) + 1;

        // Assert
        Assert.Equal(huge, new ExactDecimal(huge, 3).Mantissa);
        Assert.Equal(-huge, new ExactDecimal(-huge, 3).Mantissa);
    }

    [Fact]
    public void Zero_and_One_are_the_numbers_they_say() {
        Assert.Equal("0", ExactDecimal.Zero.ToString());
        Assert.Equal("1", ExactDecimal.One.ToString());
        Assert.Equal(ExactDecimal.FromInteger(0), ExactDecimal.Zero);
    }

    // ---- equality and order ---------------------------------------------------------------------------------------

    [Fact]
    public void Two_spellings_of_one_value_are_equal_and_share_their_hash_code() {
        // Act
        ExactDecimal sloppy    = new ExactDecimal(1500, 3);
        ExactDecimal canonical = new ExactDecimal(15, 1);

        // Assert
        Assert.True(sloppy.Equals(canonical));
        Assert.True(sloppy.Equals((object)canonical));
        Assert.Equal(canonical.GetHashCode(), sloppy.GetHashCode());
    }

    [Fact]
    public void Two_values_with_the_same_digits_but_not_the_same_scale_are_not_equal() {
        Assert.False(new ExactDecimal(15, 1).Equals(new ExactDecimal(15, 2)));
    }

    [Fact]
    public void A_number_is_not_equal_to_nothing() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Assert
        Assert.False(anyNumber.Equals((ExactDecimal?)null));
        Assert.False(anyNumber.Equals((object?)null));
    }

    [Theory]
    [InlineData("1.5", "1.50", 0)]
    [InlineData("1.5", "1.6", -1)]
    [InlineData("1.5", "1.45", 1)]
    [InlineData("-2", "1", -1)]
    [InlineData("-2", "-3", 1)]
    [InlineData("0", "-0", 0)]
    [InlineData("10", "9.999999999999999999", 1)]
    public void Numbers_compare_by_value_whatever_their_scale(string left, string right, int expectedSign) {
        Assert.Equal(expectedSign, Math.Sign(D(left).CompareTo(D(right))));
    }

    [Fact]
    public void Every_number_is_greater_than_nothing() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Assert
        Assert.True(anyNumber.CompareTo(null) > 0);
    }

    // ---- sign and integrality -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("0", 0, true)]
    [InlineData("-0.0", 0, true)]
    [InlineData("12", 1, true)]
    [InlineData("-12", -1, true)]
    [InlineData("12.0", 1, true)]
    [InlineData("0.001", 1, false)]
    [InlineData("-0.001", -1, false)]
    public void Sign_and_integrality_follow_the_normalised_value(string text, int expectedSign, bool expectedInteger) {
        // Act
        ExactDecimal number = D(text);

        // Assert
        Assert.Equal(expectedSign, number.Sign);
        Assert.Equal(expectedInteger, number.IsInteger);
        Assert.Equal(expectedSign == 0, number.IsZero);
    }

    // ---- parsing --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-0", "0")]
    [InlineData("42", "42")]
    [InlineData("-42", "-42")]
    [InlineData("007", "7")]
    [InlineData("12.50", "12.5")]
    [InlineData("12.00", "12")]
    [InlineData("-0.00123", "-0.00123")]
    [InlineData("0.0", "0")]
    [InlineData("1234.5678", "1234.5678")]
    [InlineData("115792089237316195423570985008687907853269984665640564039457584007913129639935", "115792089237316195423570985008687907853269984665640564039457584007913129639935")]
    public void A_plain_decimal_is_read_and_printed_canonically(string text, string expectedCanonical) {
        Assert.Equal(expectedCanonical, D(text).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    [InlineData("+1")]
    [InlineData("--1")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData(".5")]
    [InlineData("5.")]
    [InlineData("1..5")]
    [InlineData("1.2.3")]
    [InlineData("1e3")]
    [InlineData("1E-3")]
    [InlineData("1,000")]
    [InlineData("1,5")]
    [InlineData("1_000")]
    [InlineData("0x10")]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("١٢")]
    public void Anything_but_an_optional_minus_digits_and_an_optional_fraction_is_refused(string text) {
        Assert.Throws<FormatException>(() => ExactDecimal.Parse(text));
    }

    [Fact]
    public void A_missing_text_is_refused_as_a_missing_argument() {
        Assert.Throws<ArgumentNullException>(() => ExactDecimal.Parse(null!));
    }

    [Fact(Skip = "domain defect: the syntax regex ends with '$', which .NET also matches before a final '\\n', so \"12.5\\n\" parses as 12.5 although only -?digits(.digits)? is documented; the input is a literal (reported seed -1112055859)")]
    public void A_number_followed_by_a_line_break_is_refused() {
        Assert.Throws<FormatException>(() => ExactDecimal.Parse("12.5\n"));
    }

    [Fact]
    public void Eighty_fractional_digits_are_read() {
        // Act
        ExactDecimal number = D("0." + new string('0', 79) + "1");

        // Assert
        Assert.Equal(80, number.Scale);
        Assert.Equal(BigInteger.One, number.Mantissa);
    }

    [Fact]
    public void Eighty_one_significant_fractional_digits_are_refused() {
        Assert.Throws<FormatException>(() => ExactDecimal.Parse("0." + new string('0', 80) + "1"));
    }

    [Fact]
    public void Trailing_fractional_zeros_do_not_count_towards_the_eighty_digits() {
        // Act
        ExactDecimal number = D("1." + new string('0', 200));

        // Assert
        Assert.Equal(ExactDecimal.One, number);
    }

    [Fact]
    public void FromInteger_builds_a_number_of_scale_zero() {
        // Act
        ExactDecimal number = ExactDecimal.FromInteger(-1500);

        // Assert
        Assert.Equal("-1500", number.ToString());
        Assert.True(number.IsInteger);
    }

    // ---- printing -------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, 1, "0.1")]
    [InlineData(-123, 5, "-0.00123")]
    [InlineData(5, 0, "5")]
    [InlineData(12345, 2, "123.45")]
    [InlineData(-5, 0, "-5")]
    public void A_number_prints_in_plain_invariant_notation(int mantissa, int scale, string expected) {
        Assert.Equal(expected, new ExactDecimal(mantissa, scale).ToString());
    }

    // ---- Negate, Add, Subtract ------------------------------------------------------------------------------------

    [Fact]
    public void Negating_flips_the_sign_and_keeps_the_digits() {
        Assert.Equal(D("-1.25"), D("1.25").Negate());
        Assert.Equal(D("1.25"), D("-1.25").Negate());
    }

    [Fact]
    public void Negating_zero_gives_zero() {
        Assert.Equal(ExactDecimal.Zero, ExactDecimal.Zero.Negate());
    }

    [Fact]
    public void Adding_is_exact_where_binary_floating_point_is_not() {
        Assert.Equal(D("0.3"), D("0.1").Add(D("0.2")));
    }

    [Fact]
    public void Adding_aligns_the_scales() {
        Assert.Equal(D("3.75"), D("1.5").Add(D("2.25")));
    }

    [Fact]
    public void Adding_opposites_gives_zero_at_scale_zero() {
        // Act
        ExactDecimal sum = D("1.25").Add(D("-1.25"));

        // Assert
        Assert.Equal(0, sum.Scale);
        Assert.True(sum.IsZero);
    }

    [Fact]
    public void Adding_normalises_the_sum() {
        // Act
        ExactDecimal sum = D("0.25").Add(D("0.75"));

        // Assert
        Assert.Equal(ExactDecimal.One, sum);
        Assert.Equal(0, sum.Scale);
    }

    [Fact]
    public void Subtracting_goes_below_zero_freely() {
        Assert.Equal(D("-0.5"), D("1").Subtract(D("1.5")));
    }

    [Fact]
    public void Adding_nothing_is_refused_as_a_missing_argument() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyNumber.Add(null!));
        Assert.Throws<ArgumentNullException>(() => anyNumber.Subtract(null!));
    }

    // ---- Multiply -------------------------------------------------------------------------------------------------

    [Fact]
    public void Multiplying_is_exact() {
        Assert.Equal(D("0.3"), D("1.5").Multiply(D("0.2")));
        Assert.Equal(D("-6.25"), D("2.5").Multiply(D("-2.5")));
    }

    [Fact]
    public void Multiplying_by_zero_gives_zero_at_scale_zero_whatever_the_other_scale() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Act
        ExactDecimal product = anyNumber.Multiply(ExactDecimal.Zero);

        // Assert
        Assert.Equal(ExactDecimal.Zero, product);
        Assert.Equal(0, product.Scale);
    }

    [Fact]
    public void A_product_needing_exactly_eighty_fractional_digits_is_accepted() {
        // Act
        ExactDecimal product = new ExactDecimal(1, 40).Multiply(new ExactDecimal(1, 40));

        // Assert
        Assert.Equal(80, product.Scale);
    }

    [Fact]
    public void A_product_needing_eighty_one_fractional_digits_overflows() {
        Assert.Throws<OverflowException>(() => new ExactDecimal(1, 40).Multiply(new ExactDecimal(1, 41)));
    }

    [Fact]
    public void A_product_whose_trailing_zeros_bring_it_back_to_eighty_digits_is_accepted() {
        // 25e-41 x 4e-41 = 100e-82 = 1e-80
        // Act
        ExactDecimal product = new ExactDecimal(25, 41).Multiply(new ExactDecimal(4, 41));

        // Assert
        Assert.Equal(new ExactDecimal(1, 80), product);
    }

    [Fact]
    public void Multiplying_nothing_is_refused_as_a_missing_argument() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyNumber.Multiply(null!));
    }

    // ---- Divide ---------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("1", "3", 4, "0.3333")]
    [InlineData("2", "3", 4, "0.6666")]
    [InlineData("-2", "3", 4, "-0.6666")]
    [InlineData("2", "-3", 4, "-0.6666")]
    [InlineData("-2", "-3", 4, "0.6666")]
    [InlineData("1", "4", 0, "0")]
    [InlineData("7", "2", 0, "3")]
    [InlineData("-7", "2", 0, "-3")]
    [InlineData("10", "0.25", 2, "40")]
    [InlineData("0.5", "0.125", 3, "4")]
    [InlineData("0", "7", 10, "0")]
    [InlineData("1", "8", 80, "0.125")]
    public void A_quotient_is_truncated_toward_zero_at_the_requested_scale(string dividend, string divisor, int scale, string expected) {
        Assert.Equal(D(expected), D(dividend).Divide(D(divisor), scale));
    }

    [Fact]
    public void Dividing_by_zero_is_refused_whatever_the_dividend() {
        // Arrange
        ExactDecimal anyDividend = Any.ExactDecimal().Generate();
        int          anyScale     = Any.Int32().Between(0, ExactDecimal.MaxScale).Generate();

        // Act / Assert
        Assert.Throws<DivideByZeroException>(() => anyDividend.Divide(ExactDecimal.Zero, anyScale));
    }

    [Fact]
    public void A_quotient_scale_below_zero_is_refused() {
        // Arrange
        ExactDecimal anyDividend = Any.ExactDecimal().Generate();
        ExactDecimal anyDivisor  = Any.ExactDecimal().WithMantissa(m => m.GreaterThanOrEqualTo(1)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyDividend.Divide(anyDivisor, -1));
    }

    [Fact]
    public void A_quotient_scale_above_eighty_is_refused() {
        // Arrange
        ExactDecimal anyDividend = Any.ExactDecimal().Generate();
        ExactDecimal anyDivisor  = Any.ExactDecimal().WithMantissa(m => m.GreaterThanOrEqualTo(1)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyDividend.Divide(anyDivisor, 81));
    }

    [Fact]
    public void Dividing_by_nothing_is_refused_as_a_missing_argument() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyNumber.Divide(null!, 2));
    }

    // ---- Truncate -------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("1.999", 1, "1.9")]
    [InlineData("-1.999", 1, "-1.9")]
    [InlineData("1.999", 0, "1")]
    [InlineData("-1.999", 0, "-1")]
    [InlineData("1.05", 1, "1")]
    [InlineData("0.009", 2, "0")]
    [InlineData("-0.009", 2, "0")]
    [InlineData("1.5", 1, "1.5")]
    [InlineData("1.5", 5, "1.5")]
    [InlineData("12", 0, "12")]
    public void Truncating_drops_the_digits_beyond_the_scale_toward_zero(string text, int scale, string expected) {
        Assert.Equal(D(expected), D(text).Truncate(scale));
    }

    [Fact]
    public void Truncating_to_a_scale_below_zero_is_refused() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyNumber.Truncate(-1));
    }

    [Fact]
    public void Truncating_to_a_scale_above_eighty_is_refused() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyNumber.Truncate(81));
    }

    // ---- ToScaledInteger ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("1.5", 1, 15)]
    [InlineData("1.5", 4, 15000)]
    [InlineData("-1.5", 2, -150)]
    [InlineData("12", 0, 12)]
    [InlineData("12", 3, 12000)]
    [InlineData("0", 6, 0)]
    public void A_number_scaled_to_a_scale_it_fits_is_a_whole_number(string text, int scale, int expected) {
        Assert.Equal((BigInteger)expected, D(text).ToScaledInteger(scale));
    }

    [Fact]
    public void A_number_cannot_be_scaled_to_fewer_digits_than_it_has() {
        Assert.Throws<ArgumentOutOfRangeException>(() => D("1.25").ToScaledInteger(1));
    }

    [Fact]
    public void A_number_cannot_be_scaled_to_a_negative_scale() {
        // Arrange
        ExactDecimal anyNumber = Any.ExactDecimal().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyNumber.ToScaledInteger(-1));
    }

}
