using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class PersonNameTests {

    [Theory]
    [InlineData("Dostoïevski", "Fiodor", WritingScript.Latin)]
    [InlineData("Παπαδόπουλος", "Γιώργος", WritingScript.Greek)]
    [InlineData("Толстой", "Лев Николаевич", WritingScript.Cyrillic)]
    [InlineData("כהן", "דוד", WritingScript.Hebrew)]
    [InlineData("المنفلوطي", "مصطفى", WritingScript.Arabic)]
    [InlineData("प्रेमचंद", "मुंशी", WritingScript.Devanagari)]
    [InlineData("夏目", "漱石", WritingScript.Cjk)]
    [InlineData("ホシ", "シンイチ", WritingScript.Cjk)]
    public void A_name_takes_the_script_of_its_letters(string family, string given, WritingScript expected) {
        // Act
        PersonName name = new PersonName(family, given);

        // Assert
        Assert.Equal(expected, name.Script);
    }

    [Fact]
    public void A_name_without_given_names_has_the_family_name_as_its_heading() {
        // Act
        PersonName name = new PersonName("Homère");

        // Assert
        Assert.Null(name.GivenNames);
        Assert.Equal("Homère", name.Heading);
        Assert.Equal("Homère", name.ToString());
    }

    [Fact]
    public void The_heading_of_a_name_puts_the_family_name_first_after_a_comma() {
        // Act
        PersonName name = new PersonName("Dostoïevski", "Fiodor Mikhaïlovitch");

        // Assert
        Assert.Equal("Dostoïevski, Fiodor Mikhaïlovitch", name.Heading);
    }

    [Fact]
    public void The_heading_of_a_cjk_name_has_no_separator() {
        // Act
        PersonName name = new PersonName("夏目", "漱石");

        // Assert
        Assert.Equal("夏目漱石", name.Heading);
    }

    [Fact]
    public void A_name_trims_and_normalises_each_part() {
        // Act
        PersonName name = new PersonName("  Café  ", " Zoë\t");

        // Assert
        Assert.Equal("Café", name.FamilyName);
        Assert.Equal("Zoë", name.GivenNames);
    }

    [Theory]
    [InlineData("O'Brien")]
    [InlineData("O’Brien")]
    [InlineData("Saint-Exupéry")]
    [InlineData("Saint‐Exupéry")]
    [InlineData("Jean Pierre")]
    [InlineData("J. R. R.")]
    [InlineData("ル・グウィン")]
    public void A_name_accepts_the_punctuation_of_real_names(string family) {
        // Act
        PersonName name = new PersonName(family);

        // Assert
        Assert.Equal(family, name.FamilyName);
    }

    [Theory]
    [InlineData("Smith2")]
    [InlineData("Smith_")]
    [InlineData("Smith@")]
    [InlineData("Smith,Jr")]
    [InlineData("Smith Jr")]
    [InlineData("Smith\tJr")]
    [InlineData("Smith/Jones")]
    [InlineData("Smith‍Jones")]
    [InlineData("Smith\U0001F600")]
    public void A_name_refuses_a_character_that_is_not_a_letter_a_mark_or_a_name_separator(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Theory]
    [InlineData("Smith  Jones")]
    [InlineData("A    B")]
    public void A_name_refuses_two_consecutive_spaces(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData("'")]
    [InlineData(". .")]
    public void A_name_part_needs_at_least_one_letter(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Theory]
    [InlineData("́Smith")]
    [InlineData("̈a")]
    public void A_name_part_cannot_start_with_a_combining_mark(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Fact]
    public void An_empty_given_name_is_refused_rather_than_read_as_absent() {
        // Arrange
        string anyFamilyName = Scripts.FamilyNameText(WritingScript.Latin).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PersonName(anyFamilyName, ""));
        Assert.Throws<ArgumentException>(() => new PersonName(anyFamilyName, "  "));
    }

    [Fact]
    public void A_null_family_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new PersonName(null!));
    }

    [Theory]
    [InlineData("สมชาย")]
    [InlineData("ბერიძე")]
    [InlineData("Smith สมชาย")]
    public void A_name_refuses_letters_of_a_script_the_catalogue_does_not_support(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Theory]
    [InlineData("SmithСмит")]
    [InlineData("Ελληνικά Latin")]
    [InlineData("書き方kaki")]
    [InlineData("שלוםمرحبا")]
    public void A_name_part_cannot_mix_letters_of_two_scripts(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Fact]
    public void A_name_part_may_join_words_of_one_script_with_a_hyphen() {
        // Act
        PersonName name = new PersonName("Ben-Gurion", "David");

        // Assert
        Assert.Equal(WritingScript.Latin, name.Script);
    }

    [Theory]
    [InlineData("Smith", "Иван")]
    [InlineData("Смит", "John")]
    [InlineData("山田", "Taro")]
    [InlineData("כהן", "David")]
    public void Given_names_must_be_written_in_the_script_of_the_family_name(string family, string given) {
        Assert.Throws<ArgumentException>(() => new PersonName(family, given));
    }

    [Fact]
    public void A_name_part_may_hold_the_longest_length() {
        // Arrange
        string anyLetter = Scripts.Letter(WritingScript.Greek).Generate();
        string longestPart = string.Concat(Enumerable.Repeat(anyLetter, PersonName.MaxPartLength));

        // Act
        PersonName name = new PersonName(longestPart, longestPart);

        // Assert
        Assert.Equal(100, name.FamilyName.Length);
        Assert.Equal(100, name.GivenNames!.Length);
    }

    [Fact]
    public void A_family_name_longer_than_the_maximum_is_refused() {
        // Arrange
        string anyLetter = Scripts.Letter(WritingScript.Greek).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PersonName(string.Concat(Enumerable.Repeat(anyLetter, 101))));

        // Assert
        Assert.Equal("familyName", error.ParamName);
    }

    [Fact]
    public void Given_names_longer_than_the_maximum_are_refused() {
        // Arrange
        string anyLetter = Scripts.Letter(WritingScript.Greek).Generate();
        string anyFamilyName = Scripts.FamilyNameText(WritingScript.Greek).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PersonName(anyFamilyName, string.Concat(Enumerable.Repeat(anyLetter, 101))));

        // Assert
        Assert.Equal("givenNames", error.ParamName);
    }

    [Fact]
    public void The_length_of_a_name_part_counts_grapheme_clusters() {
        // Arrange: 100 ideographs outside the BMP are 200 code units.
        string hundredIdeographs = string.Concat(Enumerable.Repeat("\U00020000", 100));

        // Act
        PersonName name = new PersonName(hundredIdeographs);

        // Assert
        Assert.Equal(WritingScript.Cjk, name.Script);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PersonName(hundredIdeographs + "\U00020001"));
    }

    [Theory]
    [InlineData("Müller", "Hans", "MÜLLER", "hans")]
    [InlineData("Müller", "Hans", "Müller", "Hans")]
    [InlineData("Толстой", null, "ТОЛСТОЙ", null)]
    public void Two_names_are_equal_under_normalisation_and_case_folding(string family, string? given, string otherFamily, string? otherGiven) {
        // Act
        PersonName name = new PersonName(family, given);
        PersonName other = new PersonName(otherFamily, otherGiven);

        // Assert
        Assert.Equal(name, other);
        Assert.Equal(name.GetHashCode(), other.GetHashCode());
    }

    [Theory]
    [InlineData("Smith", "John", "Smith", null)]
    [InlineData("Smith", "John", "John", "Smith")]
    [InlineData("Ab", "Cd", "Abcd", null)]
    [InlineData("Ab", "Cd", "Abc", "d")]
    [InlineData("Müller", null, "Muller", null)]
    [InlineData("O'Brien", null, "O’Brien", null)]
    public void Two_names_that_differ_in_a_part_are_not_equal(string family, string? given, string otherFamily, string? otherGiven) {
        // Act
        PersonName name = new PersonName(family, given);
        PersonName other = new PersonName(otherFamily, otherGiven);

        // Assert
        Assert.NotEqual(name, other);
    }

}
