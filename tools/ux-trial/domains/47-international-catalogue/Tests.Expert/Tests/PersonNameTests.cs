using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class PersonNameTests {

    [Theory]
    [InlineData("Tolstoï", "Léon", "Tolstoï, Léon")]
    [InlineData("Чехов", "Антон Павлович", "Чехов, Антон Павлович")]
    [InlineData("Naguib Mahfouz", null, "Naguib Mahfouz")]
    public void A_name_is_filed_under_its_family_name_then_its_given_names(string family, string? given, string heading) {
        // Act
        PersonName name = new PersonName(family, given);

        // Assert
        Assert.Equal(heading, name.Heading);
        Assert.Equal(heading, name.ToString());
    }

    [Fact]
    public void A_Cjk_name_is_filed_family_name_first_with_no_separator() {
        // Act
        PersonName name = new PersonName("夏目", "漱石");

        // Assert
        Assert.Equal("夏目漱石", name.Heading);
    }

    [Theory]
    [InlineData("Tolstoï", WritingScript.Latin)]
    [InlineData("Толстой", WritingScript.Cyrillic)]
    [InlineData("Παπαδόπουλος", WritingScript.Greek)]
    [InlineData("כהן", WritingScript.Hebrew)]
    [InlineData("محفوظ", WritingScript.Arabic)]
    [InlineData("शर\u094Dम\u093E", WritingScript.Devanagari)]
    [InlineData("夏目", WritingScript.Cjk)]
    public void A_name_is_in_the_script_of_its_letters(string family, WritingScript script) {
        Assert.Equal(script, new PersonName(family).Script);
    }

    [Fact]
    public void A_name_is_kept_in_normalisation_form_C_and_trimmed() {
        // Act
        PersonName name = new PersonName("  Cafe\u0301  ", " Zoe\u0308 ");

        // Assert
        Assert.Equal("Café", name.FamilyName);
        Assert.Equal("Zoë", name.GivenNames);
    }

    [Fact]
    public void A_family_name_is_required() {
        Assert.Throws<ArgumentNullException>(() => new PersonName(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_family_name_cannot_be_blank(string blank) {
        Assert.Throws<ArgumentException>(() => new PersonName(blank));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_names_when_present_cannot_be_blank(string blank) {
        Assert.Throws<ArgumentException>(() => new PersonName("Tolstoï", blank));
    }

    [Fact]
    public void A_name_part_cannot_start_with_a_combining_mark() {
        Assert.Throws<ArgumentException>(() => new PersonName("\u0301Tolstoï"));
        Assert.Throws<ArgumentException>(() => new PersonName("Tolstoï", "\u0301Léon"));
    }

    [Fact]
    public void A_name_part_cannot_hold_two_consecutive_spaces() {
        Assert.Throws<ArgumentException>(() => new PersonName("Van  Gogh"));
        Assert.Throws<ArgumentException>(() => new PersonName("Van Gogh", "Vincent  Willem"));
    }

    [Fact]
    public void A_name_part_may_hold_single_spaces() {
        // Act
        PersonName name = new PersonName("Van Gogh", "Vincent Willem");

        // Assert
        Assert.Equal("Van Gogh, Vincent Willem", name.Heading);
    }

    [Theory]
    [InlineData("O'Brien")]
    [InlineData("O’Brien")]
    [InlineData("Saint-Exupéry")]
    [InlineData("Saint‐Exupéry")]
    [InlineData("St. John")]
    [InlineData("A.")]
    public void Apostrophes_hyphens_and_periods_are_part_of_names(string family) {
        // Act
        PersonName name = new PersonName(family);

        // Assert
        Assert.Equal(family, name.FamilyName);
    }

    [Fact]
    public void The_katakana_middle_dot_is_part_of_a_japanese_name() {
        // Act
        PersonName name = new PersonName("ジョン・スミス");

        // Assert
        Assert.Equal(WritingScript.Cjk, name.Script);
    }

    [Theory]
    [InlineData("R2D2")]
    [InlineData("Tolstoï,")]
    [InlineData("Tolstoï_")]
    [InlineData("Tol/stoi")]
    [InlineData("Tolstoi\u00A9")]
    [InlineData("Tol\u0000stoi")]
    [InlineData("Tol\tstoi")]
    [InlineData("Tol\u2028stoi")]
    [InlineData("Tolstoi\U0001F600")]
    [InlineData("Tol\u200Estoi")]
    public void A_name_refuses_what_is_neither_a_letter_nor_one_of_the_allowed_marks(string family) {
        Assert.Throws<ArgumentException>(() => new PersonName(family));
    }

    [Theory]
    [InlineData("...")]
    [InlineData("-")]
    [InlineData("'")]
    [InlineData("\u0301")]
    public void A_name_part_needs_at_least_one_letter(string withoutLetters) {
        Assert.Throws<ArgumentException>(() => new PersonName("Tolstoï", withoutLetters));
    }

    [Fact]
    public void A_name_part_may_hold_100_grapheme_clusters() {
        // Act
        PersonName name = new PersonName(new string('x', PersonName.MaxPartLength), new string('x', 100));

        // Assert
        Assert.Equal(100, name.FamilyName.Length);
    }

    [Fact]
    public void A_family_name_of_101_grapheme_clusters_is_too_long() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PersonName(new string('x', 101)));
    }

    [Fact]
    public void Given_names_of_101_grapheme_clusters_are_too_long() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PersonName("Tolstoï", new string('x', 101)));
    }

    [Fact]
    public void A_name_part_counts_grapheme_clusters_not_code_units() {
        // Arrange
        string hundredClusters = string.Concat(Enumerable.Repeat("x\u0301", 100));

        // Act
        PersonName name = new PersonName(hundredClusters);

        // Assert
        Assert.Equal(200, name.FamilyName.Length);
    }

    [Theory]
    [InlineData("Ивановich")]
    [InlineData("Tolstoï Толстой")]
    [InlineData("東京Tokyo")]
    public void A_name_part_cannot_mix_scripts(string mixed) {
        Assert.Throws<ArgumentException>(() => new PersonName(mixed));
    }

    [Theory]
    [InlineData("สมชาย")]
    [InlineData("ქართველი")]
    [InlineData("Ա")]
    public void A_name_cannot_be_written_in_an_unsupported_script(string unsupported) {
        Assert.Throws<ArgumentException>(() => new PersonName(unsupported));
    }

    [Fact]
    public void Given_names_must_be_in_the_script_of_the_family_name() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PersonName("Tolstoï", "Лев"));

        // Assert
        Assert.Equal("givenNames", refusal.ParamName);
    }

    [Fact]
    public void Given_names_in_the_same_non_latin_script_are_accepted() {
        // Act
        PersonName name = new PersonName("Чехов", "Антон");

        // Assert
        Assert.Equal(WritingScript.Cyrillic, name.Script);
    }

    [Fact]
    public void A_given_name_without_letters_of_its_own_script_cannot_hide_behind_the_family_name() {
        Assert.Throws<ArgumentException>(() => new PersonName("Tolstoï", "1828"));
    }

    [Theory]
    [InlineData("Müller", "Hans", "MÜLLER", "HANS")]
    [InlineData("Müller", "Hans", "Mu\u0308ller", "hans")]
    [InlineData("Чехов", null, "ЧЕХОВ", null)]
    public void Names_are_equal_under_normalisation_and_simple_case_folding(string family, string? given, string otherFamily, string? otherGiven) {
        Assert.Equal(new PersonName(family, given), new PersonName(otherFamily, otherGiven));
        Assert.Equal(new PersonName(family, given).GetHashCode(), new PersonName(otherFamily, otherGiven).GetHashCode());
    }

    [Fact]
    public void A_name_with_given_names_is_not_the_same_name_without() {
        Assert.NotEqual(new PersonName("Müller"), new PersonName("Müller", "Hans"));
    }

    [Fact]
    public void Moving_a_letter_from_the_family_name_to_the_given_names_makes_another_name() {
        Assert.NotEqual(new PersonName("Mar", "Ie"), new PersonName("Ma", "rIe"));
    }

    [Fact]
    public void A_name_is_not_equal_to_its_bare_letters() {
        Assert.NotEqual(new PersonName("Müller"), new PersonName("Muller"));
    }

    [Fact]
    public void A_name_is_not_equal_to_nothing() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Assert
        Assert.False(anyName.Equals(null));
    }

}
