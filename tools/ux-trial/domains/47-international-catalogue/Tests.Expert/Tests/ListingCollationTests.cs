using System.Globalization;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class ListingCollationTests {

    private static readonly CultureInfo English = new CultureInfo("en-US");
    private static readonly CultureInfo German  = new CultureInfo("de-DE");
    private static readonly CultureInfo Swedish = new CultureInfo("sv-SE");

    private static ArticleRule ArticlesOf(string language, params string[] articles) {
        return new ArticleRule(new LanguageTag(language), articles);
    }

    private static ListingCollation AnEnglishCollationSkipping(params string[] articles) {
        return new ListingCollation(English, [ArticlesOf("en", articles)]);
    }

    private static AnyBibliographicRecord ALatinRecordTitled(string title) {
        return Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title(title)).WithLanguage(new LanguageTag("en"));
    }

    // ---- construction -------------------------------------------------------------------------------------------

    [Fact]
    public void A_collation_keeps_its_culture_and_its_rules() {
        // Arrange
        ArticleRule english = ArticlesOf("en", "the");
        ArticleRule french  = ArticlesOf("fr", "le");

        // Act
        ListingCollation collation = new ListingCollation(German, [english, french]);

        // Assert
        Assert.Same(German, collation.Culture);
        Assert.Equal(2, collation.ArticleRules.Count);
        Assert.Contains(english, collation.ArticleRules);
        Assert.Contains(french, collation.ArticleRules);
    }

    [Fact]
    public void A_collation_may_skip_no_article_at_all() {
        // Act
        ListingCollation collation = new ListingCollation(English, []);

        // Assert
        Assert.Empty(collation.ArticleRules);
    }

    [Fact]
    public void A_collation_needs_a_culture_and_rules() {
        Assert.Throws<ArgumentNullException>(() => new ListingCollation(null!, []));
        Assert.Throws<ArgumentNullException>(() => new ListingCollation(English, null!));
    }

    [Fact]
    public void A_collation_cannot_hold_a_null_rule() {
        Assert.Throws<ArgumentException>(() => new ListingCollation(English, [null!]));
    }

    [Fact]
    public void A_collation_holds_at_most_one_rule_per_language() {
        // Arrange
        ArticleRule first  = ArticlesOf("en", "the");
        ArticleRule second = ArticlesOf("EN", "a");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ListingCollation(English, [first, second]));
    }

    // ---- the filing title ---------------------------------------------------------------------------------------

    [Fact]
    public void A_record_files_under_its_title_without_the_article_of_its_language() {
        // Arrange
        ListingCollation    collation = AnEnglishCollationSkipping("the", "a", "an");
        BibliographicRecord hobbit    = ALatinRecordTitled("The Hobbit").Generate();

        // Act
        string filing = collation.FilingTitle(hobbit);

        // Assert
        Assert.Equal("Hobbit", filing);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-GB")]
    [InlineData("en-Latn-US")]
    public void The_rule_is_chosen_by_the_primary_language_subtag(string tag) {
        // Arrange
        ListingCollation    collation = AnEnglishCollationSkipping("the");
        BibliographicRecord hobbit    = ALatinRecordTitled("The Hobbit").WithLanguage(new LanguageTag(tag)).Generate();

        // Assert
        Assert.Equal("Hobbit", collation.FilingTitle(hobbit));
    }

    [Fact]
    public void A_title_in_a_language_without_a_rule_files_as_it_is() {
        // Arrange
        ListingCollation    collation = AnEnglishCollationSkipping("the");
        BibliographicRecord lehobbit  = ALatinRecordTitled("The Hobbit").WithLanguage(new LanguageTag("fr")).Generate();

        // Assert
        Assert.Equal("The Hobbit", collation.FilingTitle(lehobbit));
    }

    [Fact]
    public void Each_language_files_by_its_own_rule() {
        // Arrange
        ListingCollation collation = new ListingCollation(English, [ArticlesOf("en", "the"), ArticlesOf("fr", "le", "la", "l'")]);

        BibliographicRecord english = ALatinRecordTitled("The Stranger").Generate();
        BibliographicRecord french  = ALatinRecordTitled("L’Étranger").WithLanguage(new LanguageTag("fr")).Generate();

        // Assert
        Assert.Equal("Stranger", collation.FilingTitle(english));
        Assert.Equal("Étranger", collation.FilingTitle(french));
    }

    [Fact]
    public void Bidirectional_controls_are_dropped_before_the_article_is_skipped() {
        // Arrange
        ListingCollation    collation = AnEnglishCollationSkipping("the");
        BibliographicRecord isolated  = ALatinRecordTitled("x").WithTitle(new Title("\u2066The\u2069 Hobbit", allowBidiControls: true)).Generate();

        // Assert
        Assert.Equal("Hobbit", collation.FilingTitle(isolated));
    }

    [Fact]
    public void Filing_needs_a_record() {
        Assert.Throws<ArgumentNullException>(() => AnEnglishCollationSkipping("the").FilingTitle(null!));
    }

    // ---- the order ----------------------------------------------------------------------------------------------

    [Fact]
    public void Titles_file_by_the_collation_of_the_culture_ignoring_case() {
        // Arrange
        ListingCollation collation = new ListingCollation(English, []);
        BibliographicRecord apple  = ALatinRecordTitled("apple").Generate();
        BibliographicRecord banana = ALatinRecordTitled("Banana").Generate();
        BibliographicRecord cherry = ALatinRecordTitled("CHERRY").Generate();

        // Assert
        Assert.True(collation.Compare(apple, banana) < 0);
        Assert.True(collation.Compare(banana, cherry) < 0);
        Assert.True(collation.Compare(cherry, apple) > 0);
    }

    [Fact]
    public void The_skipped_article_does_not_count_in_the_order() {
        // Arrange
        ListingCollation    collation = AnEnglishCollationSkipping("the");
        BibliographicRecord gone      = ALatinRecordTitled("Gone with the Wind").Generate();
        BibliographicRecord hobbit    = ALatinRecordTitled("The Hobbit").Generate();
        BibliographicRecord iliad     = ALatinRecordTitled("Iliad").Generate();

        // Act
        List<BibliographicRecord> shelf = [iliad, hobbit, gone];
        shelf.Sort(collation);

        // Assert
        Assert.Equal([gone, hobbit, iliad], shelf);
    }

    [Fact]
    public void A_letter_with_a_diaeresis_files_with_its_letter_in_german_and_after_z_in_swedish() {
        // Arrange
        BibliographicRecord apple = ALatinRecordTitled("Äpple").Generate();
        BibliographicRecord zebra = ALatinRecordTitled("Zebra").Generate();

        // Act
        int inGerman  = new ListingCollation(German, []).Compare(apple, zebra);
        int inSwedish = new ListingCollation(Swedish, []).Compare(apple, zebra);

        // Assert
        Assert.True(inGerman < 0);
        Assert.True(inSwedish > 0);
    }

    [Fact]
    public void Titles_that_file_alike_fall_to_the_subtitle_where_an_absent_one_files_first() {
        // Arrange
        ListingCollation    collation = new ListingCollation(English, []);
        BibliographicRecord bare      = ALatinRecordTitled("Dune").WithoutSubtitle().Generate();
        BibliographicRecord alpha     = ALatinRecordTitled("dune").WithSubtitle(new Title("Alpha")).Generate();
        BibliographicRecord beta      = ALatinRecordTitled("DUNE").WithSubtitle(new Title("beta")).Generate();

        // Act
        List<BibliographicRecord> shelf = [beta, alpha, bare];
        shelf.Sort(collation);

        // Assert
        Assert.Equal([bare, alpha, beta], shelf);
    }

    [Fact]
    public void Two_absent_subtitles_do_not_decide() {
        // Arrange
        ListingCollation    collation = new ListingCollation(English, []);
        BibliographicRecord earlier   = ALatinRecordTitled("Dune").WithoutSubtitle().WithPublicationYear(1965).Generate();
        BibliographicRecord later     = ALatinRecordTitled("Dune").WithoutSubtitle().WithPublicationYear(1984).Generate();

        // Assert
        Assert.True(collation.Compare(earlier, later) < 0);
    }

    [Fact]
    public void Bidirectional_controls_in_a_subtitle_do_not_decide() {
        // Arrange
        ListingCollation    collation = new ListingCollation(English, []);
        BibliographicRecord plain     = ALatinRecordTitled("Dune").WithSubtitle(new Title("A novel")).WithPublicationYear(1965).Generate();
        BibliographicRecord marked    = ALatinRecordTitled("Dune").WithSubtitle(new Title("A \u200Enovel", allowBidiControls: true)).WithPublicationYear(1984).Generate();

        // Assert
        Assert.True(collation.Compare(plain, marked) < 0);
    }

    [Fact]
    public void Then_the_year_the_edition_and_the_id_decide_in_that_order() {
        // Arrange
        ListingCollation collation = new ListingCollation(English, []);

        AnyBibliographicRecord alike = ALatinRecordTitled("Dune").WithoutSubtitle().WithPublicationYear(1990).WithEdition(2).WithId(new RecordId("AA00000050"));

        BibliographicRecord olderYear   = alike.WithPublicationYear(1989).WithEdition(9).WithId(new RecordId("ZZ00000099")).Generate();
        BibliographicRecord lowerEdition = alike.WithEdition(1).WithId(new RecordId("ZZ00000099")).Generate();
        BibliographicRecord reference       = alike.Generate();
        BibliographicRecord lowerId     = alike.WithId(new RecordId("AA00000049")).Generate();
        BibliographicRecord higherId    = alike.WithId(new RecordId("AB00000001")).Generate();

        // Assert
        Assert.True(collation.Compare(olderYear, lowerEdition) < 0);
        Assert.True(collation.Compare(lowerEdition, reference) < 0);
        Assert.True(collation.Compare(lowerId, reference) < 0);
        Assert.True(collation.Compare(reference, higherId) < 0);
    }

    [Fact]
    public void The_ids_are_compared_by_ordinal_value() {
        // Arrange
        ListingCollation    collation = new ListingCollation(English, []);
        AnyBibliographicRecord alike  = ALatinRecordTitled("Dune").WithoutSubtitle().WithPublicationYear(1990).WithEdition(2);
        BibliographicRecord b         = alike.WithId(new RecordId("BA00000001")).Generate();
        BibliographicRecord z         = alike.WithId(new RecordId("ZA00000001")).Generate();

        // Assert
        Assert.True(collation.Compare(b, z) < 0);
    }

    [Fact]
    public void A_record_is_compared_equal_to_itself() {
        // Arrange
        ListingCollation    collation = AnEnglishCollationSkipping("the");
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();

        // Assert
        Assert.Equal(0, collation.Compare(anyRecord, anyRecord));
    }

    [Fact]
    public void Nothing_files_before_anything() {
        // Arrange
        ListingCollation    collation = AnEnglishCollationSkipping("the");
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();

        // Assert
        Assert.True(collation.Compare(null, anyRecord) < 0);
        Assert.True(collation.Compare(anyRecord, null) > 0);
        Assert.Equal(0, collation.Compare(null, null));
    }

    // ---- sort keys ----------------------------------------------------------------------------------------------

    [Fact]
    public void Titles_that_file_alike_have_the_same_sort_key() {
        // Arrange
        ListingCollation    collation = AnEnglishCollationSkipping("the");
        BibliographicRecord withArticle    = ALatinRecordTitled("The Hobbit").Generate();
        BibliographicRecord withoutArticle = ALatinRecordTitled("HOBBIT").Generate();

        // Act
        int comparison = SortKey.Compare(collation.SortKeyOf(withArticle), collation.SortKeyOf(withoutArticle));

        // Assert
        Assert.Equal(0, comparison);
    }

    [Fact]
    public void Sort_keys_order_like_the_collation() {
        // Arrange
        ListingCollation    collation = new ListingCollation(Swedish, []);
        BibliographicRecord apple     = ALatinRecordTitled("Äpple").Generate();
        BibliographicRecord zebra     = ALatinRecordTitled("Zebra").Generate();

        // Assert
        Assert.True(SortKey.Compare(collation.SortKeyOf(zebra), collation.SortKeyOf(apple)) < 0);
    }

}
