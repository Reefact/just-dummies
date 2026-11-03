using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class CallNumberTests {

    [Fact]
    public void A_call_number_is_read_as_a_class_number_and_a_cutter() {
        // Act
        CallNumber callNumber = new CallNumber("823.914 R884");

        // Assert
        Assert.Equal(823.914m, callNumber.ClassNumber);
        Assert.Equal('R', callNumber.CutterLetter);
        Assert.Equal(0.884m, callNumber.CutterFraction);
        Assert.Equal("823.914 R884", callNumber.Value);
    }

    [Theory]
    [InlineData("100 A1", 100)]
    [InlineData("999.99999999 Z9999", 999.99999999)]
    [InlineData("005.1 B2", 5.1)]
    public void The_class_number_has_three_digits_and_up_to_eight_decimals(string written, double expected) {
        Assert.Equal((decimal)expected, new CallNumber(written).ClassNumber);
    }

    [Fact]
    public void A_call_number_is_required() {
        Assert.Throws<ArgumentNullException>(() => new CallNumber(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("82 R884")]
    [InlineData("8234 R884")]
    [InlineData("823. R884")]
    [InlineData("823.123456789 R1")]
    [InlineData("823.914R884")]
    [InlineData("823.914  R884")]
    [InlineData("823.914 r884")]
    [InlineData("823.914 R")]
    [InlineData("823.914 R12345")]
    [InlineData("823.914 RR1")]
    [InlineData("823,914 R884")]
    [InlineData(" 823.914 R884")]
    [InlineData("823.914 R884 ")]
    [InlineData("823.914 R884\n")]
    [InlineData("８２３.９１４ R884")]
    [InlineData("823.914 É884")]
    public void A_malformed_call_number_is_refused(string malformed) {
        Assert.Throws<FormatException>(() => new CallNumber(malformed));
    }

    [Fact]
    public void Call_numbers_file_by_class_number_first() {
        // Arrange
        CallNumber early = new CallNumber("100 Z9");
        CallNumber late  = new CallNumber("823.914 A1");

        // Assert
        Assert.True(early.CompareTo(late) < 0);
        Assert.True(late.CompareTo(early) > 0);
    }

    [Fact]
    public void The_class_number_is_compared_as_a_decimal_not_as_text() {
        // Arrange
        CallNumber whole   = new CallNumber("823 R1");
        CallNumber decimalPart = new CallNumber("823.1 R1");
        CallNumber longerDecimalPart = new CallNumber("823.09 R1");

        // Assert
        Assert.True(whole.CompareTo(longerDecimalPart) < 0);
        Assert.True(longerDecimalPart.CompareTo(decimalPart) < 0);
    }

    [Fact]
    public void Within_a_class_call_numbers_file_by_cutter_letter() {
        Assert.True(new CallNumber("823.914 R9").CompareTo(new CallNumber("823.914 S1")) < 0);
    }

    [Fact]
    public void Cutter_digits_are_read_as_a_decimal_fraction() {
        // Arrange
        CallNumber r884 = new CallNumber("823.914 R884");
        CallNumber r89  = new CallNumber("823.914 R89");
        CallNumber r9   = new CallNumber("823.914 R9");

        // Assert
        Assert.True(r884.CompareTo(r89) < 0);
        Assert.True(r89.CompareTo(r9) < 0);
    }

    [Fact]
    public void Equal_cutter_fractions_fall_back_to_the_text() {
        // Arrange
        CallNumber r8  = new CallNumber("823.914 R8");
        CallNumber r80 = new CallNumber("823.914 R80");

        // Assert
        Assert.True(r8.CompareTo(r80) < 0);
        Assert.NotEqual(r8, r80);
    }

    [Fact]
    public void Equal_class_numbers_written_differently_fall_back_to_the_text() {
        // Arrange
        CallNumber short914  = new CallNumber("823.914 R1");
        CallNumber padded914 = new CallNumber("823.9140 R1");

        // Assert
        Assert.True(short914.CompareTo(padded914) < 0);
    }

    [Fact]
    public void Anything_files_after_nothing() {
        Assert.True(new CallNumber("823.914 R884").CompareTo(null) > 0);
    }

    [Fact]
    public void A_call_number_compares_equal_to_itself_and_to_the_same_text() {
        Assert.Equal(0, new CallNumber("823.914 R884").CompareTo(new CallNumber("823.914 R884")));
        Assert.Equal(new CallNumber("823.914 R884"), new CallNumber("823.914 R884"));
    }

    [Fact]
    public void A_shelf_is_sorted_in_shelf_order() {
        // Arrange
        List<CallNumber> shelf = new[] { "823.914 R89", "100 A1", "823.914 R884", "823.9 Z1", "823.914 S1" }.Select(written => new CallNumber(written)).ToList();

        // Act
        shelf.Sort();

        // Assert
        Assert.Equal(new[] { "100 A1", "823.9 Z1", "823.914 R884", "823.914 R89", "823.914 S1" }, shelf.Select(callNumber => callNumber.Value));
    }

}
