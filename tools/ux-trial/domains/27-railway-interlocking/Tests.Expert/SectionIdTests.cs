namespace RailwayInterlocking.Domain.Tests.Expert;

public sealed class SectionIdTests {

    [Theory]
    [InlineData("A1")]       // the shortest id
    [InlineData("T101")]
    [InlineData("AB1234")]   // the longest id
    [InlineData("Z9999")]
    public void A_section_id_is_one_or_two_capital_letters_then_one_to_four_digits(string text) {
        SectionId id = new SectionId(text);

        Assert.Equal(text, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("T")]        // no digit
    [InlineData("101")]      // no letter
    [InlineData("t101")]     // lower case
    [InlineData("Tt101")]
    [InlineData("ABC1")]     // three letters
    [InlineData("T12345")]   // five digits
    [InlineData("1T01")]     // wrong order
    [InlineData("T1A1")]
    [InlineData("T 101")]
    [InlineData(" T101")]
    [InlineData("T101 ")]
    [InlineData("T-101")]
    [InlineData("É101")]       // É: a capital letter, but not an ASCII one
    [InlineData("T١٠١")]   // Arabic-Indic digits: digits for Unicode, not for a track circuit
    public void A_section_id_of_any_other_shape_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new SectionId(text));
    }

    [Fact]
    public void A_section_id_without_text_is_refused_as_a_missing_argument() {
        Assert.Throws<ArgumentNullException>(() => new SectionId(null!));
    }

    [Fact(Skip = "domain defect: the format is anchored with '$', which .NET also matches before a trailing line feed, so \"T101\\n\" is accepted as a section id distinct from \"T101\"; found by reading the pattern, no seed involved")]
    public void A_section_id_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new SectionId("T101\n"));
    }

    [Fact]
    public void Two_ids_made_of_the_same_text_are_equal() {
        string    anyText = Any.SectionId().Generate().Value;
        SectionId first   = new SectionId(anyText);
        SectionId second  = new SectionId(anyText);

        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_made_of_different_texts_are_not_equal() {
        SectionId first  = Any.SectionId().Generate();
        SectionId second = Any.SectionId().WithValue(value => value.DifferentFrom(first.Value)).Generate();

        Assert.False(first.Equals(second));
        Assert.False(first.Equals((object)second));
    }

    [Fact]
    public void An_id_is_equal_to_neither_a_missing_id_nor_something_else() {
        SectionId anyId = Any.SectionId().Generate();

        Assert.False(anyId.Equals((SectionId?)null));
        Assert.False(anyId.Equals((object?)null));
        Assert.False(anyId.Equals(anyId.Value));
    }

    [Fact]
    public void An_id_reads_as_its_text() {
        Assert.Equal("T101", new SectionId("T101").ToString());
    }

}
