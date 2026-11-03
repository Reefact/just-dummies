using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class CallNumberTests {

    [Fact]
    public void A_call_number_exposes_its_class_number_and_its_cutter() {
        // Act
        CallNumber callNumber = new CallNumber("823.914 R884");

        // Assert
        Assert.Equal("823.914 R884", callNumber.Value);
        Assert.Equal(823.914m, callNumber.ClassNumber);
        Assert.Equal('R', callNumber.CutterLetter);
        Assert.Equal(0.884m, callNumber.CutterFraction);
    }

    [Theory]
    [InlineData("005 A1")]
    [InlineData("000 Z9999")]
    [InlineData("999.99999999 A1234")]
    [InlineData("100.1 B5")]
    [InlineData("500 C0")]
    public void A_call_number_within_the_pattern_is_accepted(string text) {
        Assert.Equal(text, new CallNumber(text).Value);
    }

    [Theory]
    [InlineData("82 R1")]
    [InlineData("8234 R1")]
    [InlineData("823.914R884")]
    [InlineData("823.914  R884")]
    [InlineData("823.914 r884")]
    [InlineData("823. R884")]
    [InlineData("823.123456789 R1")]
    [InlineData("823 R12345")]
    [InlineData("823 R")]
    [InlineData("823 1R")]
    [InlineData("823 RR1")]
    [InlineData("823,914 R884")]
    [InlineData("823.914 R884 ")]
    [InlineData(" 823.914 R884")]
    [InlineData("823.914 R884\n")]
    [InlineData("")]
    [InlineData("8２3 R1")]
    public void A_call_number_that_leaves_the_pattern_is_refused(string text) {
        Assert.Throws<FormatException>(() => new CallNumber(text));
    }

    [Fact]
    public void A_null_call_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CallNumber(null!));
    }

    [Theory]
    [InlineData("100 A1", "200 A1")]
    [InlineData("823.9 R1", "823.914 R1")]
    [InlineData("823 R1", "823.1 R1")]
    [InlineData("823.5 A1", "823.50001 A1")]
    [InlineData("823 A9", "823 B1")]
    [InlineData("823 R884", "823 R89")]
    [InlineData("823 R1", "823 R2")]
    [InlineData("823 R12", "823 R2")]
    public void Call_numbers_are_in_shelf_order(string before, string after) {
        // Arrange
        CallNumber first = new CallNumber(before);
        CallNumber second = new CallNumber(after);

        // Act & Assert
        Assert.True(first.CompareTo(second) < 0, $"{before} should sort before {after}");
        Assert.True(second.CompareTo(first) > 0, $"{after} should sort after {before}");
    }

    [Fact]
    public void Equal_class_number_and_cutter_fraction_fall_back_to_the_text() {
        // Arrange
        CallNumber short5 = new CallNumber("823 R5");
        CallNumber long50 = new CallNumber("823 R50");

        // Act & Assert
        Assert.True(short5.CompareTo(long50) < 0);
        Assert.True(long50.CompareTo(short5) > 0);
        Assert.NotEqual(short5, long50);
    }

    [Fact]
    public void A_class_number_with_a_zero_decimal_part_is_numerically_equal_but_textually_different() {
        // Arrange
        CallNumber plain = new CallNumber("823 R1");
        CallNumber padded = new CallNumber("823.0 R1");

        // Assert
        Assert.Equal(plain.ClassNumber, padded.ClassNumber);
        Assert.NotEqual(plain, padded);
        Assert.NotEqual(0, plain.CompareTo(padded));
    }

    [Fact]
    public void A_call_number_compares_equal_to_itself_and_after_null() {
        // Arrange
        CallNumber callNumber = new CallNumber("823.914 R884");

        // Assert
        Assert.Equal(0, callNumber.CompareTo(new CallNumber("823.914 R884")));
        Assert.True(callNumber.CompareTo(null) > 0);
    }

    [Fact]
    public void A_list_of_call_numbers_sorts_in_shelf_order() {
        // Arrange
        List<CallNumber> shelf = new[] { "823.914 R89", "823.914 R884", "100 B1", "823.91 R9", "823.914 A9" }.Select(text => new CallNumber(text)).ToList();

        // Act
        shelf.Sort();

        // Assert
        Assert.Equal(["100 B1", "823.91 R9", "823.914 A9", "823.914 R884", "823.914 R89"], shelf.Select(c => c.Value));
    }

}
