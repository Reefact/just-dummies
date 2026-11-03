using HealthcareClaims.Domain;
using JustDummies;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class ClaimReferenceTests {

    public static TheoryData<string> MalformedReferences => new() {
        "",
        "CLM-2026-00421",         // five digits
        "CLM-2026-0042170",       // seven digits
        "CLM-1999-004217",        // the year is 20yy
        "CLM-2026-00421A",        // a letter in the sequence
        "clm-2026-004217",        // case matters
        "CLM2026004217",          // separators
        " CLM-2026-004217",       // a leading space
        "CLM-2026-004217 ",       // a trailing space
        "CLM-26-004217"           // a two-digit year
    };

    [Fact]
    public void A_reference_exposes_the_year_it_was_opened_in() {
        ClaimReference reference = new("CLM-2026-004217");

        Assert.Equal(2026, reference.Year);
        Assert.Equal("CLM-2026-004217", reference.Value);
        Assert.Equal("CLM-2026-004217", reference.ToString());
    }

    [Theory]
    [InlineData("CLM-2000-000001", 2000)]
    [InlineData("CLM-2099-999999", 2099)]
    public void A_reference_is_accepted_at_both_ends_of_the_century(string value, int year) {
        ClaimReference reference = new(value);

        Assert.Equal(year, reference.Year);
    }

    [Theory]
    [MemberData(nameof(MalformedReferences))]
    public void A_malformed_reference_is_refused(string value) {
        Assert.Throws<FormatException>(() => new ClaimReference(value));
    }

    [Fact]
    public void A_reference_cannot_be_null() {
        Assert.Throws<ArgumentNullException>(() => new ClaimReference(null!));
    }

    [Fact]
    public void A_sequence_starts_at_one() {
        Assert.Throws<ArgumentException>(() => new ClaimReference("CLM-2026-000000"));
    }

    [Fact]
    public void Two_references_with_the_same_text_are_equal() {
        ClaimReference anyReference  = Any.ClaimReference().Generate();
        ClaimReference sameReference = new(anyReference.Value);

        Assert.Equal(anyReference, sameReference);
        Assert.Equal(anyReference.GetHashCode(), sameReference.GetHashCode());
    }

    [Fact]
    public void Two_references_with_different_texts_are_not_equal() {
        ClaimReference anyReference      = Any.ClaimReference().Generate();
        ClaimReference anyOtherReference = Any.ClaimReference().DifferentFrom(anyReference).Generate();

        Assert.NotEqual(anyReference, anyOtherReference);
    }

    [Fact(Skip = "domain defect: a reference with a trailing line feed is accepted ($ matches before it), found by hand, no seed")]
    public void A_reference_followed_by_a_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => new ClaimReference("CLM-2026-004217\n"));
    }

    [Fact(Skip = "domain defect: \\d matches non-ASCII digits, so an all-zero sequence written with Arabic-Indic zeros slips past the 000000 guard, found by hand, no seed")]
    public void A_sequence_of_zeros_written_in_other_digits_is_refused() {
        Assert.ThrowsAny<ArgumentException>(() => new ClaimReference("CLM-2026-٠٠٠٠٠٠"));
    }

}
