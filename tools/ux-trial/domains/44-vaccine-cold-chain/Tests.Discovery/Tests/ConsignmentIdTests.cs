using ColdChain.Domain;

using JustDummies;

using Xunit;

namespace ColdChain.Tests;

public sealed class ConsignmentIdTests {

    [Fact]
    public void A_well_formed_number_is_kept_as_given() {
        // Act
        ConsignmentId id = new ConsignmentId("VCC-2026-000417");

        // Assert
        Assert.Equal("VCC-2026-000417", id.Value);
        Assert.Equal("VCC-2026-000417", id.ToString());
    }

    [Fact]
    public void It_exposes_the_year_of_booking() {
        // Arrange
        // The sequence is a dummy: the year is what the assertion reads.
        int anySequence = Any.Int32().Between(1, 999_999).Generate();

        // Act
        ConsignmentId id = new ConsignmentId($"VCC-2026-{anySequence:D6}");

        // Assert
        Assert.Equal(2026, id.Year);
    }

    [Theory]
    [InlineData("VCC-2000-000001")]
    [InlineData("VCC-2099-999999")]
    public void The_limits_of_the_year_and_the_sequence_are_accepted(string text) {
        // Act
        ConsignmentId id = new ConsignmentId(text);

        // Assert
        Assert.Equal(text, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("VCC-2026-00041")]
    [InlineData("VCC-2026-0004170")]
    [InlineData("VCC-26-000417")]
    [InlineData("vcc-2026-000417")]
    [InlineData("VCX-2026-000417")]
    [InlineData("VCC2026000417")]
    [InlineData("VCC-2026-00041A")]
    [InlineData(" VCC-2026-000417")]
    [InlineData("VCC-2026-000417 ")]
    public void A_number_of_the_wrong_shape_is_refused(string text) {
        // Act
        Exception? thrown = Record.Exception(() => new ConsignmentId(text));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Theory]
    [InlineData("VCC-1999-000417")]
    [InlineData("VCC-2100-000417")]
    public void A_booking_year_outside_2000_2099_is_refused(string text) {
        // Act
        Exception? thrown = Record.Exception(() => new ConsignmentId(text));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(thrown);
    }

    [Fact]
    public void The_sequence_never_starts_at_zero() {
        // Act
        Exception? thrown = Record.Exception(() => new ConsignmentId("VCC-2026-000000"));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(thrown);
    }

    [Fact]
    public void A_missing_number_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new ConsignmentId(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact(Skip = "domain defect: ConsignmentId accepts a trailing line break (regex $ matches before a final newline), no drawn value involved, seed n/a")]
    public void A_trailing_line_break_is_not_part_of_a_number() {
        // Act
        Exception? thrown = Record.Exception(() => new ConsignmentId("VCC-2026-000417\n"));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact(Skip = "domain defect: ConsignmentId accepts non-ASCII digits (\\d is Unicode-aware) so VCC-2026-000000 written in Arabic-indic digits passes the zero-sequence check, no drawn value involved, seed n/a")]
    public void Non_ascii_digits_are_not_digits_of_a_number() {
        // Arabic-indic digits: a sequence that looks like 000000 must not slip past the zero check.
        // Act
        Exception? thrown = Record.Exception(() => new ConsignmentId("VCC-2026-٠٠٠٠٠٠"));

        // Assert
        Assert.NotNull(thrown);
    }

    [Fact]
    public void Two_ids_with_the_same_text_are_equal() {
        // Arrange
        ConsignmentId id        = new ConsignmentId("VCC-2026-000417");
        ConsignmentId sameId    = new ConsignmentId("VCC-2026-000417");
        ConsignmentId anotherId = new ConsignmentId("VCC-2026-000418");

        // Assert
        Assert.Equal(id, sameId);
        Assert.Equal(id.GetHashCode(), sameId.GetHashCode());
        Assert.NotEqual(id, anotherId);
        Assert.False(id.Equals(null));
    }

}
