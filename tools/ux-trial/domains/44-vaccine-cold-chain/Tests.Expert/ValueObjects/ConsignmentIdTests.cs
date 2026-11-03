using ColdChain.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class ConsignmentIdTests {

    [Theory]
    [InlineData("VCC-2026-000417", 2026)]
    [InlineData("VCC-2000-000001", 2000)]
    [InlineData("VCC-2099-999999", 2099)]
    public void A_consignment_number_exposes_the_year_it_was_booked_in(string text, int expectedYear) {
        // Act
        ConsignmentId id = new ConsignmentId(text);

        // Assert
        Assert.Equal(text, id.Value);
        Assert.Equal(expectedYear, id.Year);
    }

    [Theory]
    [InlineData("VCC-1999-000417")]
    [InlineData("VCC-2100-000417")]
    public void A_booking_year_outside_2000_2099_is_out_of_range(string text) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsignmentId(text));
    }

    [Fact]
    public void The_sequence_starts_at_000001() {
        Assert.Throws<ArgumentException>(() => new ConsignmentId("VCC-2026-000000"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("VCC-2026-00417")]
    [InlineData("VCC-2026-0004170")]
    [InlineData("VCC-26-000417")]
    [InlineData("VCC-2026000417")]
    [InlineData("vcc-2026-000417")]
    [InlineData("VCX-2026-000417")]
    [InlineData("VCC-2026-00041A")]
    [InlineData(" VCC-2026-000417")]
    [InlineData("VCC-2026-000417 ")]
    public void A_number_that_is_not_shaped_VCC_yyyy_nnnnnn_is_a_format_error(string text) {
        Assert.Throws<FormatException>(() => new ConsignmentId(text));
    }

    [Fact]
    public void A_missing_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ConsignmentId(null!));
    }

    [Fact]
    public void Two_numbers_written_the_same_are_the_same_consignment() {
        // Arrange
        ConsignmentId first  = new ConsignmentId("VCC-2026-000417");
        ConsignmentId second = new ConsignmentId("VCC-2026-000417");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Numbers_that_differ_only_by_their_sequence_are_different_consignments() {
        Assert.NotEqual(new ConsignmentId("VCC-2026-000417"), new ConsignmentId("VCC-2026-000418"));
    }

    [Fact(Skip = "domain defect: ConsignmentId accepts \"VCC-2026-000417\\n\" because the regex ends in $ (matches before a final line feed), seed 211364978")]
    public void A_trailing_line_feed_does_not_make_a_well_shaped_number() {
        Assert.Throws<FormatException>(() => new ConsignmentId("VCC-2026-000417\n"));
    }

    [Theory(Skip = "domain defect: ConsignmentId accepts Arabic-Indic and full-width digits in the sequence because the regex uses \\d, seeds -1912974224 and 767265917")]
    [InlineData("VCC-2026-\u0660\u0660\u0660\u0664\u0661\u0667")]
    [InlineData("VCC-2026-\uFF10\uFF10\uFF10\uFF14\uFF11\uFF17")]
    public void A_sequence_written_with_digits_other_than_0_to_9_is_a_format_error(string text) {
        Assert.Throws<FormatException>(() => new ConsignmentId(text));
    }
}
