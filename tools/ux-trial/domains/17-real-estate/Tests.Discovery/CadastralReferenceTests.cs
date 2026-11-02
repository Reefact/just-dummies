using JustDummies;

using RealEstate.Domain;

using Xunit;

namespace RealEstate.Tests.Discovery;

public sealed class CadastralReferenceTests {

    [Theory]
    [InlineData("75056 000 AB 0123")]
    [InlineData("2A004 000 AB 0001")]
    [InlineData("2B033 123 0Z 9999")]
    [InlineData("97411 000 A1 0010")]
    public void A_well_formed_reference_is_accepted_and_keeps_its_text(string text) {
        // Act
        CadastralReference reference = new CadastralReference(text);

        // Assert
        Assert.Equal(text, reference.Value);
        Assert.Equal(text, reference.ToString());
    }

    [Fact]
    public void A_null_reference_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CadastralReference(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_reference_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => new CadastralReference(blank));
    }

    [Theory]
    [InlineData("75056000AB0123")]           // no separators
    [InlineData("75056  000 AB 0123")]       // double space
    [InlineData(" 75056 000 AB 0123")]       // leading space
    [InlineData("75056 000 AB 0123 ")]       // trailing space
    [InlineData("7505 000 AB 0123")]         // commune too short
    [InlineData("750560 000 AB 0123")]       // commune too long
    [InlineData("2C004 000 AB 0001")]        // Corsica is 2A or 2B only
    [InlineData("2A04 000 AB 0001")]         // Corsican commune too short
    [InlineData("75056 00 AB 0123")]         // prefix too short
    [InlineData("75056 000 ab 0123")]        // section is upper case
    [InlineData("75056 000 A 0123")]         // section too short
    [InlineData("75056 000 ABC 0123")]       // section too long
    [InlineData("75056 000 AB 123")]         // plan too short
    [InlineData("75056 000 AB 01234")]       // plan too long
    [InlineData("75056 000 AB 01A3")]        // plan is digits only
    public void A_malformed_reference_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new CadastralReference(text));
    }

    [Theory]
    [InlineData("75056 000 AB 0000")]
    [InlineData("2A004 000 AB 0000")]
    public void A_plan_number_starts_at_0001(string text) {
        Assert.Throws<ArgumentException>(() => new CadastralReference(text));
    }

    [Fact]
    public void A_plan_number_of_0001_is_accepted() {
        Assert.Equal("0001", new CadastralReference("75056 000 AB 0001").Value.Substring(13));
    }

    [Theory]
    [InlineData("75056 000 AB 0123", "75056", "AB", "75", false)]
    [InlineData("97411 000 A1 0010", "97411", "A1", "974", false)]
    [InlineData("97101 000 ZZ 0002", "97101", "ZZ", "971", false)]
    [InlineData("2A004 000 AB 0001", "2A004", "AB", "2A", true)]
    [InlineData("2B033 123 0Z 9999", "2B033", "0Z", "2B", true)]
    [InlineData("20000 000 AB 0001", "20000", "AB", "20", false)]
    [InlineData("97000 000 AB 0001", "97000", "AB", "970", false)]
    [InlineData("01001 000 AB 0001", "01001", "AB", "01", false)]
    public void A_reference_exposes_its_parts(string text, string communeCode, string section, string department, bool isInCorsica) {
        // Arrange
        CadastralReference reference = new CadastralReference(text);

        // Assert
        Assert.Equal(communeCode, reference.CommuneCode);
        Assert.Equal(section, reference.Section);
        Assert.Equal(department, reference.Department);
        Assert.Equal(isInCorsica, reference.IsInCorsica);
    }

    [Fact]
    public void References_with_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.CadastralReference().Generate().Value;

        // Act
        CadastralReference first  = new CadastralReference(anyText);
        CadastralReference second = new CadastralReference(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void References_with_different_texts_are_not_equal() {
        // Arrange
        CadastralReference first  = new CadastralReference("75056 000 AB 0123");
        CadastralReference second = new CadastralReference("75056 000 AB 0124");

        // Assert
        Assert.NotEqual(first, second);
        Assert.False(first.Equals((object?)null));
    }

    [Fact(Skip = "domain defect: the pattern ends with a dollar anchor, which also matches before a final line feed, so a reference followed by a line feed is accepted; seed-independent (literal input)")]
    public void A_trailing_line_feed_is_refused() {
        // Skip until the domain is fixed: '$' in the pattern also matches before a final '\n', so the text is accepted.
        Assert.Throws<ArgumentException>(() => new CadastralReference("75056 000 AB 0123\n"));
    }

    [Fact(Skip = "domain defect: the 0000 check looks at the end of the text, which here is the line feed, so a plan number of 0000 followed by a line feed bypasses it; seed-independent (literal input)")]
    public void A_plan_number_of_0000_cannot_hide_behind_a_trailing_line_feed() {
        // The "0000" check looks at the end of the text, which here is the line feed.
        Assert.Throws<ArgumentException>(() => new CadastralReference("75056 000 AB 0000\n"));
    }

    [Fact(Skip = "domain defect: the digit class matches any Unicode decimal digit, so Arabic-Indic digits are accepted in a reference; seed-independent (literal input)")]
    public void Digits_are_ascii_digits() {
        // '\d' in .NET also matches Arabic-Indic digits; a land registry only issues 0-9.
        Assert.Throws<ArgumentException>(() => new CadastralReference("٧٥٠٥٦ 000 AB 0123"));
    }

}
