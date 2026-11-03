using System.Globalization;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class ListingCollationTests {

    private static readonly ArticleRule English = new ArticleRule(new LanguageTag("en"), ["the", "a", "an"]);

    private static readonly ArticleRule French = new ArticleRule(new LanguageTag("fr"), ["le", "la", "les", "l'"]);

    private static readonly ArticleRule German = new ArticleRule(new LanguageTag("de"), ["der", "die", "das"]);

    private static ListingCollation Collation(string culture, params ArticleRule[] rules) {
        return new ListingCollation(CultureInfo.GetCultureInfo(culture), rules);
    }

    private static AnyBibliographicRecord Record(string title, string language = "en") {
        return Any.BibliographicRecord().WithTitle(new Title(title)).WithLanguage(new LanguageTag(language));
    }

    [Fact]
    public void A_collation_needs_a_culture_and_a_list_of_rules() {
        Assert.Throws<ArgumentNullException>(() => new ListingCollation(null!, []));
        Assert.Throws<ArgumentNullException>(() => new ListingCollation(CultureInfo.InvariantCulture, null!));
    }

    [Fact]
    public void A_collation_refuses_a_null_rule() {
        Assert.Throws<ArgumentException>(() => new ListingCollation(CultureInfo.InvariantCulture, [English, null!]));
    }

    [Fact]
    public void A_collation_refuses_two_rules_for_one_language() {
        // Arrange
        ArticleRule anotherEnglish = new ArticleRule(new LanguageTag("en"), ["thy"]);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ListingCollation(CultureInfo.InvariantCulture, [English, anotherEnglish]));
        Assert.Throws<ArgumentException>(() => new ListingCollation(CultureInfo.InvariantCulture, [English, English]));
    }

    [Fact]
    public void A_collation_accepts_one_rule_per_language_and_none() {
        // Act
        ListingCollation none = new ListingCollation(CultureInfo.InvariantCulture, []);
        ListingCollation several = new ListingCollation(CultureInfo.InvariantCulture, [English, French, German]);

        // Assert
        Assert.Empty(none.ArticleRules);
        Assert.Equal(3, several.ArticleRules.Count);
    }

    [Fact]
    public void The_filing_title_skips_the_leading_article_of_the_records_language() {
        // Arrange
        ListingCollation collation = Collation("en-US", English, French);
        BibliographicRecord english = Record("The Hobbit", "en").Generate();
        BibliographicRecord french = Record("Le Petit Prince", "fr").Generate();

        // Act & Assert
        Assert.Equal("Hobbit", collation.FilingTitle(english));
        Assert.Equal("Petit Prince", collation.FilingTitle(french));
    }

    [Fact]
    public void The_rule_follows_the_primary_language_subtag_of_the_record() {
        // Arrange
        ListingCollation collation = Collation("en-US", French);
        BibliographicRecord canadian = Record("Les Belles-sœurs", "fr-CA").Generate();
        BibliographicRecord swiss = Record("Les Belles-sœurs", "fr-Latn-CH").Generate();

        // Act & Assert
        Assert.Equal("Belles-sœurs", collation.FilingTitle(canadian));
        Assert.Equal("Belles-sœurs", collation.FilingTitle(swiss));
    }

    [Fact]
    public void A_rule_of_another_language_is_not_applied() {
        // Arrange
        ListingCollation collation = Collation("en-US", English);
        BibliographicRecord french = Record("The Hobbit", "fr").Generate();
        BibliographicRecord withoutRule = Record("The Hobbit", "de").Generate();

        // Act & Assert
        Assert.Equal("The Hobbit", collation.FilingTitle(french));
        Assert.Equal("The Hobbit", collation.FilingTitle(withoutRule));
    }

    [Fact]
    public void The_filing_title_drops_bidirectional_controls() {
        // Arrange
        ListingCollation collation = Collation("en-US", English);
        BibliographicRecord arabic = Any.BibliographicRecord()
                                        .WithTitle(new Title("\u200Fكتاب \u2067الأمير\u2069", allowBidiControls: true))
                                        .WithLanguage(new LanguageTag("ar"))
                                        .Generate();

        // Act
        string filingTitle = collation.FilingTitle(arabic);

        // Assert
        Assert.Equal("كتاب الأمير", filingTitle);
    }

    [Fact]
    public void The_filing_title_of_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Collation("en-US").FilingTitle(null!));
    }

    [Fact]
    public void Titles_are_ordered_by_filing_title_whatever_their_case() {
        // Arrange
        ListingCollation collation = Collation("en-US", English);
        BibliographicRecord banana = Record("banana").Generate();
        BibliographicRecord cherry = Record("Cherry").Generate();
        BibliographicRecord theApple = Record("The Apple").Generate();

        // Act
        List<BibliographicRecord> sorted = new List<BibliographicRecord> { cherry, banana, theApple }.OrderBy(r => r, collation).ToList();

        // Assert
        Assert.Equal([theApple, banana, cherry], sorted);
    }

    [Fact]
    public void Titles_that_differ_only_by_case_fall_to_the_following_keys() {
        // Arrange
        ListingCollation collation = Collation("en-US");
        BibliographicRecord upper = Record("ANNUAL").WithoutSubtitle().WithPublicationYear(2005).Generate();
        BibliographicRecord lower = Record("annual").WithoutSubtitle().WithPublicationYear(2001).Generate();

        // Act & Assert
        Assert.True(collation.Compare(lower, upper) < 0);
        Assert.True(collation.Compare(upper, lower) > 0);
    }

    [Fact]
    public void A_record_without_a_subtitle_comes_before_one_with() {
        // Arrange
        ListingCollation collation = Collation("en-US");
        BibliographicRecord bare = Record("Poems").WithoutSubtitle().WithPublicationYear(2010).Generate();
        BibliographicRecord subtitled = Record("Poems").WithSubtitle(new Title("Collected")).WithPublicationYear(1990).Generate();

        // Act & Assert
        Assert.True(collation.Compare(bare, subtitled) < 0);
        Assert.True(collation.Compare(subtitled, bare) > 0);
    }

    [Fact]
    public void Subtitles_are_collated_like_titles() {
        // Arrange
        ListingCollation collation = Collation("en-US");
        BibliographicRecord alpha = Record("Poems").WithSubtitle(new Title("alpha")).Generate();
        BibliographicRecord beta = Record("Poems").WithSubtitle(new Title("Beta")).Generate();

        // Act & Assert
        Assert.True(collation.Compare(alpha, beta) < 0);
    }

    [Fact]
    public void Then_the_year_then_the_edition_then_the_id_break_the_ties() {
        // Arrange
        ListingCollation collation = Collation("en-US");
        AnyBibliographicRecord same = Record("Poems").WithoutSubtitle().WithPublicationYear(2000).WithEdition(2).WithId(new RecordId("FR00000010"));
        BibliographicRecord earlier = same.WithPublicationYear(1999).Generate();
        BibliographicRecord later = same.Generate();
        BibliographicRecord firstEdition = same.WithEdition(1).Generate();
        BibliographicRecord lowerId = same.WithId(new RecordId("FR00000009")).Generate();

        // Act & Assert
        Assert.True(collation.Compare(earlier, later) < 0);
        Assert.True(collation.Compare(firstEdition, later) < 0);
        Assert.True(collation.Compare(lowerId, later) < 0);
        Assert.True(collation.Compare(later, lowerId) > 0);
    }

    [Fact]
    public void Ids_are_compared_ordinally() {
        // Arrange
        ListingCollation collation = Collation("en-US");
        AnyBibliographicRecord same = Record("Poems").WithoutSubtitle().WithPublicationYear(2000).WithEdition(2);
        BibliographicRecord upperAgency = same.WithId(new RecordId("ZA00000001")).Generate();
        BibliographicRecord otherAgency = same.WithId(new RecordId("AZ00000001")).Generate();

        // Act & Assert
        Assert.True(collation.Compare(otherAgency, upperAgency) < 0);
    }

    [Fact]
    public void A_listing_does_not_depend_on_the_order_the_records_were_added_in() {
        // Arrange: four titles that all file as "cat", told apart only by their ids.
        ListingCollation collation = Collation("en-US", English);
        AnyBibliographicRecord cat = Any.BibliographicRecord().WithLanguage(new LanguageTag("en")).WithoutSubtitle().WithPublicationYear(2001).WithEdition(1).WithContributors().WithoutIsbn().WithoutIssn();
        BibliographicRecord one = cat.WithTitle(new Title("The Cat")).WithId(new RecordId("FR00000001")).Generate();
        BibliographicRecord two = cat.WithTitle(new Title("cat")).WithId(new RecordId("FR00000002")).Generate();
        BibliographicRecord three = cat.WithTitle(new Title("A CAT")).WithId(new RecordId("FR00000003")).Generate();
        BibliographicRecord four = cat.WithTitle(new Title("CAT")).WithId(new RecordId("FR00000004")).Generate();

        // Act
        List<BibliographicRecord> forward = new[] { one, two, three, four }.OrderBy(r => r, collation).ToList();
        List<BibliographicRecord> backward = new[] { four, three, two, one }.OrderBy(r => r, collation).ToList();
        List<BibliographicRecord> shuffled = new[] { three, one, four, two }.OrderBy(r => r, collation).ToList();

        // Assert
        Assert.Equal([one, two, three, four], forward);
        Assert.Equal(forward, backward);
        Assert.Equal(forward, shuffled);
    }

    [Fact]
    public void A_record_compares_equal_to_itself_and_nulls_come_first() {
        // Arrange
        ListingCollation collation = Collation("en-US");
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();

        // Act & Assert
        Assert.Equal(0, collation.Compare(anyRecord, anyRecord));
        Assert.Equal(0, collation.Compare(null, null));
        Assert.True(collation.Compare(null, anyRecord) < 0);
        Assert.True(collation.Compare(anyRecord, null) > 0);
    }

    [Fact]
    public void The_culture_decides_where_accented_letters_file() {
        // Arrange
        BibliographicRecord zebra = Record("Zebra", "sv").Generate();
        BibliographicRecord ox = Record("Ödla", "sv").Generate();

        // Act
        int swedish = Collation("sv-SE").Compare(zebra, ox);
        int english = Collation("en-US").Compare(zebra, ox);

        // Assert: Swedish files Ö after Z, English files it with O.
        Assert.True(swedish < 0, "in Swedish, Ödla files after Zebra");
        Assert.True(english > 0, "in English, Ödla files before Zebra");
    }

    [Fact]
    public void Spanish_files_n_tilde_as_a_letter_after_n() {
        // Arrange
        BibliographicRecord anzuelo = Record("Anzuelo", "es").Generate();
        BibliographicRecord anil = Record("Añil", "es").Generate();

        // Act
        int spanish = Collation("es-ES").Compare(anil, anzuelo);
        int english = Collation("en-US").Compare(anil, anzuelo);

        // Assert
        Assert.True(spanish > 0, "in Spanish, Añil files after Anzuelo");
        Assert.True(english < 0, "in English, Añil files as Anil, before Anzuelo");
    }

    [Fact]
    public void The_sort_key_of_titles_that_differ_only_by_case_is_the_same() {
        // Arrange
        ListingCollation collation = Collation("fr-FR", French);
        BibliographicRecord lower = Record("le petit prince", "fr").Generate();
        BibliographicRecord upper = Record("LE PETIT PRINCE", "fr").Generate();

        // Act & Assert
        Assert.Equal(0, System.Globalization.SortKey.Compare(collation.SortKeyOf(lower), collation.SortKeyOf(upper)));
    }

    [Fact]
    public void Sort_keys_order_titles_as_the_comparison_does() {
        // Arrange
        ListingCollation collation = Collation("fr-FR", French);
        BibliographicRecord apple = Record("Pomme", "fr").Generate();
        BibliographicRecord prince = Record("Le Petit Prince", "fr").Generate();
        BibliographicRecord zebra = Record("Zèbre", "fr").Generate();

        // Act
        List<BibliographicRecord> byKey = new[] { zebra, apple, prince }.OrderBy(r => collation.SortKeyOf(r), Comparer<System.Globalization.SortKey>.Create(System.Globalization.SortKey.Compare)).ToList();

        // Assert
        Assert.Equal([prince, apple, zebra], byKey);
    }

    [Fact]
    public void A_listing_of_titles_in_several_scripts_follows_the_cultures_script_order() {
        // Arrange
        ListingCollation collation = Collation("en-US");
        BibliographicRecord latin = Record("Alpha").Generate();
        BibliographicRecord greek = Any.BibliographicRecord().WithTitle(new Title("Άλφα")).WithLanguage(new LanguageTag("el")).Generate();
        BibliographicRecord cyrillic = Any.BibliographicRecord().WithTitle(new Title("Азбука")).WithLanguage(new LanguageTag("ru")).Generate();

        // Act
        List<BibliographicRecord> sorted = new[] { cyrillic, greek, latin }.OrderBy(r => r, collation).ToList();

        // Assert: Latin, then Greek, then Cyrillic in the standard collation.
        Assert.Equal([latin, greek, cyrillic], sorted);
    }

}
