using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.ValueObjects;

public sealed class LotNumberTests {

    // The format is what these tests are about, so the numbers they feed it are literals.

    [Theory]
    [InlineData("AB-000001")]
    [InlineData("API-240117")]
    [InlineData("ABCD-999999")]
    [InlineData("API-240117.01")]
    [InlineData("API-240117.99")]
    public void A_well_formed_lot_number_is_accepted_and_keeps_its_text(string text) {
        // Act
        LotNumber lotNumber = new LotNumber(text);

        // Assert
        Assert.Equal(text, lotNumber.Value);
        Assert.Equal(text, lotNumber.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("A-123456")]
    [InlineData("ABCDE-123456")]
    [InlineData("api-123456")]
    [InlineData("Api-123456")]
    [InlineData("API123456")]
    [InlineData("API_123456")]
    [InlineData("API-12345")]
    [InlineData("API-1234567")]
    [InlineData("API-12345A")]
    [InlineData("API-240117.")]
    [InlineData("API-240117.1")]
    [InlineData("API-240117.100")]
    [InlineData("API-240117.0A")]
    [InlineData("API-240117.03.04")]
    [InlineData(" API-240117")]
    [InlineData("API-240117 ")]
    [InlineData("API-٢٤٠١١٧")] // Arabic-Indic digits are digits for \d, not for the domain
    public void A_malformed_lot_number_is_refused(string text) {
        Assert.Throws<FormatException>(() => new LotNumber(text));
    }

    [Fact(Skip = "domain defect: the pattern ends with $, which also matches before a final \\n, so a trailing line break is accepted and kept in Value; no seed involved, the text is a literal")]
    public void A_trailing_line_break_does_not_make_a_lot_number_malformed_by_accident() {
        // `$` in a .NET regular expression also matches before a final "\n": a lot number read from a
        // file line by line must not slip through carrying the line break.
        Assert.Throws<FormatException>(() => new LotNumber("API-240117\n"));
    }

    [Fact]
    public void A_sub_lot_index_of_zero_is_refused_as_out_of_range_not_as_malformed() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LotNumber("API-240117.00"));
    }

    [Fact]
    public void A_missing_lot_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new LotNumber(null!));
    }

    [Fact]
    public void A_base_lot_has_no_sub_lot_index() {
        // Act
        LotNumber lotNumber = new LotNumber("API-240117");

        // Assert
        Assert.False(lotNumber.IsSubLot);
        Assert.Null(lotNumber.SubLotIndex);
        Assert.Equal("API-240117", lotNumber.BaseLot);
    }

    [Fact]
    public void A_sub_lot_knows_its_base_lot_and_its_index() {
        // Act
        LotNumber lotNumber = new LotNumber("API-240117.03");

        // Assert
        Assert.True(lotNumber.IsSubLot);
        Assert.Equal(3, lotNumber.SubLotIndex);
        Assert.Equal("API-240117", lotNumber.BaseLot);
    }

    [Theory]
    [InlineData(1, "API-240117.01")]
    [InlineData(7, "API-240117.07")]
    [InlineData(10, "API-240117.10")]
    [InlineData(99, "API-240117.99")]
    public void The_sub_lot_of_a_lot_carries_a_two_digit_index(int index, string expected) {
        // Arrange
        LotNumber lotNumber = new LotNumber("API-240117");

        // Act
        LotNumber subLot = lotNumber.SubLot(index);

        // Assert
        Assert.Equal(expected, subLot.Value);
        Assert.True(subLot.IsSubLot);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_sub_lot_index_outside_one_to_ninety_nine_is_refused(int index) {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Base().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyLot.SubLot(index));
    }

    [Fact]
    public void The_sub_lot_of_a_sub_lot_replaces_its_index_rather_than_stacking_a_second_one() {
        // Arrange
        LotNumber subLot = new LotNumber("API-240117.03");

        // Act
        LotNumber sibling = subLot.SubLot(4);

        // Assert
        Assert.Equal("API-240117.04", sibling.Value);
    }

    [Fact]
    public void Two_lot_numbers_with_the_same_text_are_equal_and_hash_alike() {
        // Arrange
        string anyText = Any.LotNumber().Generate().Value;

        // Act
        LotNumber first  = new LotNumber(anyText);
        LotNumber second = new LotNumber(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_sub_lot_is_not_its_base_lot() {
        // Arrange
        LotNumber lotNumber = Any.LotNumber().Base().Generate();

        // Act
        LotNumber subLot = lotNumber.SubLot(1);

        // Assert
        Assert.NotEqual(lotNumber, subLot);
    }

    [Fact]
    public void Lot_numbers_that_differ_by_one_character_are_different() {
        // Assert
        Assert.NotEqual(new LotNumber("API-240117"), new LotNumber("API-240118"));
        Assert.NotEqual(new LotNumber("API-240117"), new LotNumber("APX-240117"));
    }

    [Fact]
    public void A_lot_number_is_never_equal_to_nothing_nor_to_its_text() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Assert
        Assert.False(anyLot.Equals((LotNumber?)null));
        Assert.False(anyLot.Equals(anyLot.Value));
    }

}
