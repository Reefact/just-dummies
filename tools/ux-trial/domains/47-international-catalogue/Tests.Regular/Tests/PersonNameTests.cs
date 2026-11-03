using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class PersonNameTests {

    [Fact]
    public void The_heading_of_a_Latin_name_is_family_comma_given() {
        // Act
        PersonName name = new PersonName("Müller", "Anne-Marie");

        // Assert
        Assert.Equal("Müller, Anne-Marie", name.Heading);
        Assert.Equal("Müller, Anne-Marie", name.ToString());
        Assert.Equal(WritingScript.Latin, name.Script);
    }

    [Fact]
    public void The_heading_of_a_CJK_name_has_no_separator() {
        // Act
        PersonName name = new PersonName("村上", "春樹");

        // Assert
        Assert.Equal("村上春樹", name.Heading);
        Assert.Equal(WritingScript.Cjk, name.Script);
    }

    [Fact]
    public void Without_given_names_the_heading_is_the_family_name() {
        // Arrange
        // Act
        PersonName name = new PersonName("Толстой");

        // Assert
        Assert.Null(name.GivenNames);
        Assert.Equal("Толстой", name.Heading);
    }

    [Theory]
    [InlineData("Толстой", "Лев", WritingScript.Cyrillic)]
    [InlineData("Παπαδόπουλος", "Γεώργιος", WritingScript.Greek)]
    [InlineData("כהן", "דוד", WritingScript.Hebrew)]
    [InlineData("الخوارزمي", "محمد", WritingScript.Arabic)]
    [InlineData("शर\u094Dम\u093E", "र\u093Eज", WritingScript.Devanagari)]
    [InlineData("김", "민수", WritingScript.Cjk)]
    [InlineData("ジョン・スミス", "太郎", WritingScript.Cjk)]
    public void A_name_takes_the_script_of_its_letters(string family, string given, WritingScript expected) {
        // Act
        PersonName name = new PersonName(family, given);

        // Assert
        Assert.Equal(expected, name.Script);
    }

    [Fact]
    public void A_name_is_stored_in_NFC_and_trimmed() {
        // Act
        PersonName name = new PersonName("  Mu\u0308ller ", " Zoe\u0308\t");

        // Assert
        Assert.Equal("Müller", name.FamilyName);
        Assert.Equal("Zoë", name.GivenNames);
    }

    [Theory]
    [InlineData("O'Brien")]
    [InlineData("N’Diaye")]
    [InlineData("Jean-Pierre")]
    [InlineData("Wolf‐Hall")]
    [InlineData("St. John")]
    [InlineData("de la Cruz")]
    public void Apostrophes_hyphens_periods_and_single_spaces_are_allowed(string family) {
        // Arrange
        string anyGivenNames = Any.PersonName().InScript(WritingScript.Latin).Generate().FamilyName;

        // Act
        PersonName name = new PersonName(family, anyGivenNames);

        // Assert
        Assert.Equal(family, name.FamilyName);
    }

    [Theory]
    [InlineData("Smith2")]
    [InlineData("Smith!")]
    [InlineData("snake_case")]
    [InlineData("a/b")]
    [InlineData("a,b")]
    [InlineData("1234")]
    [InlineData("...")]
    [InlineData("-")]
    [InlineData("'")]
    public void A_character_outside_letters_marks_and_the_few_separators_is_refused_and_a_letter_is_required(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Theory]
    [InlineData("Jean  Pierre")]
    [InlineData("Jean   Pierre")]
    public void Two_consecutive_spaces_are_refused(string family) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PersonName(family));

        // Assert
        Assert.Contains("consecutive", refusal.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_or_blank_name_part_is_refused_for_the_family_and_for_the_given_names(string part) {
        // Arrange
        string anyFamily = Any.PersonName().InScript(WritingScript.Latin).Generate().FamilyName;

        // Act & Assert
        Assert.Equal("familyName", Assert.Throws<ArgumentException>(() => new PersonName(part)).ParamName);
        Assert.Equal("givenNames", Assert.Throws<ArgumentException>(() => new PersonName(anyFamily, part)).ParamName);
    }

    [Fact]
    public void A_null_family_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new PersonName(null!));
    }

    [Theory]
    [InlineData("\u0301Müller")]
    [InlineData("\u0903कर")]
    public void A_name_part_cannot_start_with_a_combining_mark(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Fact]
    public void An_unpaired_surrogate_is_refused() {
        Assert.Throws<ArgumentException>(() => new PersonName("Smith\uD800"));
    }

    [Theory]
    [InlineData("สมชาย")]   // Thai
    [InlineData("გიორგი")]  // Georgian
    [InlineData("Ａｂ")]    // fullwidth Latin
    public void A_letter_of_an_unsupported_script_is_refused(string family) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PersonName(family));

        // Assert
        Assert.Contains("no supported script", refusal.Message);
    }

    [Theory]
    [InlineData("SmithСмит")]
    [InlineData("Smith Смит")]
    [InlineData("Ελλάς A")]
    public void A_part_mixing_two_scripts_is_refused(string family) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PersonName(family));

        // Assert
        Assert.Contains("mixes", refusal.Message);
    }

    [Theory]
    [InlineData("Smith", "Смит")]
    [InlineData("Смит", "John")]
    [InlineData("村上", "Haruki")]
    [InlineData("שלום", "سلام")]
    public void Given_names_must_be_in_the_script_of_the_family_name(string family, string given) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PersonName(family, given));

        // Assert
        Assert.Equal("givenNames", refusal.ParamName);
    }

    [Fact]
    public void A_part_of_100_clusters_is_accepted_and_101_is_refused() {
        // Arrange
        string hundred = new string('é', 100);

        // Act
        PersonName accepted = new PersonName(hundred);
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PersonName(hundred + "a"));

        // Assert
        Assert.Equal(PersonName.MaxPartLength, accepted.FamilyName.Length);
        Assert.Equal("familyName", refusal.ParamName);
    }

    [Fact]
    public void The_length_limit_counts_clusters_for_the_given_names_too() {
        // Arrange
        string anyFamily = Any.PersonName().InScript(WritingScript.Latin).Generate().FamilyName;
        string decomposed100 = string.Concat(Enumerable.Repeat("x\u0301", 100));
        string decomposed101 = string.Concat(Enumerable.Repeat("x\u0301", 101));

        // Act
        PersonName accepted = new PersonName(anyFamily, decomposed100);
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PersonName(anyFamily, decomposed101));

        // Assert
        Assert.Equal(200, accepted.GivenNames!.Length);
        Assert.Equal("givenNames", refusal.ParamName);
    }

    [Fact]
    public void Names_are_equal_under_NFC_and_case_folding() {
        // Assert
        Assert.Equal(new PersonName("Müller", "Anne"), new PersonName("MÜLLER", "anne"));
        Assert.Equal(new PersonName("Mu\u0308ller"), new PersonName("Müller"));
        Assert.Equal(new PersonName("Müller", "Anne").GetHashCode(), new PersonName("MÜLLER", "ANNE").GetHashCode());
    }

    [Fact]
    public void The_given_names_take_part_in_equality() {
        // Arrange
        PersonName family   = new PersonName("Dupont");
        PersonName withJean = new PersonName("Dupont", "Jean");
        PersonName withLuc  = new PersonName("Dupont", "Luc");

        // Assert
        Assert.NotEqual(family, withJean);
        Assert.NotEqual(withJean, withLuc);
    }

    [Fact]
    public void A_family_name_and_given_names_cannot_be_confused_with_one_longer_family_name() {
        // Assert
        Assert.NotEqual(new PersonName("Ann Bo"), new PersonName("Ann", "Bo"));
    }

    [Fact]
    public void An_accented_letter_is_not_its_bare_letter() {
        Assert.NotEqual(new PersonName("Muller"), new PersonName("Müller"));
    }

}
