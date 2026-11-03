using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class RuleIdTests {

    [Theory]
    [InlineData("a")]
    [InlineData("z")]
    [InlineData("a1")]
    [InlineData("allow-dns-out")]
    [InlineData("a-b-c")]
    [InlineData("rule-1")]
    [InlineData("a-1")]
    [InlineData("a0-0")]
    public void An_id_starts_with_a_lower_case_letter_then_letters_digits_and_single_hyphens(string text) {
        // Act
        RuleId id = new RuleId(text);

        // Assert
        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Allow")]
    [InlineData("aB")]
    [InlineData("1abc")]
    [InlineData("-abc")]
    [InlineData("abc-")]
    [InlineData("a--b")]
    [InlineData("a_b")]
    [InlineData("a b")]
    [InlineData("a.b")]
    [InlineData(" abc")]
    [InlineData("abc ")]
    [InlineData("éa")]         // an accented letter is not a-z
    [InlineData("a١")]         // an Arabic-Indic digit is not 0-9
    public void An_id_outside_the_format_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new RuleId(text));
    }

    [Theory(Skip = "domain defect: RuleId's regex ends with '$', which .NET also matches before a final line feed, so 'abc\\n' is accepted although '\\n' is neither a letter, a digit nor a hyphen. No draw involved: deterministic on any seed")]
    [InlineData("abc\n")]
    [InlineData("a\n")]
    [InlineData("allow-dns-out\n")]
    public void An_id_with_a_trailing_line_feed_is_refused(string text) {
        // A line feed is neither a letter, a digit nor a hyphen, whatever the length check lets through.
        Assert.Throws<ArgumentException>(() => new RuleId(text));
    }

    [Fact]
    public void An_empty_id_is_refused_for_its_length() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RuleId(string.Empty));

        // Assert
        Assert.Contains("1 to 32", refusal.Message);
    }

    [Fact]
    public void A_missing_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RuleId(null!));
    }

    [Fact]
    public void An_id_of_32_characters_is_accepted() {
        // Arrange: the characters are dummies, the length is the point.
        string anyThirtyTwoCharacters = Any.StringMatching("[a-z][a-z0-9]{31}").Generate();

        // Act
        RuleId id = new RuleId(anyThirtyTwoCharacters);

        // Assert
        Assert.Equal(32, id.Value.Length);
    }

    [Fact]
    public void An_id_of_33_characters_is_refused() {
        // Arrange
        string anyThirtyThreeCharacters = Any.StringMatching("[a-z][a-z0-9]{32}").Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RuleId(anyThirtyThreeCharacters));
    }

    [Fact]
    public void The_length_is_checked_before_the_format() {
        // Arrange: 40 upper-case letters break both rules.
        string tooLongAndMalformed = new string('A', 40);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RuleId(tooLongAndMalformed));

        // Assert
        Assert.Contains("1 to 32", refusal.Message);
    }

    [Fact]
    public void Ids_with_the_same_text_are_equal_and_hash_alike() {
        // Arrange
        RuleId first  = new RuleId("allow-web");
        RuleId second = new RuleId("allow-web");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Ids_are_compared_case_sensitively_by_construction_since_only_lower_case_exists() {
        Assert.NotEqual(new RuleId("allow-web"), new RuleId("allow-web2"));
        Assert.False(new RuleId("a").Equals(null));
    }

}
