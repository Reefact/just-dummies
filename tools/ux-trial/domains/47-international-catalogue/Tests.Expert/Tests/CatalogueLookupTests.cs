using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class CatalogueLookupTests {

    private static readonly Isbn13 Isbn978 = new Isbn13("978-0-306-40615-7");
    private static readonly Isbn13 Isbn979 = new Isbn13("979-10-90636-07-1");

    private static Catalogue ACatalogueHolding(params BibliographicRecord[] records) {
        Catalogue catalogue = Any.Catalogue().Generate();
        foreach (BibliographicRecord record in records) {
            catalogue.Add(record);
        }

        return catalogue;
    }

    // ---- withdrawing --------------------------------------------------------------------------------------------

    [Fact]
    public void A_withdrawn_record_is_no_longer_held() {
        // Arrange
        BibliographicRecord record    = Any.BibliographicRecord().Generate();
        BibliographicRecord other     = Any.BibliographicRecord().Generate();
        Catalogue           catalogue = ACatalogueHolding(record, other);

        // Act
        catalogue.Withdraw(record.Id);

        // Assert
        Assert.Equal(1, catalogue.Count);
        Assert.Null(catalogue.Find(record.Id));
        Assert.Same(other, Assert.Single(catalogue.Records()));
    }

    [Fact]
    public void A_withdrawn_id_is_retired_and_names_itself_when_a_record_tries_to_reuse_it() {
        // Arrange
        BibliographicRecord record    = Any.BibliographicRecord().Generate();
        Catalogue           catalogue = ACatalogueHolding(record);
        catalogue.Withdraw(record.Id);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(Any.BibliographicRecord().WithId(record.Id).Generate()));

        // Assert
        Assert.Equal(DuplicateReason.RetiredRecordId, refusal.Reason);
        Assert.Equal(record.Id, refusal.Existing);
    }

    [Fact]
    public void A_withdrawn_record_can_be_added_back_under_a_new_id() {
        // Arrange
        BibliographicRecord record    = Any.BibliographicRecord().Generate();
        Catalogue           catalogue = ACatalogueHolding(record);
        catalogue.Withdraw(record.Id);
        BibliographicRecord again = Any.BibliographicRecord().DescribingTheSameWorkAs(record).Generate();

        // Act
        catalogue.Add(again);

        // Assert
        Assert.Same(again, catalogue.Find(again.Id));
    }

    [Fact]
    public void A_withdrawn_isbn_can_be_used_again() {
        // Arrange
        BibliographicRecord record    = Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        Catalogue           catalogue = ACatalogueHolding(record);
        catalogue.Withdraw(record.Id);

        // Assert
        Assert.Equal(DuplicateReason.None, catalogue.CheckDuplicate(Any.BibliographicRecord().WithTitle(new Title("Emma")).WithPublicationYear(1995).WithIsbn(Isbn978).Generate()));
    }

    [Fact]
    public void Only_a_current_record_can_be_withdrawn() {
        // Arrange
        BibliographicRecord record    = Any.BibliographicRecord().Generate();
        RecordId            unknown   = Any.RecordId().Generate();
        Catalogue           catalogue = ACatalogueHolding(record);
        catalogue.Withdraw(record.Id);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalogue.Withdraw(unknown));
        Assert.Throws<InvalidOperationException>(() => catalogue.Withdraw(record.Id));
    }

    [Fact]
    public void A_record_absorbed_by_a_merge_cannot_be_withdrawn() {
        // Arrange
        Contributor tolstoi = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
        Contributor garnett = new Contributor(new PersonName("Garnett", "Constance"), ContributorRole.Translator);

        BibliographicRecord survivor = Any.BibliographicRecord().WithoutIsbn().WithoutIssn().WithContributors([tolstoi]).Generate();
        BibliographicRecord absorbed = Any.BibliographicRecord().DescribingTheSameWorkAs(survivor).WithoutIssn().WithContributors([garnett]).Generate();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);
        catalogue.Merge(survivor.Id, absorbed.Id);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalogue.Withdraw(absorbed.Id));

        // Assert
        Assert.Contains(survivor.Id.Value, refusal.Message);
    }

    [Fact]
    public void Withdrawing_the_survivor_of_a_merge_loses_the_absorbed_id_too() {
        // Arrange
        Contributor tolstoi = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
        Contributor garnett = new Contributor(new PersonName("Garnett", "Constance"), ContributorRole.Translator);

        BibliographicRecord survivor = Any.BibliographicRecord().WithoutIsbn().WithoutIssn().WithContributors([tolstoi]).Generate();
        BibliographicRecord absorbed = Any.BibliographicRecord().DescribingTheSameWorkAs(survivor).WithoutIssn().WithContributors([garnett]).Generate();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);
        catalogue.Merge(survivor.Id, absorbed.Id);

        // Act
        catalogue.Withdraw(survivor.Id);

        // Assert
        Assert.Null(catalogue.Find(survivor.Id));
        Assert.Null(catalogue.Find(absorbed.Id));
    }

    [Fact]
    public void An_id_retired_by_a_merge_whose_survivor_was_withdrawn_names_itself() {
        // Arrange
        Contributor tolstoi = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
        Contributor garnett = new Contributor(new PersonName("Garnett", "Constance"), ContributorRole.Translator);

        BibliographicRecord survivor = Any.BibliographicRecord().WithoutIsbn().WithoutIssn().WithContributors([tolstoi]).Generate();
        BibliographicRecord absorbed = Any.BibliographicRecord().DescribingTheSameWorkAs(survivor).WithoutIssn().WithContributors([garnett]).Generate();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);
        catalogue.Merge(survivor.Id, absorbed.Id);
        catalogue.Withdraw(survivor.Id);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(Any.BibliographicRecord().WithId(absorbed.Id).Generate()));

        // Assert
        Assert.Equal(DuplicateReason.RetiredRecordId, refusal.Reason);
        Assert.Equal(absorbed.Id, refusal.Existing);
    }

    [Fact]
    public void Withdrawing_needs_an_id() {
        Assert.Throws<ArgumentNullException>(() => Any.Catalogue().Generate().Withdraw(null!));
    }

    // ---- finding ------------------------------------------------------------------------------------------------

    [Fact]
    public void An_unknown_id_finds_nothing() {
        // Arrange
        Catalogue catalogue = ACatalogueHolding(Any.BibliographicRecord().Generate());
        RecordId  unknown   = Any.RecordId().Generate();

        // Assert
        Assert.Null(catalogue.Find(unknown));
    }

    [Fact]
    public void Finding_needs_an_id() {
        Assert.Throws<ArgumentNullException>(() => Any.Catalogue().Generate().Find(null!));
    }

    [Fact]
    public void A_record_is_found_by_its_isbn_13() {
        // Arrange
        BibliographicRecord withIsbn    = Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        BibliographicRecord withoutIsbn = Any.BibliographicRecord().WithoutIsbn().Generate();
        Catalogue           catalogue   = ACatalogueHolding(withIsbn, withoutIsbn);

        // Assert
        Assert.Same(withIsbn, catalogue.FindByIsbn(new Isbn13("9780306406157")));
        Assert.Null(catalogue.FindByIsbn(Isbn979));
    }

    [Fact]
    public void A_record_is_found_by_the_isbn_10_of_its_isbn_13() {
        // Arrange
        BibliographicRecord withIsbn  = Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        Catalogue           catalogue = ACatalogueHolding(withIsbn);

        // Act
        BibliographicRecord? found = catalogue.FindByIsbn(new Isbn10("0-306-40615-2"));

        // Assert
        Assert.Same(withIsbn, found);
    }

    [Fact]
    public void An_isbn_10_that_matches_no_record_finds_nothing() {
        // Arrange
        Catalogue catalogue = ACatalogueHolding(Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate());

        // Assert
        Assert.Null(catalogue.FindByIsbn(new Isbn10("0-19-852663-6")));
    }

    [Fact]
    public void Finding_by_isbn_needs_an_isbn() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => catalogue.FindByIsbn((Isbn13)null!));
        Assert.Throws<ArgumentNullException>(() => catalogue.FindByIsbn((Isbn10)null!));
    }

    [Fact]
    public void Titles_are_found_under_normalisation_and_case_folding() {
        // Arrange
        BibliographicRecord cafe  = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Café")).Generate();
        BibliographicRecord other = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Cafe")).Generate();
        Catalogue           catalogue = ACatalogueHolding(cafe, other);

        // Act
        IReadOnlyList<BibliographicRecord> found = catalogue.FindByTitle(new Title("CAFE\u0301"));

        // Assert
        Assert.Same(cafe, Assert.Single(found));
    }

    [Fact]
    public void Every_edition_with_the_title_is_found() {
        // Arrange
        BibliographicRecord firstEdition  = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Dune")).WithEdition(1).Generate();
        BibliographicRecord secondEdition = Any.BibliographicRecord().DescribingTheSameWorkAs(firstEdition).WithEdition(2).Generate();
        BibliographicRecord another       = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Emma")).Generate();
        Catalogue           catalogue     = ACatalogueHolding(firstEdition, secondEdition, another);

        // Act
        IReadOnlyList<BibliographicRecord> found = catalogue.FindByTitle(new Title("Dune"));

        // Assert
        Assert.Equal(2, found.Count);
        Assert.Contains(firstEdition, found);
        Assert.Contains(secondEdition, found);
    }

    [Fact]
    public void A_title_nobody_has_finds_an_empty_list() {
        // Arrange
        Catalogue catalogue = ACatalogueHolding(Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Dune")).Generate());

        // Assert
        Assert.Empty(catalogue.FindByTitle(new Title("Emma")));
    }

    [Fact]
    public void Finding_by_title_needs_a_title() {
        Assert.Throws<ArgumentNullException>(() => Any.Catalogue().Generate().FindByTitle(null!));
    }

}
