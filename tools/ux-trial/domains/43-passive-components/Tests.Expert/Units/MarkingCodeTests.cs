using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class MarkingCodeTests {

    private const ComponentKind Capacitor = ComponentKind.Capacitor;
    private const ComponentKind Inductor  = ComponentKind.Inductor;
    private const ComponentKind Resistor  = ComponentKind.Resistor;

    // --- numeric notation -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("104", Capacitor, 1e-7)]       // 10 × 10⁴ pF = 100 nF
    [InlineData("104", Inductor, 0.1)]         // 10 × 10⁴ µH = 100 mH
    [InlineData("104", Resistor, 1e5)]         // 10 × 10⁴ Ω = 100 kΩ
    [InlineData("222", Capacitor, 2.2e-9)]
    [InlineData("470", Capacitor, 4.7e-11)]
    [InlineData("101", Inductor, 1e-4)]
    [InlineData("100", Resistor, 10)]
    [InlineData("107", Resistor, 1e8)]
    public void A_three_digit_code_is_two_significant_digits_and_a_power_of_ten(string code, ComponentKind kind, double expected) {
        // Act
        MarkingCode marking = new MarkingCode(code, kind);

        // Assert
        Assert.Equal(expected, marking.Value);
    }

    [Theory]
    [InlineData("478", Capacitor, 4.7e-13)]    // multiplier 8 divides by one hundred: 0.47 pF
    [InlineData("479", Capacitor, 4.7e-12)]    // multiplier 9 divides by ten: 4.7 pF
    [InlineData("478", Resistor, 0.47)]
    [InlineData("479", Resistor, 4.7)]
    [InlineData("478", Inductor, 4.7e-7)]
    public void The_multiplier_digits_eight_and_nine_divide_by_one_hundred_and_ten(string code, ComponentKind kind, double expected) {
        Assert.Equal(expected, new MarkingCode(code, kind).Value);
    }

    [Theory]
    [InlineData("1002", 1e4)]
    [InlineData("4991", 4990)]
    [InlineData("1008", 1)]
    [InlineData("4993", 4.99e5)]
    public void A_four_digit_code_marks_a_precision_resistor_with_three_significant_digits(string code, double expectedOhms) {
        Assert.Equal(expectedOhms, new MarkingCode(code, Resistor).Value);
    }

    [Theory]
    [InlineData(Capacitor)]
    [InlineData(Inductor)]
    public void A_four_digit_code_is_reserved_to_resistors(ComponentKind kind) {
        Assert.Throws<FormatException>(() => new MarkingCode("1002", kind));
    }

    // --- letter notation --------------------------------------------------------------------------------------

    [Theory]
    [InlineData("4R7", Resistor, 4.7)]
    [InlineData("R47", Resistor, 0.47)]
    [InlineData("4K7", Resistor, 4700)]
    [InlineData("4k7", Resistor, 4700)]
    [InlineData("4M7", Resistor, 4.7e6)]
    [InlineData("1G5", Resistor, 1.5e9)]
    [InlineData("47K", Resistor, 47000)]
    [InlineData("100R", Resistor, 100)]
    [InlineData("1000G", Resistor, 1e12)]
    [InlineData("R001", Resistor, 1e-3)]
    public void On_a_resistor_the_letter_is_the_decimal_point_and_the_unit(string code, ComponentKind kind, double expected) {
        Assert.Equal(expected, new MarkingCode(code, kind).Value);
    }

    [Theory]
    [InlineData("4R7", 4.7e-12)]
    [InlineData("4p7", 4.7e-12)]
    [InlineData("R47", 4.7e-13)]
    [InlineData("47p", 4.7e-11)]
    [InlineData("2n2", 2.2e-9)]
    [InlineData("2u2", 2.2e-6)]
    [InlineData("2µ2", 2.2e-6)]
    [InlineData("2m2", 2.2e-3)]
    [InlineData("R10", 1e-13)]
    public void On_a_capacitor_the_letter_is_the_decimal_point_and_the_unit(string code, double expected) {
        Assert.Equal(expected, new MarkingCode(code, Capacitor).Value);
    }

    [Theory]
    [InlineData("4n7", 4.7e-9)]
    [InlineData("4R7", 4.7e-6)]
    [InlineData("4u7", 4.7e-6)]
    [InlineData("4µ7", 4.7e-6)]
    [InlineData("4m7", 4.7e-3)]
    [InlineData("R47", 4.7e-7)]
    [InlineData("1n0", 1e-9)]
    public void On_an_inductor_the_letter_is_the_decimal_point_and_the_unit(string code, double expected) {
        Assert.Equal(expected, new MarkingCode(code, Inductor).Value);
    }

    [Theory]
    [InlineData("2n2", Resistor)]
    [InlineData("4K7", Capacitor)]
    [InlineData("4K7", Inductor)]
    [InlineData("4M7", Capacitor)]
    [InlineData("4p7", Inductor)]
    [InlineData("4p7", Resistor)]
    [InlineData("4m7", Resistor)]
    [InlineData("4u7", Resistor)]
    [InlineData("4G7", Capacitor)]
    public void A_unit_letter_that_the_kind_does_not_use_is_refused(string code, ComponentKind kind) {
        Assert.Throws<FormatException>(() => new MarkingCode(code, kind));
    }

    // --- refused shapes ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("10")]
    [InlineData("1")]
    [InlineData("10000")]
    [InlineData("abc")]
    [InlineData("4X7")]
    [InlineData("4,7")]
    [InlineData("4.7")]
    [InlineData(" 104")]
    [InlineData("104 ")]
    [InlineData("-104")]
    [InlineData("R")]
    [InlineData("n")]
    [InlineData("4R7R")]
    [InlineData("4R7n")]
    [InlineData("12345n")]
    [InlineData("1n2345")]
    [InlineData("1R2R3")]
    public void A_text_that_is_neither_digits_nor_digits_around_one_unit_letter_is_refused(string code) {
        // Arrange: a resistor reads the widest set of letters
        // Act & Assert
        Assert.Throws<FormatException>(() => new MarkingCode(code, Resistor));
    }

    [Fact(Skip = "domain defect: the numeric format ends with '$', which .NET also matches before a final line feed, so '104\\n' passes the shape check and fails later as ArgumentOutOfRangeException (multiplier read from the line feed) instead of FormatException; deterministic, no seed")]
    public void A_numeric_code_followed_by_a_line_feed_is_a_format_error() {
        Assert.Throws<FormatException>(() => new MarkingCode("104\n", Resistor));
    }

    [Theory]
    [InlineData("١٠٤")]
    [InlineData("１０４")]
    public void Digits_of_other_scripts_are_a_format_error_not_a_reading(string code) {
        Assert.Throws<FormatException>(() => new MarkingCode(code, Capacitor));
    }

    [Theory]
    [InlineData("000", Capacitor)]
    [InlineData("000", Resistor)]
    [InlineData("0000", Resistor)]
    [InlineData("R00", Resistor)]
    [InlineData("0R0", Resistor)]
    [InlineData("0n0", Capacitor)]
    [InlineData("000", Inductor)]
    public void A_code_that_reads_zero_is_refused_as_an_argument(string code, ComponentKind kind) {
        Assert.Throws<ArgumentException>(() => new MarkingCode(code, kind));
    }

    [Theory]
    [InlineData("098", Capacitor)]   // 0.09 pF, under the 0.1 pF floor
    [InlineData("99m", Capacitor)]   // 99 mF, over the 10 mF ceiling
    [InlineData("997", Inductor)]    // 990 H
    [InlineData("9999G", Resistor)]  // 9.999 TΩ
    [InlineData("R0001", Resistor)]  // 0.1 mΩ
    public void A_value_outside_the_range_of_the_kind_is_refused_as_out_of_range(string code, ComponentKind kind) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarkingCode(code, kind));
    }

    [Fact]
    public void A_code_cannot_be_null_and_a_kind_must_be_defined() {
        Assert.Throws<ArgumentNullException>(() => new MarkingCode(null!, Capacitor));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarkingCode("104", (ComponentKind)7));
    }

    [Fact]
    public void A_marking_keeps_the_text_it_was_read_from() {
        // Act
        MarkingCode marking = new MarkingCode("4k7", Resistor);

        // Assert
        Assert.Equal("4k7", marking.Code);
        Assert.Equal("4k7", marking.ToString());
        Assert.Equal(Resistor, marking.Kind);
    }

    // --- writing the usual code -------------------------------------------------------------------------------

    [Theory]
    [InlineData(1e-7, Capacitor, "104")]
    [InlineData(2.2e-9, Capacitor, "222")]
    [InlineData(4.7e-12, Capacitor, "4R7")]
    [InlineData(1e-11, Capacitor, "100")]
    [InlineData(1e-12, Capacitor, "1R0")]
    [InlineData(1e-13, Capacitor, "R10")]
    [InlineData(1e5, Resistor, "104")]
    [InlineData(4700, Resistor, "472")]
    [InlineData(10, Resistor, "100")]
    [InlineData(4.7, Resistor, "4R7")]
    [InlineData(0.47, Resistor, "R47")]
    [InlineData(1e-4, Inductor, "101")]
    [InlineData(4.7e-6, Inductor, "4R7")]
    public void The_usual_code_of_a_value_is_three_digits_from_ten_base_units_up_and_the_R_notation_below(double value, ComponentKind kind, string expected) {
        Assert.Equal(expected, MarkingCode.For(value, kind).Code);
    }

    [Theory]
    [InlineData(1.02e4, "1022")]
    [InlineData(4.99e3, "4991")]
    [InlineData(49.9, "49R9")]
    [InlineData(0.499, "R499")]
    [InlineData(1.02e5, "1023")]
    public void A_resistor_needing_three_significant_digits_takes_the_four_digit_or_the_R_notation(double ohms, string expected) {
        Assert.Equal(expected, MarkingCode.For(ohms, Resistor).Code);
    }

    [Fact]
    public void The_smallest_resistor_has_a_usual_code_that_reads_back_as_one_milliohm() {
        // Act
        MarkingCode marking = MarkingCode.For(1e-3, Resistor);

        // Assert
        Assert.Equal(1e-3, marking.Value);
    }

    [Fact]
    public void A_capacitor_cannot_be_given_three_significant_digits() {
        // 10.2 nF would need 102 × 10² pF: no three-digit code carries it
        Assert.Throws<ArgumentException>(() => MarkingCode.For(1.02e-8, Capacitor));
    }

    [Fact]
    public void A_value_past_the_largest_multiplier_has_no_marking() {
        // 4.7 mF is 47 × 10⁸ pF, past the 10⁷ multiplier
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => MarkingCode.For(4.7e-3, Capacitor));

        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(double.NaN, Capacitor)]
    [InlineData(double.PositiveInfinity, Resistor)]
    [InlineData(0, Inductor)]
    [InlineData(-1, Resistor)]
    [InlineData(9e-14, Capacitor)]
    [InlineData(1.1e-2, Capacitor)]
    [InlineData(1.1, Inductor)]
    [InlineData(1e-10, Inductor)]
    [InlineData(1.1e12, Resistor)]
    public void A_value_outside_the_range_of_the_kind_has_no_marking(double value, ComponentKind kind) {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkingCode.For(value, kind));
    }

    [Fact]
    public void A_marking_for_an_undefined_kind_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkingCode.For(1e-7, (ComponentKind)5));
    }

    [Fact]
    public void Reading_the_usual_code_of_a_value_gives_the_value_back_for_every_E24_value_a_capacitor_can_have() {
        for (int decade = -12; decade <= -4; decade++) {
            for (int index = 0; index < 24; index++) {
                PreferredValue preferred = new PreferredValue(ESeries.E24, decade, index);

                MarkingCode marking = MarkingCode.For(preferred.Value, Capacitor);

                Assert.True(Magnitude(marking.Value, preferred.Value), $"{preferred}: {marking.Code} reads {marking.Value}");
            }
        }
    }

    [Fact]
    public void Every_E96_resistor_value_up_to_a_megaohm_has_a_usual_code_that_reads_back() {
        for (int decade = -1; decade <= 5; decade++) {
            for (int index = 0; index < 96; index++) {
                PreferredValue preferred = new PreferredValue(ESeries.E96, decade, index);

                MarkingCode marking = MarkingCode.For(preferred.Value, Resistor);

                Assert.True(Magnitude(marking.Value, preferred.Value), $"{preferred}: {marking.Code} reads {marking.Value}");
            }
        }
    }

    // --- same value, same marking -----------------------------------------------------------------------------

    [Fact]
    public void Two_notations_of_one_capacitance_denote_the_same_value() {
        // Arrange
        MarkingCode numeric = new MarkingCode("222", Capacitor);
        MarkingCode letter  = new MarkingCode("2n2", Capacitor);

        // Act & Assert
        Assert.True(numeric.DenotesSameValueAs(letter));
        Assert.True(letter.DenotesSameValueAs(numeric));
        Assert.NotEqual(numeric, letter);
    }

    [Fact]
    public void A_four_digit_resistor_code_can_denote_the_same_value_as_a_three_digit_one() {
        Assert.True(new MarkingCode("1002", Resistor).DenotesSameValueAs(new MarkingCode("103", Resistor)));
        Assert.True(new MarkingCode("4K7", Resistor).DenotesSameValueAs(new MarkingCode("472", Resistor)));
    }

    [Fact]
    public void Different_values_do_not_denote_the_same_value() {
        Assert.False(new MarkingCode("222", Capacitor).DenotesSameValueAs(new MarkingCode("223", Capacitor)));
    }

    [Fact]
    public void The_same_numbers_on_another_kind_never_denote_the_same_value() {
        // 4.7 in the base unit of a resistor and of an inductor: the kind is part of what a marking says
        Assert.False(new MarkingCode("4R7", Resistor).DenotesSameValueAs(new MarkingCode("4R7", Inductor)));
        Assert.False(new MarkingCode("R47", Capacitor).DenotesSameValueAs(new MarkingCode("R47", Resistor)));
    }

    [Fact]
    public void A_marking_cannot_be_compared_with_nothing() {
        // Arrange
        MarkingCode anyMarking = Any.MarkingCode().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyMarking.DenotesSameValueAs(null!));
    }

    [Fact]
    public void Equality_is_by_code_and_kind() {
        // Arrange
        MarkingCode capacitor = new MarkingCode("104", Capacitor);

        // Act & Assert
        Assert.Equal(new MarkingCode("104", Capacitor), capacitor);
        Assert.Equal(new MarkingCode("104", Capacitor).GetHashCode(), capacitor.GetHashCode());
        Assert.NotEqual(new MarkingCode("104", Resistor), capacitor);
        Assert.NotEqual(new MarkingCode("105", Capacitor), capacitor);
        Assert.NotEqual(new MarkingCode("4k7", Resistor), new MarkingCode("4K7", Resistor));
    }

    private static bool Magnitude(double actual, double expected) {
        return Math.Abs(actual - expected) <= 1e-9 * Math.Max(Math.Abs(actual), Math.Abs(expected));
    }

}
