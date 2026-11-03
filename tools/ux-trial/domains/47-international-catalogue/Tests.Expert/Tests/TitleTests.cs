using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class TitleTests {

    [Fact]
    public void A_title_is_kept_in_normalisation_form_C() {
        // Arrange
        string decomposedCafe = "Cafe\u0301";

        // Act
        Title title = new Title(decomposedCafe);

        // Assert
        Assert.Equal("Café", title.Value);
    }

    [Fact]
    public void A_title_is_trimmed() {
        // Act
        Title title = new Title("  Dune \t");

        // Assert
        Assert.Equal("Dune", title.Value);
    }

    [Fact]
    public void Trailing_line_breaks_are_trimmed_before_control_characters_are_refused() {
        // Act
        Title title = new Title("Dune\u2028\n");

        // Assert
        Assert.Equal("Dune", title.Value);
    }

    [Fact]
    public void A_title_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Title(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t \n")]
    public void A_title_cannot_be_blank(string blank) {
        Assert.Throws<ArgumentException>(() => new Title(blank));
    }

    // Not a theory: the test runner replaces a lone surrogate by U+FFFD on its way through the theory data.
    [Fact]
    public void An_unpaired_surrogate_is_refused() {
        // Arrange
        string[] unpairedSurrogates = ["\uD800", "a\uD83D", "\uDC00a", "a\uDC00b", "ab\uD83D"];

        foreach (string unpaired in unpairedSurrogates) {
            // Act
            ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => new Title(unpaired));

            // Assert
            Assert.Contains("surrogate", refusal.Message);
        }
    }

    [Fact]
    public void An_unpaired_surrogate_is_refused_before_anything_else_is_looked_at() {
        // Arrange
        string startsWithACombiningMarkAndHoldsALoneSurrogate = "\u0301\uDC00";

        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => new Title(startsWithACombiningMarkAndHoldsALoneSurrogate));

        // Assert
        Assert.Contains("surrogate", refusal.Message);
    }

    [Fact]
    public void A_surrogate_pair_is_not_unpaired() {
        // Act
        Title title = new Title("\U00020000");

        // Assert
        Assert.Equal("\U00020000", title.Value);
    }

    [Theory]
    [InlineData("\u0301Cafe")]
    [InlineData("\u0308")]
    [InlineData("\u20DDx")]
    [InlineData("  \u0903x")]
    public void A_title_cannot_start_with_a_combining_mark(string startsWithAMark) {
        Assert.Throws<ArgumentException>(() => new Title(startsWithAMark));
    }

    [Fact]
    public void A_combining_mark_inside_a_title_is_fine() {
        // Act
        Title title = new Title("x\u0301ylophone");

        // Assert
        Assert.Equal("x\u0301ylophone", title.Value);
    }

    [Theory]
    [InlineData("Cafe\u0301", 4)]
    [InlineData("Café", 4)]
    [InlineData("\U00020000", 1)]
    [InlineData("x\u0301", 1)]
    [InlineData("Dune", 4)]
    public void The_length_counts_grapheme_clusters_not_code_units(string text, int expectedLength) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal(expectedLength, title.Length);
    }

    [Fact]
    public void A_title_may_hold_up_to_250_grapheme_clusters() {
        // Act
        Title title = new Title(new string('a', Title.MaxLength));

        // Assert
        Assert.Equal(250, title.Length);
    }

    [Fact]
    public void A_title_of_251_grapheme_clusters_is_too_long() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Title(new string('a', 251)));
    }

    [Fact]
    public void Supplementary_ideographs_count_once_against_the_limit() {
        // Arrange
        string twoHundredFiftyIdeographs = string.Concat(Enumerable.Repeat("\U00020000", 250));

        // Act
        Title title = new Title(twoHundredFiftyIdeographs);

        // Assert
        Assert.Equal(250, title.Length);
    }

    [Fact]
    public void Letters_with_a_mark_that_has_no_precomposed_form_count_once_against_the_limit() {
        // Arrange
        string twoHundredFiftyClusters = string.Concat(Enumerable.Repeat("x\u0301", 250));

        // Act
        Title title = new Title(twoHundredFiftyClusters);

        // Assert
        Assert.Equal(250, title.Length);
    }

    [Fact]
    public void A_251st_cluster_is_too_long_even_when_each_cluster_is_two_code_units() {
        // Arrange
        string twoHundredFiftyOneClusters = string.Concat(Enumerable.Repeat("x\u0301", 251));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Title(twoHundredFiftyOneClusters));
    }

    [Theory]
    [InlineData("a\u0000b")]
    [InlineData("a\tb")]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("a\u0085b")]
    [InlineData("a\u007Fb")]
    [InlineData("a\u2028b")]
    [InlineData("a\u2029b")]
    public void A_title_cannot_contain_a_control_or_line_break_character(string withControl) {
        Assert.Throws<ArgumentException>(() => new Title(withControl));
    }

    [Theory]
    [InlineData("\U0001F600")]
    [InlineData("\U0001F000")]
    [InlineData("\U0001FAFF")]
    [InlineData("\u2600")]
    [InlineData("\u27BF")]
    [InlineData("\u2B50")]
    [InlineData("\u2B55")]
    [InlineData("a\uFE0F")]
    [InlineData("1\u20E3")]
    public void A_title_cannot_contain_an_emoji(string withEmoji) {
        Assert.Throws<ArgumentException>(() => new Title(withEmoji));
    }

    [Theory]
    [InlineData("\u25FF")]
    [InlineData("\u27C0")]
    [InlineData("\u2B4F")]
    [InlineData("\u2B56")]
    [InlineData("\U0001EFFF")]
    [InlineData("\U0001FB00")]
    public void The_symbols_next_to_the_emoji_ranges_are_not_emoji(string neighbour) {
        // Act
        Title title = new Title("a" + neighbour);

        // Assert
        Assert.Equal("a" + neighbour, title.Value);
    }

    [Theory]
    [InlineData("a\u200Db")]
    [InlineData("a\u200Cb")]
    public void Joiners_stay_allowed(string withJoiner) {
        // Act
        Title title = new Title(withJoiner);

        // Assert
        Assert.Equal(withJoiner, title.Value);
    }

    [Theory]
    [InlineData("a\u200Eb")]
    [InlineData("a\u200Fb")]
    [InlineData("a\u061Cb")]
    [InlineData("a\u202Ab")]
    [InlineData("a\u202Bb")]
    [InlineData("a\u202Cb")]
    [InlineData("a\u202Db")]
    [InlineData("a\u202Eb")]
    [InlineData("a\u2066b")]
    [InlineData("a\u2067b")]
    [InlineData("a\u2068b")]
    [InlineData("a\u2069b")]
    public void A_bidirectional_control_is_refused_unless_the_title_declares_it(string withBidiControl) {
        Assert.Throws<ArgumentException>(() => new Title(withBidiControl));
    }

    [Theory]
    [InlineData("a\u200Eb")]
    [InlineData("a\u200Fb")]
    [InlineData("a\u061Cb")]
    [InlineData("a\u2066b\u2069")]
    [InlineData("a\u2067b\u2069")]
    [InlineData("a\u2068b\u2069")]
    [InlineData("\u2066x\u2067y\u2069z\u2069")]
    public void A_declared_title_accepts_marks_and_balanced_isolates(string withBidiControls) {
        // Act
        Title title = new Title(withBidiControls, allowBidiControls: true);

        // Assert
        Assert.True(title.HasBidiControls);
    }

    [Theory]
    [InlineData("a\u202Ab\u202C")]
    [InlineData("a\u202Bb\u202C")]
    [InlineData("a\u202Cb")]
    [InlineData("a\u202Db\u202C")]
    [InlineData("a\u202Eb\u202C")]
    public void Embeddings_and_overrides_are_refused_even_when_bidirectional_controls_are_declared(string withEmbedding) {
        Assert.Throws<ArgumentException>(() => new Title(withEmbedding, allowBidiControls: true));
    }

    [Theory]
    [InlineData("a\u2069")]
    [InlineData("\u2066a")]
    [InlineData("\u2066a\u2069\u2069")]
    [InlineData("\u2066\u2067a\u2069")]
    [InlineData("\u2069\u2066a")]
    public void Isolates_must_balance(string unbalanced) {
        Assert.Throws<ArgumentException>(() => new Title(unbalanced, allowBidiControls: true));
    }

    [Fact]
    public void A_title_declared_to_carry_bidirectional_controls_without_any_reports_none() {
        // Act
        Title title = new Title("Dune", allowBidiControls: true);

        // Assert
        Assert.False(title.HasBidiControls);
    }

    [Fact]
    public void A_title_has_no_bidirectional_controls_unless_it_holds_one() {
        Assert.False(new Title("Dune").HasBidiControls);
    }

    [Theory]
    [InlineData("Moscou", new[] { WritingScript.Latin })]
    [InlineData("Москва", new[] { WritingScript.Cyrillic })]
    [InlineData("Αθήνα", new[] { WritingScript.Greek })]
    [InlineData("ירושלים", new[] { WritingScript.Hebrew })]
    [InlineData("القاهرة", new[] { WritingScript.Arabic })]
    [InlineData("द\u093Fल\u094Dल\u0940", new[] { WritingScript.Devanagari })]
    [InlineData("東京", new[] { WritingScript.Cjk })]
    [InlineData("한국어", new[] { WritingScript.Cjk })]
    [InlineData("ひらがなカタカナ漢字", new[] { WritingScript.Cjk })]
    [InlineData("Moscou Москва", new[] { WritingScript.Latin, WritingScript.Cyrillic })]
    public void The_scripts_of_a_title_are_those_of_its_letters(string text, WritingScript[] expected) {
        // Act
        IReadOnlySet<WritingScript> scripts = new Title(text).LetterScripts();

        // Assert
        Assert.True(scripts.SetEquals(expected));
    }

    [Theory]
    [InlineData("1984")]
    [InlineData("1 - 2 : 3 ?")]
    [InlineData("(...)")]
    public void A_title_without_letters_belongs_to_no_script(string withoutLetters) {
        // Act
        Title title = new Title(withoutLetters);

        // Assert
        Assert.Empty(title.LetterScripts());
        Assert.False(title.HasLettersOfUnsupportedScript());
    }

    [Theory]
    [InlineData("ไทย")]
    [InlineData("ქართული")]
    [InlineData("Ա")]
    [InlineData("ᴀ")]
    [InlineData("ʰ")]
    [InlineData("অ")]
    [InlineData("ㄅ")]
    [InlineData("ꀀ")]
    public void A_letter_of_a_script_the_catalogue_does_not_know_is_reported(string unsupported) {
        // Act
        Title title = new Title("a " + unsupported);

        // Assert
        Assert.True(title.HasLettersOfUnsupportedScript());
        Assert.True(title.LetterScripts().SetEquals([WritingScript.Latin]));
    }

    [Theory]
    [InlineData("A", WritingScript.Latin)]
    [InlineData("ª", WritingScript.Latin)]
    [InlineData("ɏ", WritingScript.Latin)]
    [InlineData("ɐ", WritingScript.Latin)]
    [InlineData("ʯ", WritingScript.Latin)]
    [InlineData("Ḁ", WritingScript.Latin)]
    [InlineData("ỿ", WritingScript.Latin)]
    [InlineData("Ⱡ", WritingScript.Latin)]
    [InlineData("Ɀ", WritingScript.Latin)]
    [InlineData("Ꜣ", WritingScript.Latin)]
    [InlineData("Ͱ", WritingScript.Greek)]
    [InlineData("Ͽ", WritingScript.Greek)]
    [InlineData("ἀ", WritingScript.Greek)]
    [InlineData("ῼ", WritingScript.Greek)]
    [InlineData("Ѐ", WritingScript.Cyrillic)]
    [InlineData("ԯ", WritingScript.Cyrillic)]
    [InlineData("ᲀ", WritingScript.Cyrillic)]
    [InlineData("ᲈ", WritingScript.Cyrillic)]
    [InlineData("Ꙁ", WritingScript.Cyrillic)]
    [InlineData("ꚝ", WritingScript.Cyrillic)]
    [InlineData("א", WritingScript.Hebrew)]
    [InlineData("ת", WritingScript.Hebrew)]
    [InlineData("יִ", WritingScript.Hebrew)]
    [InlineData("ﭏ", WritingScript.Hebrew)]
    [InlineData("ء", WritingScript.Arabic)]
    [InlineData("ۿ", WritingScript.Arabic)]
    [InlineData("ݐ", WritingScript.Arabic)]
    [InlineData("ݿ", WritingScript.Arabic)]
    [InlineData("ࢠ", WritingScript.Arabic)]
    [InlineData("ﭐ", WritingScript.Arabic)]
    [InlineData("ﷻ", WritingScript.Arabic)]
    [InlineData("ﹰ", WritingScript.Arabic)]
    [InlineData("ﻼ", WritingScript.Arabic)]
    [InlineData("ऄ", WritingScript.Devanagari)]
    [InlineData("ॿ", WritingScript.Devanagari)]
    [InlineData("ꣲ", WritingScript.Devanagari)]
    [InlineData("ᄀ", WritingScript.Cjk)]
    [InlineData("ᇿ", WritingScript.Cjk)]
    [InlineData("ぁ", WritingScript.Cjk)]
    [InlineData("ヿ", WritingScript.Cjk)]
    [InlineData("㐀", WritingScript.Cjk)]
    [InlineData("䶿", WritingScript.Cjk)]
    [InlineData("一", WritingScript.Cjk)]
    [InlineData("鿿", WritingScript.Cjk)]
    [InlineData("가", WritingScript.Cjk)]
    [InlineData("힣", WritingScript.Cjk)]
    [InlineData("豈", WritingScript.Cjk)]
    [InlineData("\U00020000", WritingScript.Cjk)]
    [InlineData("\U0003134A", WritingScript.Cjk)]
    public void A_letter_at_the_edge_of_a_script_range_belongs_to_that_script(string letter, WritingScript expected) {
        // Act
        IReadOnlySet<WritingScript> scripts = new Title(letter).LetterScripts();

        // Assert
        Assert.True(scripts.SetEquals([expected]));
    }

    [Theory]
    [InlineData("שלום", true)]
    [InlineData("مرحبا", true)]
    [InlineData("123 שלום", true)]
    [InlineData("(مرحبا)", true)]
    [InlineData("abc שלום", false)]
    [InlineData("Москва", false)]
    [InlineData("東京", false)]
    [InlineData("1984", false)]
    public void A_title_reads_right_to_left_when_its_first_letter_is_arabic_or_hebrew(string text, bool rightToLeft) {
        Assert.Equal(rightToLeft, new Title(text).IsRightToLeft());
    }

    [Theory]
    [InlineData("Café", "CAFÉ")]
    [InlineData("Café", "Cafe\u0301")]
    [InlineData("Straße", "STRASSE", false)]
    [InlineData("Москва", "МОСКВА")]
    [InlineData("Αθήνα", "ΑΘΉΝΑ")]
    public void Titles_are_equal_under_normalisation_and_simple_case_folding(string first, string second, bool equal = true) {
        // Act
        bool areEqual = new Title(first).Equals(new Title(second));

        // Assert
        Assert.Equal(equal, areEqual);
    }

    [Theory]
    [InlineData("L'Étranger", "L’Étranger")]
    [InlineData("Cafe", "Café")]
    [InlineData("Dune", "Dune 2")]
    public void Titles_that_differ_beyond_case_and_normalisation_are_different(string first, string second) {
        Assert.False(new Title(first).Equals(new Title(second)));
    }

    [Fact]
    public void Bidirectional_controls_do_not_make_two_titles_different() {
        // Arrange
        Title plain  = new Title("Dune");
        Title marked = new Title("Du\u200Ene", allowBidiControls: true);

        // Assert
        Assert.Equal(plain, marked);
    }

    [Fact]
    public void Equal_titles_have_the_same_hash_code() {
        // Arrange
        Title lower = new Title("café");
        Title upper = new Title("CAFE\u0301");

        // Assert
        Assert.Equal(lower.GetHashCode(), upper.GetHashCode());
    }

    [Fact]
    public void A_title_is_not_equal_to_nothing_or_to_a_string() {
        // Arrange
        Title title = new Title("Dune");

        // Assert
        Assert.False(title.Equals(null));
        Assert.False(title.Equals((object)"Dune"));
    }

    [Fact]
    public void A_title_is_written_as_its_text() {
        Assert.Equal("Dune", new Title(" Dune ").ToString());
    }

}
