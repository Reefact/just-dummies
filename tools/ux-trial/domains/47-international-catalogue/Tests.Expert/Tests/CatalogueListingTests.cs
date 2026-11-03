using System.Globalization;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class CatalogueListingTests {

    private static readonly ListingCollation EnglishSkippingThe = new ListingCollation(new CultureInfo("en-US"), [new ArticleRule(new LanguageTag("en"), ["the"])]);

    private static AnyBibliographicRecord ARecordTitled(string title, WritingScript script = WritingScript.Latin) {
        return Any.BibliographicRecord().WithScript(script).WithTitle(new Title(title)).WithLanguage(new LanguageTag("en")).WithoutIsbn().WithoutIssn();
    }

    private static Catalogue ACatalogueHolding(params BibliographicRecord[] records) {
        Catalogue catalogue = Any.Catalogue().Generate();
        foreach (BibliographicRecord record in records) {
            catalogue.Add(record);
        }

        return catalogue;
    }

    [Fact]
    public void A_listing_is_sorted_by_the_collation() {
        // Arrange
        BibliographicRecord iliad  = ARecordTitled("Iliad").Generate();
        BibliographicRecord hobbit = ARecordTitled("The Hobbit").Generate();
        BibliographicRecord emma   = ARecordTitled("Emma").Generate();
        Catalogue catalogue = ACatalogueHolding(iliad, hobbit, emma);

        // Act
        IReadOnlyList<BibliographicRecord> listing = catalogue.Listing(EnglishSkippingThe);

        // Assert
        Assert.Equal([emma, hobbit, iliad], listing);
    }

    [Fact]
    public void A_listing_of_an_empty_catalogue_is_empty() {
        Assert.Empty(Any.Catalogue().Generate().Listing(EnglishSkippingThe));
    }

    [Fact]
    public void A_listing_may_be_limited_to_one_script() {
        // Arrange
        BibliographicRecord emma     = ARecordTitled("Emma").Generate();
        BibliographicRecord war      = ARecordTitled("Война и мир", WritingScript.Cyrillic).Generate();
        BibliographicRecord anna     = ARecordTitled("Анна Каренина", WritingScript.Cyrillic).Generate();
        BibliographicRecord tokyo    = ARecordTitled("東京物語", WritingScript.Cjk).Generate();
        Catalogue catalogue = ACatalogueHolding(emma, war, anna, tokyo);

        // Act
        IReadOnlyList<BibliographicRecord> cyrillic = catalogue.Listing(EnglishSkippingThe, WritingScript.Cyrillic);

        // Assert
        Assert.Equal(2, cyrillic.Count);
        Assert.Contains(war, cyrillic);
        Assert.Contains(anna, cyrillic);
        Assert.Empty(catalogue.Listing(EnglishSkippingThe, WritingScript.Greek));
    }

    [Fact]
    public void A_listing_needs_a_collation_and_a_defined_script() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => catalogue.Listing(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => catalogue.Listing(EnglishSkippingThe, (WritingScript)99));
        Assert.Throws<ArgumentNullException>(() => catalogue.RenderListing(null!));
    }

    [Fact]
    public void A_listing_does_not_disturb_the_catalogue() {
        // Arrange
        BibliographicRecord iliad = ARecordTitled("Iliad").Generate();
        BibliographicRecord emma  = ARecordTitled("Emma").Generate();
        Catalogue catalogue = ACatalogueHolding(iliad, emma);

        // Act
        catalogue.Listing(EnglishSkippingThe);

        // Assert
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void A_listing_is_rendered_one_line_per_record_with_the_first_contributor() {
        // Arrange
        Contributor tolstoi = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
        Contributor garnett = new Contributor(new PersonName("Garnett", "Constance"), ContributorRole.Translator);
        Contributor austen  = new Contributor(new PersonName("Austen"), ContributorRole.Author);

        BibliographicRecord anna = ARecordTitled("Anna Karenina").WithContributors([tolstoi, garnett]).WithPublicationYear(1901).Generate();
        BibliographicRecord emma = ARecordTitled("The Emma").WithContributors([austen]).WithPublicationYear(1815).Generate();
        Catalogue catalogue = ACatalogueHolding(emma, anna);

        // Act
        string rendered = catalogue.RenderListing(EnglishSkippingThe);

        // Assert
        Assert.Equal("Anna Karenina / Tolstoï, Léon (1901)\nEmma / Austen (1815)\n", rendered);
    }

    [Fact]
    public void A_record_without_contributor_is_rendered_without_the_slash() {
        // Arrange
        BibliographicRecord anonymous = ARecordTitled("Beowulf").WithContributors([]).WithPublicationYear(1999).Generate();
        Catalogue catalogue = ACatalogueHolding(anonymous);

        // Assert
        Assert.Equal("Beowulf (1999)\n", catalogue.RenderListing(EnglishSkippingThe));
    }

    [Fact]
    public void A_cjk_contributor_is_rendered_family_name_first_with_no_separator() {
        // Arrange
        Contributor natsume = new Contributor(new PersonName("夏目", "漱石"), ContributorRole.Author);

        BibliographicRecord kokoro = ARecordTitled("こころ", WritingScript.Cjk).WithLanguage(new LanguageTag("ja")).WithContributors([natsume]).WithPublicationYear(1914).Generate();
        Catalogue catalogue = ACatalogueHolding(kokoro);

        // Assert
        Assert.Equal("こころ / 夏目漱石 (1914)\n", catalogue.RenderListing(EnglishSkippingThe));
    }

    [Fact]
    public void An_empty_catalogue_renders_nothing() {
        Assert.Equal(string.Empty, Any.Catalogue().Generate().RenderListing(EnglishSkippingThe));
    }

    [Fact]
    public void The_rendered_listing_may_be_limited_to_one_script() {
        // Arrange
        Contributor austen  = new Contributor(new PersonName("Austen"), ContributorRole.Author);
        Contributor tolstoi = new Contributor(new PersonName("Толстой", "Лев"), ContributorRole.Author);

        BibliographicRecord emma = ARecordTitled("Emma").WithContributors([austen]).WithPublicationYear(1815).Generate();
        BibliographicRecord war  = ARecordTitled("Война и мир", WritingScript.Cyrillic).WithLanguage(new LanguageTag("ru")).WithContributors([tolstoi]).WithPublicationYear(1869).Generate();
        Catalogue catalogue = ACatalogueHolding(emma, war);

        // Assert
        Assert.Equal("Война и мир / Толстой, Лев (1869)\n", catalogue.RenderListing(EnglishSkippingThe, WritingScript.Cyrillic));
    }

    [Fact]
    public void The_rendered_title_is_the_filing_title() {
        // Arrange
        BibliographicRecord hobbit = ARecordTitled("The Hobbit").WithContributors([]).WithPublicationYear(1937).Generate();
        Catalogue catalogue = ACatalogueHolding(hobbit);

        // Assert
        Assert.Equal("Hobbit (1937)\n", catalogue.RenderListing(EnglishSkippingThe));
    }

}
