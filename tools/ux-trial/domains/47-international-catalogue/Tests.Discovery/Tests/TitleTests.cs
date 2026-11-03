using System.Globalization;
using System.Text;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class TitleTests {

    [Theory]
    [InlineData("Café", WritingScript.Latin)]
    [InlineData("Ελλάδα", WritingScript.Greek)]
    [InlineData("Война и мир", WritingScript.Cyrillic)]
    [InlineData("ספר הספרים", WritingScript.Hebrew)]
    [InlineData("كتاب الأمير", WritingScript.Arabic)]
    [InlineData("पुस्तक", WritingScript.Devanagari)]
    [InlineData("東京物語", WritingScript.Cjk)]
    [InlineData("きょうの料理", WritingScript.Cjk)]
    [InlineData("한국어", WritingScript.Cjk)]
    [InlineData("\U00020000\U00020001", WritingScript.Cjk)]
    public void A_title_reports_the_script_of_its_letters(string text, WritingScript expectedScript) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal([expectedScript], title.LetterScripts());
        Assert.False(title.HasLettersOfUnsupportedScript());
    }

    [Fact]
    public void A_title_may_mix_the_scripts_of_its_letters() {
        // Act
        Title title = new Title("Moscou Москва 1984");

        // Assert
        Assert.Equal(new HashSet<WritingScript> { WritingScript.Latin, WritingScript.Cyrillic }, title.LetterScripts());
    }

    [Theory]
    [InlineData("1984")]
    [InlineData("42 - 7 !")]
    public void A_title_without_letters_belongs_to_no_script(string text) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Empty(title.LetterScripts());
        Assert.False(title.HasLettersOfUnsupportedScript());
    }

    [Theory]
    [InlineData("หนังสือ")]
    [InlineData("ბიბლიოთეკა")]
    [InlineData("Livre หนังสือ")]
    public void A_title_flags_letters_of_a_script_the_catalogue_does_not_support(string text) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.True(title.HasLettersOfUnsupportedScript());
    }

    [Fact]
    public void A_title_is_stored_in_normalisation_form_C() {
        // Arrange
        string decomposed = "Café";

        // Act
        Title title = new Title(decomposed);

        // Assert
        Assert.Equal("Café", title.Value);
        Assert.Equal(4, title.Length);
    }

    [Fact]
    public void A_title_is_trimmed() {
        // Act
        Title title = new Title("  \tLes Misérables  　");

        // Assert
        Assert.Equal("Les Misérables", title.Value);
        Assert.Equal("Les Misérables", title.ToString());
    }

    [Fact]
    public void A_title_counts_a_supplementary_ideograph_once() {
        // Act
        Title title = new Title("\U00020000\U00020001\U00020002");

        // Assert
        Assert.Equal(3, title.Length);
    }

    [Fact]
    public void A_title_counts_a_base_letter_and_its_marks_as_one_grapheme_cluster() {
        // Arrange: no precomposed form exists for this letter with two marks.
        string cluster = "ạ̈";

        // Act
        Title title = new Title(cluster + cluster + cluster);

        // Assert
        Assert.Equal(3, title.Length);
    }

    [Fact]
    public void A_title_may_hold_the_longest_length() {
        // Arrange
        string anyLetter = Scripts.Letter(WritingScript.Cyrillic).Generate();

        // Act
        Title title = new Title(string.Concat(Enumerable.Repeat(anyLetter, Title.MaxLength)));

        // Assert
        Assert.Equal(250, title.Length);
    }

    [Fact]
    public void A_title_longer_than_the_maximum_is_refused() {
        // Arrange
        string anyLetter = Scripts.Letter(WritingScript.Cyrillic).Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Title(string.Concat(Enumerable.Repeat(anyLetter, 251))));

        // Assert
        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void A_title_measures_its_length_in_grapheme_clusters_not_code_units() {
        // Arrange: 250 ideographs are 500 UTF-16 code units.
        string maximal = string.Concat(Enumerable.Repeat("\U00020000", 250));

        // Act
        Title title = new Title(maximal);

        // Assert
        Assert.Equal(250, title.Length);
        Assert.Equal(500, title.Value.Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Title(maximal + "\U00020000"));
    }

    [Fact]
    public void A_null_title_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Title(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t 　")]
    public void A_blank_title_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new Title(text));
    }

    [Theory]
    [InlineData("́e")]
    [InlineData("̈abc")]
    [InlineData("⃝abc")]
    [InlineData("ःabc")]
    public void A_title_that_starts_with_a_combining_mark_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new Title(text));
    }

    [Fact]
    public void A_title_may_hold_a_combining_mark_after_its_first_character() {
        // Act
        Title title = new Title("élan");

        // Assert
        Assert.Equal("élan", title.Value);
    }

    [Theory]
    [InlineData("\u0000")]
    [InlineData("\u0007")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\u001F")]
    [InlineData("\u007F")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void A_title_refuses_a_control_or_line_break_character_anywhere_inside(string forbidden) {
        // Arrange
        string anyPrefix = Scripts.TitleText(WritingScript.Latin).Generate();
        string anySuffix = Scripts.TitleText(WritingScript.Latin).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Title(anyPrefix + forbidden + anySuffix));
    }

    [Theory]
    [InlineData("\U0001F600")]
    [InlineData("☀")]
    [InlineData("✂")]
    [InlineData("➿")]
    [InlineData("⭐")]
    [InlineData("⭕")]
    [InlineData("️")]
    [InlineData("⃣")]
    [InlineData("\U0001FAFF")]
    [InlineData("\U0001F000")]
    public void A_title_refuses_an_emoji(string emoji) {
        // Arrange
        string anyPrefix = Scripts.TitleText(WritingScript.Latin).Generate();
        string anySuffix = Scripts.TitleText(WritingScript.Latin).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Title(anyPrefix + " " + emoji + " " + anySuffix));
    }

    [Theory]
    [InlineData("⭖")]
    [InlineData("■")]
    [InlineData("⯿")]
    [InlineData("©")]
    [InlineData("‍")]
    [InlineData("‌")]
    public void A_title_accepts_symbols_next_to_the_emoji_ranges_and_the_joiners(string allowed) {
        // Arrange
        string anyPrefix = Scripts.TitleText(WritingScript.Latin).Generate();

        // Act
        Title title = new Title(anyPrefix + allowed + "x");

        // Assert
        Assert.Contains(allowed, title.Value);
    }

    public static TheoryData<char> UnpairedSurrogates => [(char)0xD800, (char)0xDBFF, (char)0xDC00, (char)0xDFFF];

    // The surrogates are passed as chars: an attribute argument holding a lone surrogate is mangled into U+FFFD by the compiler.
    [Theory]
    [MemberData(nameof(UnpairedSurrogates))]
    public void A_title_with_an_unpaired_surrogate_is_refused(char surrogate) {
        // Arrange
        string anyText = Scripts.TitleText(WritingScript.Latin).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Title(anyText + surrogate));
        Assert.Throws<ArgumentException>(() => new Title(surrogate + anyText));
        Assert.Throws<ArgumentException>(() => new Title(anyText + surrogate + anyText));
    }

    [Fact]
    public void A_high_surrogate_followed_by_its_low_surrogate_is_a_supplementary_character() {
        // Act
        Title title = new Title("\uD840\uDC00");

        // Assert
        Assert.Equal(1, title.Length);
        Assert.Equal(WritingScript.Cjk, Assert.Single(title.LetterScripts()));
    }

    [Fact]
    public void Surrogates_in_the_wrong_order_are_refused() {
        Assert.Throws<ArgumentException>(() => new Title("\uDC00\uD840"));
    }

    [Fact]
    public void An_unpaired_surrogate_is_refused_before_anything_else() {
        // Arrange: the text is also too long, which would raise another exception type.
        string tooLongWithSurrogate = new string('a', 400) + (char)0xD800;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Title(tooLongWithSurrogate));
    }

    [Theory]
    [InlineData("‎")]
    [InlineData("‏")]
    [InlineData("؜")]
    [InlineData("⁦⁩")]
    [InlineData("⁧⁩")]
    [InlineData("⁨⁩")]
    [InlineData("‪")]
    public void A_title_refuses_bidirectional_controls_unless_it_declares_them(string control) {
        // Arrange
        string anyText = Scripts.TitleText(WritingScript.Latin).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Title(anyText + control + anyText));
    }

    [Theory]
    [InlineData("‎")]
    [InlineData("‏")]
    [InlineData("؜")]
    [InlineData("⁦⁩")]
    [InlineData("⁧⁩")]
    [InlineData("⁨⁩")]
    [InlineData("⁦⁧⁩⁩")]
    [InlineData("⁦⁩⁧⁩")]
    public void A_title_declared_to_carry_bidirectional_controls_accepts_marks_and_balanced_isolates(string controls) {
        // Arrange
        string anyText = Scripts.TitleText(WritingScript.Latin).Generate();

        // Act
        Title title = new Title(anyText + controls + anyText, allowBidiControls: true);

        // Assert
        Assert.True(title.HasBidiControls);
    }

    [Fact]
    public void A_title_without_bidirectional_controls_does_not_report_any_even_when_declared() {
        // Arrange
        string anyText = Scripts.TitleText(WritingScript.Hebrew).Generate();

        // Act
        Title title = new Title(anyText, allowBidiControls: true);

        // Assert
        Assert.False(title.HasBidiControls);
    }

    [Theory]
    [InlineData("‪")]
    [InlineData("‫")]
    [InlineData("‬")]
    [InlineData("‭")]
    [InlineData("‮")]
    public void Embeddings_and_overrides_are_refused_even_when_bidirectional_controls_are_declared(string control) {
        // Arrange
        string anyText = Scripts.TitleText(WritingScript.Arabic).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Title(anyText + control + anyText, allowBidiControls: true));
    }

    [Theory]
    [InlineData("⁩")]
    [InlineData("⁩⁦")]
    [InlineData("⁦⁩⁩")]
    [InlineData("⁦")]
    [InlineData("⁧⁦⁩")]
    [InlineData("⁨")]
    public void Isolates_must_balance(string controls) {
        // Arrange
        string anyText = Scripts.TitleText(WritingScript.Hebrew).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Title(anyText + controls + anyText, allowBidiControls: true));
    }

    [Theory]
    [InlineData("كتاب", true)]
    [InlineData("ספר", true)]
    [InlineData("123 كتاب", true)]
    [InlineData("... ספר Book", true)]
    [InlineData("Book كتاب", false)]
    [InlineData("Книга ספר", false)]
    [InlineData("1984", false)]
    [InlineData("東京", false)]
    public void A_title_reads_right_to_left_when_its_first_letter_is_arabic_or_hebrew(string text, bool expected) {
        // Act
        Title title = new Title(text);

        // Assert
        Assert.Equal(expected, title.IsRightToLeft());
    }

    [Theory]
    [InlineData("Café", "CAFÉ")]
    [InlineData("café", "Café")]
    [InlineData("Война и мир", "ВОЙНА И МИР")]
    [InlineData("ελλάδα", "ΕΛΛΆΔΑ")]
    [InlineData("Straße", "straße")]
    public void Two_titles_are_equal_under_normalisation_and_case_folding(string first, string second) {
        // Act
        Title one = new Title(first);
        Title other = new Title(second);

        // Assert
        Assert.Equal(one, other);
        Assert.True(one == other || one.Equals(other));
        Assert.Equal(one.GetHashCode(), other.GetHashCode());
    }

    [Theory]
    [InlineData("L'été", "L’été")]
    [InlineData("cafe", "café")]
    [InlineData("Eté", "Ete")]
    [InlineData("Mr Smith", "Mr  Smith")]
    [InlineData("a-b", "a‐b")]
    public void Two_titles_that_differ_beyond_case_and_normalisation_are_not_equal(string first, string second) {
        // Act
        Title one = new Title(first);
        Title other = new Title(second);

        // Assert
        Assert.NotEqual(one, other);
    }

    [Fact]
    public void Bidirectional_marks_do_not_take_part_in_equality() {
        // Act
        Title plain = new Title("Book كتاب");
        Title marked = new Title("Book ‏⁧كتاب⁩", allowBidiControls: true);

        // Assert
        Assert.Equal(plain, marked);
        Assert.Equal(plain.GetHashCode(), marked.GetHashCode());
    }

    [Fact]
    public void A_title_is_never_equal_to_null_or_to_another_type() {
        // Arrange
        Title anyTitle = Any.Title().Generate();

        // Assert
        Assert.False(anyTitle.Equals(null));
        Assert.False(anyTitle.Equals((object)anyTitle.Value));
    }

}
