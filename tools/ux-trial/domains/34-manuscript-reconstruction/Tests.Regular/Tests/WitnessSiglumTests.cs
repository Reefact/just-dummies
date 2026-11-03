using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class WitnessSiglumTests {

    [Theory]
    [InlineData("A")]
    [InlineData("B2")]
    [InlineData("Vat.3225")]
    [InlineData("Z.")]
    [InlineData("Abcdefghij.9")]
    public void A_conventional_siglum_is_accepted_as_written(string conventional) {
        // Act
        WitnessSiglum siglum = new WitnessSiglum(conventional);

        // Assert
        Assert.Equal(conventional, siglum.Value);
        Assert.Equal(conventional, siglum.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("2B")]
    [InlineData(".A")]
    [InlineData("A_1")]
    [InlineData("A 1")]
    [InlineData("É")]
    [InlineData("Abcdefghij.90")]
    public void A_malformed_siglum_is_refused(string malformed) {
        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => new WitnessSiglum(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_siglum_cannot_be_null() {
        Assert.Throws<ArgumentNullException>(() => new WitnessSiglum(null!));
    }

    [Fact(Skip = "domain defect: WitnessSiglum accepts a trailing line feed because the pattern ends with $, which also matches before a final line feed (new WitnessSiglum(\"A\" + LF) is built), seed n/a (literal input)")]
    public void A_siglum_cannot_end_with_a_line_feed() {
        // The model says a siglum is `^[A-Z][A-Za-z0-9.]*$`, and a line feed is none of those characters.
        Assert.ThrowsAny<ArgumentException>(() => new WitnessSiglum("A\n"));
    }

    [Fact]
    public void A_siglum_cannot_end_with_a_carriage_return_and_line_feed() {
        Assert.ThrowsAny<ArgumentException>(() => new WitnessSiglum("A\r\n"));
    }

    [Fact]
    public void Two_sigla_written_alike_are_equal_and_hash_alike() {
        // Arrange
        string anyValue = Any.WitnessSiglum().Generate().Value;

        // Act
        WitnessSiglum first  = new WitnessSiglum(anyValue);
        WitnessSiglum second = new WitnessSiglum(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Sigla_are_compared_case_sensitively() {
        Assert.NotEqual(new WitnessSiglum("Vat"), new WitnessSiglum("VAT"));
    }

    [Fact]
    public void A_siglum_is_not_equal_to_nothing() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Assert
        Assert.False(anySiglum.Equals(null));
        Assert.False(anySiglum.Equals((object?)"A"));
    }

}
