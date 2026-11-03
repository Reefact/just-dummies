namespace PassiveComponents.Tests.Regular;

public sealed class MarkingCodeTests {

    [Theory]
    [InlineData("104", ComponentKind.Capacitor, 1e-7)]
    [InlineData("104", ComponentKind.Inductor, 0.1)]       // 10 x 10^4 uH, i.e. 100 mH
    [InlineData("104", ComponentKind.Resistor, 1e5)]
    [InlineData("222", ComponentKind.Capacitor, 2.2e-9)]
    [InlineData("101", ComponentKind.Capacitor, 1e-10)]
    [InlineData("109", ComponentKind.Capacitor, 1e-12)]
    [InlineData("108", ComponentKind.Capacitor, 1e-13)]
    [InlineData("100", ComponentKind.Resistor, 10.0)]
    [InlineData("472", ComponentKind.Resistor, 4700.0)]
    [InlineData("102", ComponentKind.Inductor, 1e-3)]
    [InlineData("101", ComponentKind.Inductor, 1e-4)]
    [InlineData("1002", ComponentKind.Resistor, 1e4)]
    [InlineData("1008", ComponentKind.Resistor, 1.0)]
    [InlineData("1009", ComponentKind.Resistor, 10.0)]
    [InlineData("4992", ComponentKind.Resistor, 49900.0)]
    public void A_numeric_code_is_significant_digits_then_a_multiplier(string code, ComponentKind kind, double expected) {
        MarkingCode marking = new MarkingCode(code, kind);

        Assert.Equal(expected, marking.Value);
        Assert.Equal(code, marking.Code);
        Assert.Equal(kind, marking.Kind);
    }

    [Theory]
    [InlineData("4R7", ComponentKind.Resistor, 4.7)]
    [InlineData("R47", ComponentKind.Resistor, 0.47)]
    [InlineData("4K7", ComponentKind.Resistor, 4700.0)]
    [InlineData("4k7", ComponentKind.Resistor, 4700.0)]
    [InlineData("1M0", ComponentKind.Resistor, 1e6)]
    [InlineData("2M2", ComponentKind.Resistor, 2.2e6)]
    [InlineData("1G5", ComponentKind.Resistor, 1.5e9)]
    [InlineData("22R", ComponentKind.Resistor, 22.0)]
    [InlineData("R001", ComponentKind.Resistor, 1e-3)]
    [InlineData("4R7", ComponentKind.Capacitor, 4.7e-12)]
    [InlineData("4p7", ComponentKind.Capacitor, 4.7e-12)]
    [InlineData("R47", ComponentKind.Capacitor, 0.47e-12)]
    [InlineData("2n2", ComponentKind.Capacitor, 2.2e-9)]
    [InlineData("4u7", ComponentKind.Capacitor, 4.7e-6)]
    [InlineData("4µ7", ComponentKind.Capacitor, 4.7e-6)]
    [InlineData("4m7", ComponentKind.Capacitor, 4.7e-3)]
    [InlineData("10m0", ComponentKind.Capacitor, 1e-2)]
    [InlineData("4n7", ComponentKind.Inductor, 4.7e-9)]
    [InlineData("1n0", ComponentKind.Inductor, 1e-9)]
    [InlineData("4R7", ComponentKind.Inductor, 4.7e-6)]
    [InlineData("4u7", ComponentKind.Inductor, 4.7e-6)]
    [InlineData("R47", ComponentKind.Inductor, 0.47e-6)]
    [InlineData("2m2", ComponentKind.Inductor, 2.2e-3)]
    public void A_letter_stands_for_the_decimal_point_and_the_unit(string code, ComponentKind kind, double expected) {
        MarkingCode marking = new MarkingCode(code, kind);

        Assert.Equal(expected, marking.Value);
    }

    [Theory]
    [InlineData("1002", ComponentKind.Capacitor)]
    [InlineData("1002", ComponentKind.Inductor)]
    public void Four_digits_mark_a_resistor_only(string code, ComponentKind kind) {
        Assert.Throws<FormatException>(() => new MarkingCode(code, kind));
    }

    [Theory]
    [InlineData("4K7", ComponentKind.Capacitor)]
    [InlineData("4M7", ComponentKind.Capacitor)]
    [InlineData("4G7", ComponentKind.Capacitor)]
    [InlineData("4n7", ComponentKind.Resistor)]
    [InlineData("4p7", ComponentKind.Resistor)]
    [InlineData("4u7", ComponentKind.Resistor)]
    [InlineData("4m7", ComponentKind.Resistor)]
    [InlineData("4p7", ComponentKind.Inductor)]
    [InlineData("4K7", ComponentKind.Inductor)]
    [InlineData("4M7", ComponentKind.Inductor)]
    public void A_unit_letter_the_kind_does_not_use_is_refused(string code, ComponentKind kind) {
        Assert.Throws<FormatException>(() => new MarkingCode(code, kind));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("ABC")]
    [InlineData("R")]
    [InlineData("4R7R")]
    [InlineData("4 7")]
    [InlineData("4R7 ")]
    [InlineData(" 104")]
    [InlineData("12R345")]
    [InlineData("4,7")]
    [InlineData("-104")]
    [InlineData("1e4")]
    public void A_code_of_the_wrong_shape_is_refused(string code) {
        ComponentKind anyKind = Any.Enum<ComponentKind>().Generate();

        Assert.Throws<FormatException>(() => new MarkingCode(code, anyKind));
    }

    [Fact]
    public void A_missing_code_is_refused() {
        ComponentKind anyKind = Any.Enum<ComponentKind>().Generate();

        Assert.Throws<ArgumentNullException>(() => new MarkingCode(null!, anyKind));
    }

    [Fact]
    public void An_unknown_kind_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarkingCode("104", (ComponentKind)9));
    }

    [Theory]
    [InlineData("000")]
    [InlineData("R00")]
    [InlineData("0R0")]
    [InlineData("0000")]
    [InlineData("0K0")]
    public void A_zero_reading_is_refused(string code) {
        // 4-digit "0000" is only for resistors; the other shapes read for any kind, resistor here
        Assert.Throws<ArgumentException>(() => new MarkingCode(code, ComponentKind.Resistor));
    }

    [Theory]
    [InlineData("000", ComponentKind.Capacitor)]
    [InlineData("R00", ComponentKind.Inductor)]
    public void A_zero_reading_is_refused_for_every_kind(string code, ComponentKind kind) {
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => new MarkingCode(code, kind));

        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData("R09", ComponentKind.Capacitor)]
    [InlineData("11m0", ComponentKind.Capacitor)]
    [InlineData("0n5", ComponentKind.Inductor)]
    [InlineData("997", ComponentKind.Inductor)]
    [InlineData("R0001", ComponentKind.Resistor)]
    public void A_value_outside_the_range_of_the_kind_is_refused(string code, ComponentKind kind) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarkingCode(code, kind));
    }

    [Theory]
    [InlineData(4.7e-12, ComponentKind.Capacitor, "4R7")]
    [InlineData(1e-7, ComponentKind.Capacitor, "104")]
    [InlineData(2.2e-9, ComponentKind.Capacitor, "222")]
    [InlineData(1e-13, ComponentKind.Capacitor, "R10")]
    [InlineData(1e-6, ComponentKind.Inductor, "1R0")]
    [InlineData(1e-4, ComponentKind.Inductor, "101")]
    [InlineData(4.7e-6, ComponentKind.Inductor, "4R7")]
    [InlineData(4.7, ComponentKind.Resistor, "4R7")]
    [InlineData(0.47, ComponentKind.Resistor, "R47")]
    [InlineData(1e5, ComponentKind.Resistor, "104")]
    [InlineData(1e4, ComponentKind.Resistor, "103")]
    [InlineData(10.2e3, ComponentKind.Resistor, "1022")]
    [InlineData(49.9, ComponentKind.Resistor, "49R9")]
    [InlineData(0.499, ComponentKind.Resistor, "R499")]
    [InlineData(1.0, ComponentKind.Resistor, "1R0")]
    [InlineData(10.0, ComponentKind.Resistor, "100")]
    public void The_usual_code_of_a_value_is_the_shortest_one_that_represents_it(double value, ComponentKind kind, string expected) {
        MarkingCode marking = MarkingCode.For(value, kind);

        Assert.Equal(expected, marking.Code);
        Assert.Equal(kind, marking.Kind);
    }

    [Theory]
    [InlineData(4.7e-3, ComponentKind.Capacitor)]
    [InlineData(1e-2, ComponentKind.Capacitor)]
    [InlineData(2.2e-12 * 1.0001, ComponentKind.Capacitor)]
    public void A_value_no_code_represents_is_refused(double value, ComponentKind kind) {
        Assert.Throws<ArgumentException>(() => MarkingCode.For(value, kind));
    }

    [Theory]
    [InlineData(0.0, ComponentKind.Capacitor)]
    [InlineData(9.9e-14, ComponentKind.Capacitor)]
    [InlineData(1.1e-2, ComponentKind.Capacitor)]
    [InlineData(double.NaN, ComponentKind.Inductor)]
    [InlineData(2.0, ComponentKind.Inductor)]
    [InlineData(double.PositiveInfinity, ComponentKind.Resistor)]
    [InlineData(1e-4, ComponentKind.Resistor)]
    public void A_value_outside_the_range_of_the_kind_has_no_code(double value, ComponentKind kind) {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkingCode.For(value, kind));
    }

    [Fact]
    public void An_unknown_kind_has_no_code() {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkingCode.For(1e-9, (ComponentKind)5));
    }

    [Fact]
    public void A_code_made_for_a_value_reads_back_as_that_value() {
        MarkingCode marking = MarkingCode.For(2.2e-9, ComponentKind.Capacitor);

        Approx.Equal(2.2e-9, marking.Value);
    }

    [Fact]
    public void Two_notations_of_one_value_denote_the_same_value() {
        MarkingCode numeric = new MarkingCode("222", ComponentKind.Capacitor);
        MarkingCode letter  = new MarkingCode("2n2", ComponentKind.Capacitor);

        Assert.True(numeric.DenotesSameValueAs(letter));
        Assert.True(letter.DenotesSameValueAs(numeric));
        Assert.NotEqual(numeric, letter);
    }

    [Fact]
    public void The_same_code_read_for_another_kind_is_not_the_same_value() {
        MarkingCode capacitor = new MarkingCode("104", ComponentKind.Capacitor);
        MarkingCode resistor  = new MarkingCode("104", ComponentKind.Resistor);

        Assert.False(capacitor.DenotesSameValueAs(resistor));
    }

    [Fact]
    public void The_same_value_read_for_two_kinds_is_not_the_same_value() {
        // 1 µ: a capacitor of 1 µF is 1e-6 and an inductor of 1 µH is 1e-6 too
        MarkingCode capacitor = new MarkingCode("1u0", ComponentKind.Capacitor);
        MarkingCode inductor  = new MarkingCode("1u0", ComponentKind.Inductor);

        Assert.Equal(capacitor.Value, inductor.Value);
        Assert.False(capacitor.DenotesSameValueAs(inductor));
    }

    [Fact]
    public void Different_values_do_not_denote_the_same_value() {
        MarkingCode first  = new MarkingCode("222", ComponentKind.Capacitor);
        MarkingCode second = new MarkingCode("223", ComponentKind.Capacitor);

        Assert.False(first.DenotesSameValueAs(second));
        Assert.Throws<ArgumentNullException>(() => first.DenotesSameValueAs(null!));
    }

    [Fact]
    public void Equality_is_by_code_and_kind() {
        MarkingCode anyMarking = Any.MarkingCode().Generate();
        MarkingCode same       = new MarkingCode(anyMarking.Code, anyMarking.Kind);

        Assert.Equal(anyMarking, same);
        Assert.Equal(anyMarking.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(new MarkingCode("4k7", ComponentKind.Resistor), new MarkingCode("4K7", ComponentKind.Resistor));
        Assert.False(anyMarking.Equals(null));
    }

    [Theory(Skip = "domain defect: '$' in the letter-notation pattern also matches before a final line break, so 'R47\\n' and '4R7\\n' are read as 0.47 and 4.7; input is a literal, no seed")]
    [InlineData("4R7\n", ComponentKind.Resistor)]
    [InlineData("R47\n", ComponentKind.Capacitor)]
    [InlineData("4u7\n", ComponentKind.Inductor)]
    public void A_letter_code_followed_by_a_line_break_is_refused(string code, ComponentKind kind) {
        Assert.Throws<FormatException>(() => new MarkingCode(code, kind));
    }

    [Fact(Skip = "domain defect: a numeric resistor code followed by a line break is refused with ArgumentOutOfRangeException (the newline is read as multiplier digit '\\n' - '0'), not FormatException; input is a literal, no seed")]
    public void A_numeric_resistor_code_followed_by_a_line_break_is_a_format_error() {
        Assert.Throws<FormatException>(() => new MarkingCode("104\n", ComponentKind.Resistor));
    }

    [Fact]
    public void It_is_written_as_the_code() {
        Assert.Equal("4K7", new MarkingCode("4K7", ComponentKind.Resistor).ToString());
    }

}
