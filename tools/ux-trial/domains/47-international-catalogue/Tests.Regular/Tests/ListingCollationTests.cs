using System.Globalization;

using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class ListingCollationTests {

    private static readonly CultureInfo French  = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly CultureInfo German  = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");

    private static ArticleRule EnglishArticles() => new ArticleRule(new LanguageTag("en"), ["the", "a", "an"]);

    private static ArticleRule FrenchArticles() => new ArticleRule(new LanguageTag("fr"), ["le", "la", "les", "l'"]);

    private static BibliographicRecord Latin(string title, string language = "en") {
        return Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title(title)).WithLanguage(new LanguageTag(language)).Generate();
    }

    private static List<string> Sorted(ListingCollation collation, params BibliographicRecord[] records) {
        List<BibliographicRecord> list = records.ToList();
        list.Sort(collation);
        return list.Select(record => record.Title.Value).ToList();
    }

    [Fact]
    public void Two_rules_for_one_language_are_refused() {
        // Arrange
        CultureInfo anyCulture = Any.OneOf(French, German, Swedish).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ListingCollation(anyCulture, [EnglishArticles(), new ArticleRule(new LanguageTag("EN"), ["ye"])]));

        // Assert
        Assert.Equal("articleRules", refusal.ParamName);
    }

    [Fact]
    public void Rules_for_different_languages_are_kept() {
        // Act
        ListingCollation collation = new ListingCollation(French, [EnglishArticles(), FrenchArticles()]);

        // Assert
        Assert.Equal(2, collation.ArticleRules.Count);
        Assert.Same(French, collation.Culture);
    }

    [Fact]
    public void A_null_culture_list_or_rule_is_refused() {
        // Arrange
        CultureInfo anyCulture = Any.OneOf(French, German, Swedish).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ListingCollation(null!, []));
        Assert.Throws<ArgumentNullException>(() => new ListingCollation(anyCulture, null!));
        Assert.Throws<ArgumentException>(() => new ListingCollation(anyCulture, [EnglishArticles(), null!]));
    }

    [Fact]
    public void A_collation_without_rules_files_a_title_as_it_is_written() {
        // Arrange
        ListingCollation collation = new ListingCollation(CultureInfo.InvariantCulture, []);

        // Assert
        Assert.Equal("The Hobbit", collation.FilingTitle(Latin("The Hobbit")));
    }

    [Fact]
    public void The_filing_title_drops_the_article_of_the_primary_language_subtag() {
        // Arrange
        ListingCollation collation = new ListingCollation(French, [EnglishArticles(), FrenchArticles()]);

        // Assert
        Assert.Equal("Hobbit", collation.FilingTitle(Latin("The Hobbit", "en")));
        Assert.Equal("Hobbit", collation.FilingTitle(Latin("The Hobbit", "en-GB")));
        Assert.Equal("Hobbit", collation.FilingTitle(Latin("The Hobbit", "en-Latn-US")));
        Assert.Equal("étranger", collation.FilingTitle(Latin("L'étranger", "fr-CA")));
    }

    [Fact]
    public void The_rule_of_the_language_not_of_the_text_decides() {
        // Arrange
        ListingCollation collation = new ListingCollation(French, [EnglishArticles(), FrenchArticles()]);

        // Assert
        Assert.Equal("The Hobbit", collation.FilingTitle(Latin("The Hobbit", "fr")));
        Assert.Equal("The Hobbit", collation.FilingTitle(Latin("The Hobbit", "de")));
        Assert.Equal("Le Hobbit", collation.FilingTitle(Latin("Le Hobbit", "en")));
    }

    [Fact]
    public void The_filing_title_has_no_bidirectional_controls() {
        // Arrange
        Title marked = new Title("\u200Fשלום\u200F עליכם", allowBidiControls: true);
        BibliographicRecord record = Records.Unnumbered().WithScript(WritingScript.Hebrew).WithTitle(marked).Generate();
        ListingCollation collation = new ListingCollation(CultureInfo.InvariantCulture, []);

        // Act
        string filing = collation.FilingTitle(record);

        // Assert
        Assert.Equal("שלום עליכם", filing);
    }

    [Fact]
    public void A_title_made_of_an_article_alone_files_under_itself() {
        // Arrange
        ListingCollation collation = new ListingCollation(CultureInfo.InvariantCulture, [EnglishArticles()]);

        // Assert
        Assert.Equal("The", collation.FilingTitle(Latin("The")));
    }

    [Fact]
    public void Records_are_sorted_ignoring_case() {
        // Arrange
        ListingCollation collation = new ListingCollation(CultureInfo.InvariantCulture, []);

        // Act
        List<string> sorted = Sorted(collation, Latin("banana"), Latin("Apple"), Latin("cherry"), Latin("APRICOT"));

        // Assert
        Assert.Equal(["Apple", "APRICOT", "banana", "cherry"], sorted);
    }

    [Fact]
    public void An_article_does_not_decide_the_place_of_a_title() {
        // Arrange
        ListingCollation collation = new ListingCollation(CultureInfo.InvariantCulture, [EnglishArticles()]);

        // Act
        List<string> sorted = Sorted(collation, Latin("The Great Gatsby"), Latin("A Farewell to Arms"), Latin("Hamlet"), Latin("An Inspector Calls"));

        // Assert
        Assert.Equal(["A Farewell to Arms", "The Great Gatsby", "Hamlet", "An Inspector Calls"], sorted);
    }

    [Fact]
    public void Without_the_rule_the_article_is_part_of_the_sort_key() {
        // Arrange
        ListingCollation collation = new ListingCollation(CultureInfo.InvariantCulture, []);

        // Act
        List<string> sorted = Sorted(collation, Latin("The Great Gatsby"), Latin("A Farewell to Arms"), Latin("Hamlet"));

        // Assert
        Assert.Equal(["A Farewell to Arms", "Hamlet", "The Great Gatsby"], sorted);
    }

    [Fact]
    public void The_culture_decides_where_an_accented_letter_goes() {
        // Arrange
        BibliographicRecord zebra = Latin("Zebra");
        BibliographicRecord aeppl = Latin("Äpple");
        BibliographicRecord apple = Latin("Apple");

        // Act
        List<string> inGerman  = Sorted(new ListingCollation(German, []), zebra, aeppl, apple);
        List<string> inSwedish = Sorted(new ListingCollation(Swedish, []), zebra, aeppl, apple);

        // Assert
        Assert.Equal(["Apple", "Äpple", "Zebra"], inGerman);
        Assert.Equal(["Apple", "Zebra", "Äpple"], inSwedish);
    }

    [Fact]
    public void Accents_are_secondary_to_letters() {
        // Arrange
        ListingCollation collation = new ListingCollation(French, []);

        // Act
        List<string> sorted = Sorted(collation, Latin("Zèbre"), Latin("Eve"), Latin("Étude"), Latin("Apple"));

        // Assert
        Assert.Equal(["Apple", "Étude", "Eve", "Zèbre"], sorted);
    }

    [Fact]
    public void Titles_equal_under_the_collation_are_ordered_by_subtitle_with_none_first() {
        // Arrange
        Title anyTitle = Any.Title().Generate();
        AnyBibliographicRecord same = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(anyTitle).WithLanguage(new LanguageTag("fr"));
        BibliographicRecord bare  = same.WithoutSubtitle().Generate();
        BibliographicRecord alpha = same.WithSubtitle(new Title("alpha")).Generate();
        BibliographicRecord beta  = same.WithSubtitle(new Title("Beta")).Generate();
        ListingCollation collation = new ListingCollation(French, []);

        // Act
        List<BibliographicRecord> sorted = new[] { beta, alpha, bare }.OrderBy(record => record, collation).ToList();

        // Assert
        Assert.Equal([bare.Id, alpha.Id, beta.Id], sorted.Select(record => record.Id));
    }

    [Fact]
    public void Then_by_year_then_edition_then_id() {
        // Arrange
        Title anyTitle = Any.Title().Generate();
        AnyBibliographicRecord same = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(anyTitle).WithoutSubtitle().WithLanguage(new LanguageTag("fr"));
        BibliographicRecord y1990      = same.WithPublicationYear(1990).WithEdition(5).Generate();
        BibliographicRecord y2000e1    = same.WithPublicationYear(2000).WithEdition(1).Generate();
        BibliographicRecord y2000e2a   = same.WithPublicationYear(2000).WithEdition(2).WithId(new RecordId("AA00000001")).Generate();
        BibliographicRecord y2000e2b   = same.WithPublicationYear(2000).WithEdition(2).WithId(new RecordId("AA00000002")).Generate();
        ListingCollation collation = new ListingCollation(French, []);

        // Act
        List<BibliographicRecord> sorted = new[] { y2000e2b, y2000e1, y2000e2a, y1990 }.OrderBy(record => record, collation).ToList();

        // Assert
        Assert.Equal([y1990, y2000e1, y2000e2a, y2000e2b], sorted);
    }

    [Fact]
    public void Titles_that_differ_only_by_an_article_the_rule_does_not_know_are_not_tied() {
        // Arrange
        ListingCollation collation = new ListingCollation(French, [EnglishArticles()]);
        BibliographicRecord withArticle = Latin("The Hobbit", "en");
        BibliographicRecord without     = Latin("Hobbit", "en");

        // Act
        int comparison = collation.Compare(withArticle, without);

        // Assert: both file under "Hobbit"; the tie falls to the subtitle, year, edition and id, never to 0 for distinct records.
        Assert.NotEqual(0, comparison);
    }

    [Fact]
    public void Comparing_a_record_with_itself_or_with_nothing() {
        // Arrange
        ListingCollation collation = new ListingCollation(French, []);
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();

        // Assert
        Assert.Equal(0, collation.Compare(anyRecord, anyRecord));
        Assert.Equal(0, collation.Compare(null, null));
        Assert.True(collation.Compare(null, anyRecord) < 0);
        Assert.True(collation.Compare(anyRecord, null) > 0);
    }

    [Fact]
    public void The_comparison_is_antisymmetric() {
        // Arrange
        ListingCollation collation = new ListingCollation(Swedish, [EnglishArticles(), FrenchArticles()]);
        BibliographicRecord first  = Any.BibliographicRecord().Generate();
        BibliographicRecord second = Any.BibliographicRecord().WithId(Any.RecordId().DifferentFrom(first.Id).Generate()).Generate();

        // Act
        int forward  = collation.Compare(first, second);
        int backward = collation.Compare(second, first);

        // Assert
        Assert.Equal(Math.Sign(forward), -Math.Sign(backward));
    }

    [Fact]
    public void The_sort_key_ignores_case_and_follows_the_filing_title() {
        // Arrange
        ListingCollation collation = new ListingCollation(CultureInfo.InvariantCulture, [EnglishArticles()]);
        BibliographicRecord lower   = Latin("the hobbit");
        BibliographicRecord upper   = Latin("HOBBIT");
        BibliographicRecord another = Latin("Iliad");

        // Act & Assert
        Assert.Equal(0, System.Globalization.SortKey.Compare(collation.SortKeyOf(lower), collation.SortKeyOf(upper)));
        Assert.True(System.Globalization.SortKey.Compare(collation.SortKeyOf(upper), collation.SortKeyOf(another)) < 0);
    }

}
