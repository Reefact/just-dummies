using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class TitleTests {

    [Fact]
    public void A_decomposed_title_is_stored_in_NFC_and_trimmed() {
        // Act
        Title title = new Title("  Cafe\u0301 \t");

        // Assert
        Assert.Equal("Caf\u00E9", title.Value);
        Assert.Equal(4, title.Length);
    }

    [Theory]
    [InlineData("Caf\u00E9", 4)]
    [InlineData("\U00020000", 1)]                  // a supplementary CJK ideograph is two UTF-16 units and one cluster
    [InlineData("\u0915\u093F", 1)]                // Devanagari KA + vowel sign I: two code points, one cluster
    [InlineData("x\u0301y\u0301", 2)]              // no precomposed form: base + combining mark stays one cluster
    [InlineData("\u0645\u0631\u062D\u0628\u0627", 5)]
    public void Length_counts_grapheme_clusters_rather_than_UTF16_units(string text, int expectedLength) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal(expectedLength, title.Length);
    }

    [Fact]
    public void A_title_of_250_clusters_is_accepted_even_when_it_has_more_than_250_UTF16_units() {
        // Arrange
        string text = string.Concat(Enumerable.Repeat("x\u0301", Title.MaxLength));

        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal(250, title.Length);
        Assert.Equal(500, title.Value.Length);
    }

    [Fact]
    public void A_title_of_251_clusters_is_refused() {
        // Arrange
        string text = string.Concat(Enumerable.Repeat("x\u0301", Title.MaxLength + 1));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Title(text));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void The_longest_drawn_title_is_accepted_and_one_cluster_more_is_not() {
        // Arrange
        bool anyAllowBidi = Any.Boolean().Generate();
        string longest = Any.Title().OfLength(Title.MaxLength).Generate().Value;

        // Act
        Title accepted = new Title(longest, anyAllowBidi);
        Exception? refusal = Record.Exception(() => new Title(longest + "a", anyAllowBidi));

        // Assert
        Assert.Equal(250, accepted.Length);
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    [InlineData("\u00A0")]
    public void An_empty_or_blank_title_is_refused(string text) {
        // Arrange
        bool anyAllowBidi = Any.Boolean().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Title(text, anyAllowBidi));
    }

    [Fact]
    public void A_null_title_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Title(null!));
    }

    // Lone surrogates cannot travel through [InlineData]: xUnit's serialization turns them into U+FFFD, so the cases are indexed.
    private static readonly string[] UnpairedSurrogateTexts = ["\uD800", "ab\uD83Dcd", "\uDC00ab", "ab\uDC00", "\uD83D\uD83D\uDE00"];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void An_unpaired_surrogate_is_refused_before_anything_else(int caseIndex) {
        // Arrange
        string text = UnpairedSurrogateTexts[caseIndex];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Title(text));

        // Assert
        Assert.Contains("surrogate", refusal.Message);
    }

    [Theory]
    [InlineData("\u0301abc")]
    [InlineData("  \u0301abc")]
    [InlineData("\u0903abc")]   // a spacing combining mark
    [InlineData("\u20DDabc")]   // an enclosing mark
    public void A_title_cannot_start_with_a_combining_mark(string text) {
        Assert.Throws<ArgumentException>(() => new Title(text));
    }

    [Theory]
    [InlineData("ab\u0007cd")]
    [InlineData("two\nlines")]
    [InlineData("tab\tinside")]
    [InlineData("a\u2028b")]
    [InlineData("a\u2029b")]
    [InlineData("a\u0085b")]
    public void A_control_or_line_break_character_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new Title(text));
    }

    [Theory]
    [InlineData("Hello \U0001F600")]
    [InlineData("Star \u2605")]
    [InlineData("Scissors \u2702")]
    [InlineData("Medium star \u2B50")]
    [InlineData("a\uFE0F")]
    [InlineData("1\u20E3")]
    [InlineData("\U0001FAFF x")]
    [InlineData("\U0001F000 tile")]
    public void An_emoji_is_refused_whether_or_not_bidirectional_controls_are_declared(string text) {
        // Arrange
        bool anyAllowBidi = Any.Boolean().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Title(text, anyAllowBidi));

        // Assert
        Assert.Contains("emoji", refusal.Message);
    }

    [Fact]
    public void Joiners_are_not_emoji_and_stay_allowed() {
        // Act
        Title title = new Title("\u0915\u094D\u200D\u0937 \u0645\u200C\u0647");

        // Assert
        Assert.False(title.HasBidiControls);
    }

    [Theory]
    [InlineData("\u200E")]   // LRM
    [InlineData("\u200F")]   // RLM
    [InlineData("\u061C")]   // ALM
    [InlineData("\u2066")]   // LRI
    [InlineData("\u2067")]   // RLI
    [InlineData("\u2068")]   // FSI
    [InlineData("\u2069")]   // PDI
    [InlineData("\u202A")]
    [InlineData("\u202B")]
    [InlineData("\u202C")]
    [InlineData("\u202D")]
    [InlineData("\u202E")]
    public void A_bidirectional_control_is_refused_unless_declared(string control) {
        // Arrange
        string text = "ab" + control + "cd";

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Title(text));

        // Assert
        Assert.Contains("declared", refusal.Message);
    }

    [Theory]
    [InlineData("\u200E")]
    [InlineData("\u200F")]
    [InlineData("\u061C")]
    public void A_directional_mark_is_accepted_when_declared(string mark) {
        // Act
        Title title = new Title("abc " + mark + "\u05E9\u05DC\u05D5\u05DD", allowBidiControls: true);

        // Assert
        Assert.True(title.HasBidiControls);
    }

    [Theory]
    [InlineData("\u202A")]
    [InlineData("\u202B")]
    [InlineData("\u202C")]
    [InlineData("\u202D")]
    [InlineData("\u202E")]
    public void Embeddings_and_overrides_are_never_accepted_even_when_bidi_controls_are_declared(string control) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Title("ab" + control + "cd", allowBidiControls: true));

        // Assert
        Assert.Contains("isolates", refusal.Message);
    }

    [Theory]
    [InlineData("\u2066abc\u2069")]
    [InlineData("\u2067abc\u2069")]
    [InlineData("\u2068abc\u2069")]
    [InlineData("\u2066a\u2067b\u2069c\u2069")]
    [InlineData("\u2066a\u2069 \u2067b\u2069")]
    public void Balanced_isolates_are_accepted_when_declared(string text) {
        // Act
        Title title = new Title(text, allowBidiControls: true);

        // Assert
        Assert.True(title.HasBidiControls);
    }

    [Theory]
    [InlineData("abc\u2069")]
    [InlineData("\u2066abc")]
    [InlineData("\u2066\u2067abc\u2069")]
    [InlineData("\u2069abc\u2066")]
    public void Unbalanced_isolates_are_refused(string text) {
        Assert.Throws<ArgumentException>(() => new Title(text, allowBidiControls: true));
    }

    [Fact]
    public void A_title_declared_to_carry_bidi_controls_that_has_none_reports_none() {
        // Arrange
        Title anyPlainTitle = Any.Title().Generate();

        // Act
        Title title = new Title(anyPlainTitle.Value, allowBidiControls: true);

        // Assert
        Assert.False(title.HasBidiControls);
    }

    [Fact]
    public void Titles_are_equal_under_NFC_and_case_folding() {
        // Assert
        Assert.Equal(new Title("Caf\u00E9"), new Title("CAF\u00C9"));
        Assert.Equal(new Title("Caf\u00E9"), new Title("Cafe\u0301"));
        Assert.Equal(new Title("\u0422\u043E\u043B\u0441\u0442\u043E\u0439"), new Title("\u0442\u043E\u043B\u0421\u0422\u041E\u0419"));
    }

    [Fact]
    public void Equal_titles_have_equal_hash_codes() {
        // Act
        int lower = new Title("caf\u00E9 noir").GetHashCode();
        int upper = new Title("CAFE\u0301 NOIR").GetHashCode();

        // Assert
        Assert.Equal(lower, upper);
    }

    [Fact]
    public void Titles_ignore_bidirectional_controls_when_compared() {
        // Arrange
        Title bare   = new Title("abc");
        Title marked = new Title("a\u200Ebc\u200F", allowBidiControls: true);

        // Assert
        Assert.Equal(bare, marked);
        Assert.Equal(bare.GetHashCode(), marked.GetHashCode());
    }

    [Fact]
    public void An_apostrophe_is_not_a_typographic_apostrophe_and_an_accent_is_not_its_bare_letter() {
        // Assert
        Assert.NotEqual(new Title("L'etranger"), new Title("L\u2019etranger"));
        Assert.NotEqual(new Title("Cafe"), new Title("Caf\u00E9"));
    }

    [Fact]
    public void A_title_is_never_equal_to_null_or_to_another_type() {
        // Arrange
        Title anyTitle = Any.Title().Generate();

        // Assert
        Assert.False(anyTitle.Equals((Title?)null));
        Assert.False(anyTitle.Equals(anyTitle.Value));
    }

    [Fact]
    public void ToString_gives_the_stored_text() {
        Assert.Equal("Caf\u00E9", new Title("Cafe\u0301 ").ToString());
    }

    [Theory]
    [InlineData("Hello", WritingScript.Latin)]
    [InlineData("\u0395\u03BB\u03BB\u03AC\u03B4\u03B1", WritingScript.Greek)]
    [InlineData("\u03A3\u1F00", WritingScript.Greek)]
    [InlineData("\u0420\u043E\u0441\u0441\u0438\u044F", WritingScript.Cyrillic)]
    [InlineData("\u05E9\u05DC\u05D5\u05DD", WritingScript.Hebrew)]
    [InlineData("\u0633\u0644\u0627\u0645", WritingScript.Arabic)]
    [InlineData("\u092D\u093E\u0930\u0924", WritingScript.Devanagari)]
    [InlineData("\u65E5\u672C\u8A9E", WritingScript.Cjk)]
    [InlineData("\u3072\u3089\u304C\u306A", WritingScript.Cjk)]
    [InlineData("\u30AB\u30BF\u30AB\u30CA", WritingScript.Cjk)]
    [InlineData("\uD55C\uAD6D\uC5B4", WritingScript.Cjk)]
    [InlineData("\U00020000", WritingScript.Cjk)]
    public void Letters_are_attributed_to_their_script(string text, WritingScript expected) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal([expected], title.LetterScripts());
        Assert.False(title.HasLettersOfUnsupportedScript());
    }

    [Fact]
    public void A_title_can_mix_scripts_and_reports_each() {
        // Act
        Title title = new Title("Moscou \u041C\u043E\u0441\u043A\u0432\u0430 \u6771\u4EAC");

        // Assert
        Assert.Equal(3, title.LetterScripts().Count);
        Assert.Contains(WritingScript.Latin, title.LetterScripts());
        Assert.Contains(WritingScript.Cyrillic, title.LetterScripts());
        Assert.Contains(WritingScript.Cjk, title.LetterScripts());
    }

    [Theory]
    [InlineData("1984")]
    [InlineData("42 - 7 (second)!")]
    [InlineData("\u2116 5")]
    public void Digits_and_punctuation_belong_to_no_script(string text) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal(text == "42 - 7 (second)!" ? 1 : 0, title.LetterScripts().Count);
    }

    [Theory]
    [InlineData("\u0E2A\u0E27\u0E31\u0E2A\u0E14\u0E35")]   // Thai
    [InlineData("\u10D0\u10D1\u10D2")]                      // Georgian
    [InlineData("\uFF21\uFF22")]                            // fullwidth Latin
    public void A_letter_of_an_unsupported_script_is_reported_and_has_no_script(string text) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.True(title.HasLettersOfUnsupportedScript());
        Assert.Empty(title.LetterScripts());
    }

    [Fact]
    public void A_supported_letter_next_to_an_unsupported_one_is_still_reported() {
        // Act
        Title title = new Title("Bangkok \u0E01\u0E23\u0E38\u0E07");

        // Assert
        Assert.True(title.HasLettersOfUnsupportedScript());
        Assert.Equal([WritingScript.Latin], title.LetterScripts());
    }

    [Theory]
    [InlineData("\u05E9\u05DC\u05D5\u05DD world", true)]
    [InlineData("hello \u05E9\u05DC\u05D5\u05DD", false)]
    [InlineData("123 \u0633\u0644\u0627\u0645", true)]
    [InlineData("(\u0633\u0644\u0627\u0645) hello", true)]
    [InlineData("1984", false)]
    [InlineData("\u0420\u043E\u0441\u0441\u0438\u044F", false)]
    [InlineData("\u0E2A\u0E27\u0E31\u0E2A\u0E14\u0E35 \u05E9\u05DC\u05D5\u05DD", false)]
    public void A_title_reads_right_to_left_when_its_first_letter_is_Arabic_or_Hebrew(string text, bool expected) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal(expected, title.IsRightToLeft());
    }

}
