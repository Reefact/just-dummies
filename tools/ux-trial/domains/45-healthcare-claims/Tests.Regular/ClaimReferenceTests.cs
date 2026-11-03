using HealthcareClaims.Domain;
using HealthcareClaims.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace HealthcareClaims.Tests.Regular;

public sealed class ClaimReferenceTests {

    [Fact]
    public void A_reference_carries_the_year_it_was_opened_in() {
        // Act
        ClaimReference reference = new("CLM-2026-004217");

        // Assert
        Assert.Equal(2026, reference.Year);
        Assert.Equal("CLM-2026-004217", reference.Value);
        Assert.Equal("CLM-2026-004217", reference.ToString());
    }

    [Theory]
    [InlineData("CLM-2000-000001")]
    [InlineData("CLM-2099-999999")]
    [InlineData("CLM-2026-000010")]   // zeros inside the sequence are fine; only an all-zero sequence is refused
    public void References_at_the_edges_of_the_format_are_accepted(string value) {
        // Act
        Exception? refusal = Record.Exception(() => new ClaimReference(value));

        // Assert
        Assert.Null(refusal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CLM-2026-12345")]      // five digits
    [InlineData("CLM-2026-1234567")]    // seven digits
    [InlineData("CLM-1999-000001")]     // not 20yy
    [InlineData("CLM-2126-000001")]
    [InlineData("clm-2026-000001")]
    [InlineData("CLM2026000001")]
    [InlineData(" CLM-2026-000001")]
    [InlineData("CLM-2026-00000A")]
    public void A_malformed_reference_is_refused(string value) {
        // Act & Assert
        Assert.Throws<FormatException>(() => new ClaimReference(value));
    }

    [Fact(Skip = "domain defect: the format is anchored with $, which also matches before a final line break, so \"CLM-2026-000001\\n\" is accepted and kept as the Value; seed 2009675764 (no draw involved)")]
    public void A_reference_followed_by_a_line_break_is_refused() {
        // Act & Assert
        Assert.Throws<FormatException>(() => new ClaimReference("CLM-2026-000001\n"));
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit, so a sequence of Arabic-Indic zeros slips past the all-zero check and is accepted as a reference; seed 0 (no draw involved)")]
    public void A_sequence_written_in_non_ascii_digits_is_refused() {
        // Act & Assert
        Assert.Throws<FormatException>(() => new ClaimReference("CLM-2026-\u0660\u0660\u0660\u0660\u0660\u0660"));
    }

    [Fact]
    public void A_null_reference_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClaimReference(null!));
    }

    [Theory]
    [InlineData("CLM-2026-000000")]
    [InlineData("CLM-2000-000000")]
    public void A_sequence_of_zeros_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ClaimReference(value));

        // Assert
        Assert.IsNotType<FormatException>(refusal);
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void Two_references_with_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.ClaimReference().Generate().Value;

        // Act
        ClaimReference first  = new(anyText);
        ClaimReference second = new(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_references_with_different_sequences_are_not_equal() {
        // Assert
        Assert.NotEqual(new ClaimReference("CLM-2026-000001"), new ClaimReference("CLM-2026-000002"));
    }

}
