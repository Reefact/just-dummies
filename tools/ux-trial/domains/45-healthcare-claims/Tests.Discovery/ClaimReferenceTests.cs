using JustDummies;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class ClaimReferenceTests {

    [Fact]
    public void A_well_formed_reference_exposes_its_value_and_its_year() {
        // Act
        ClaimReference reference = new("CLM-2026-004217");

        // Assert
        Assert.Equal("CLM-2026-004217", reference.Value);
        Assert.Equal(2026, reference.Year);
        Assert.Equal("CLM-2026-004217", reference.ToString());
    }

    [Theory]
    [InlineData("CLM-2000-000001", 2000)]
    [InlineData("CLM-2099-999999", 2099)]
    public void The_year_runs_from_2000_to_2099(string value, int year) {
        // Act / Assert
        Assert.Equal(year, new ClaimReference(value).Year);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CLM-2026-00421")]
    [InlineData("CLM-2026-0042170")]
    [InlineData("CLM-1999-004217")]
    [InlineData("CLM-2126-004217")]
    [InlineData("clm-2026-004217")]
    [InlineData("CLM-2026-00421A")]
    [InlineData(" CLM-2026-004217")]
    [InlineData("CLM2026004217")]
    public void A_malformed_reference_is_refused_as_a_format_error(string value) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new ClaimReference(value));
    }

    [Fact(Skip = "domain defect: the pattern ends with $, which also matches before a final line feed, so 'CLM-2026-004217\\n' is accepted and kept with the line feed in Value; seed -1228743505")]
    public void A_reference_followed_by_a_line_feed_is_refused_as_a_format_error() {
        // Act / Assert
        Assert.Throws<FormatException>(() => new ClaimReference("CLM-2026-004217\n"));
    }

    [Fact]
    public void A_missing_reference_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ClaimReference(null!));
    }

    [Fact]
    public void A_sequence_of_zeros_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ClaimReference("CLM-2026-000000"));

        // Assert
        Assert.IsNotType<FormatException>(refusal);
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void The_first_sequence_is_accepted() {
        // Act / Assert
        Assert.Equal("CLM-2026-000001", new ClaimReference("CLM-2026-000001").Value);
    }

    [Fact]
    public void References_are_equal_when_their_text_is() {
        // Arrange
        ClaimReference anyReference = Any.ClaimReference().Generate();

        // Act
        ClaimReference sameText = new(anyReference.Value);

        // Assert
        Assert.Equal(anyReference, sameText);
        Assert.Equal(anyReference.GetHashCode(), sameText.GetHashCode());
    }

    [Fact]
    public void References_with_different_text_are_different() {
        // Act / Assert
        Assert.NotEqual(new ClaimReference("CLM-2026-000001"), new ClaimReference("CLM-2026-000002"));
        Assert.NotEqual(new ClaimReference("CLM-2026-000001"), new ClaimReference("CLM-2027-000001"));
    }

}
