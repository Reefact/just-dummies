using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class CatalogueAddTests {

    private static readonly Isbn13 Isbn978 = new Isbn13("978-0-306-40615-7");
    private static readonly Issn   Issn1   = new Issn("0378-5955");
    private static readonly Issn   Issn2   = new Issn("2049-3630");

    private static Catalogue AnEmptyCatalogue() {
        return Any.Catalogue().Generate();
    }

    [Fact]
    public void A_record_added_is_held_and_found() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord record    = Any.BibliographicRecord().Generate();

        // Act
        catalogue.Add(record);

        // Assert
        Assert.Equal(1, catalogue.Count);
        Assert.Same(record, catalogue.Find(record.Id));
        Assert.Same(record, Assert.Single(catalogue.Records()));
    }

    [Fact]
    public void Records_of_different_works_live_side_by_side() {
        // Arrange
        Catalogue catalogue = AnEmptyCatalogue();
        BibliographicRecord first  = Any.BibliographicRecord().WithTitle(new Title("Dune")).Generate();
        BibliographicRecord second = Any.BibliographicRecord().WithTitle(new Title("Emma")).Generate();

        // Act
        catalogue.Add(first);
        catalogue.Add(second);

        // Assert
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void Records_returns_a_snapshot() {
        // Arrange
        Catalogue catalogue = AnEmptyCatalogue();
        IReadOnlyList<BibliographicRecord> beforeTheAddition = catalogue.Records();

        // Act
        catalogue.Add(Any.BibliographicRecord().Generate());

        // Assert
        Assert.Empty(beforeTheAddition);
        Assert.Single(catalogue.Records());
    }

    [Fact]
    public void A_missing_record_cannot_be_added() {
        // Arrange
        Catalogue catalogue = AnEmptyCatalogue();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => catalogue.Add(null!));
        Assert.Throws<ArgumentNullException>(() => catalogue.CheckDuplicate(null!));
    }

    // ---- duplicates, in the order the catalogue checks them -----------------------------------------------------

    [Fact]
    public void A_record_that_duplicates_nothing_is_accepted() {
        // Arrange
        Catalogue catalogue = AnEmptyCatalogue();
        catalogue.Add(Any.BibliographicRecord().WithTitle(new Title("Dune")).Generate());

        // Act
        DuplicateReason reason = catalogue.CheckDuplicate(Any.BibliographicRecord().WithTitle(new Title("Emma")).Generate());

        // Assert
        Assert.Equal(DuplicateReason.None, reason);
    }

    [Fact]
    public void An_id_already_held_is_refused() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord held      = Any.BibliographicRecord().WithTitle(new Title("Dune")).Generate();
        BibliographicRecord sameId    = Any.BibliographicRecord().WithId(held.Id).WithTitle(new Title("Emma")).Generate();
        catalogue.Add(held);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(sameId));

        // Assert
        Assert.Equal(DuplicateReason.SameRecordId, refusal.Reason);
        Assert.Equal(held.Id, refusal.Existing);
        Assert.Equal(1, catalogue.Count);
        Assert.Equal(DuplicateReason.SameRecordId, catalogue.CheckDuplicate(sameId));
    }

    [Fact]
    public void The_id_comes_before_the_isbn() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord held      = Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        BibliographicRecord sameBoth  = Any.BibliographicRecord().DescribingTheSameWorkAs(held).WithId(held.Id).WithIsbn(Isbn978).Generate();
        catalogue.Add(held);

        // Assert
        Assert.Equal(DuplicateReason.SameRecordId, catalogue.CheckDuplicate(sameBoth));
    }

    [Fact]
    public void An_isbn_already_held_is_refused() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord held      = Any.BibliographicRecord().WithTitle(new Title("Dune")).WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        BibliographicRecord sameIsbn  = Any.BibliographicRecord().WithTitle(new Title("Emma")).WithPublicationYear(1995).WithIsbn(Isbn978).Generate();
        catalogue.Add(held);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(sameIsbn));

        // Assert
        Assert.Equal(DuplicateReason.SameIsbn, refusal.Reason);
        Assert.Equal(held.Id, refusal.Existing);
    }

    [Fact]
    public void The_isbn_comes_before_the_work() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord held      = Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        BibliographicRecord sameWorkAndIsbn = Any.BibliographicRecord().DescribingTheSameWorkAs(held).WithIsbn(Isbn978).Generate();
        catalogue.Add(held);

        // Assert
        Assert.Equal(DuplicateReason.SameIsbn, catalogue.CheckDuplicate(sameWorkAndIsbn));
    }

    [Fact]
    public void Two_serials_with_one_issn_are_refused() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord held      = Any.BibliographicRecord().WithTitle(new Title("Nature")).WithoutIsbn().WithIssn(Issn1).Generate();
        BibliographicRecord sameIssn  = Any.BibliographicRecord().WithTitle(new Title("Science")).WithoutIsbn().WithIssn(Issn1).Generate();
        catalogue.Add(held);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(sameIssn));

        // Assert
        Assert.Equal(DuplicateReason.SameIssn, refusal.Reason);
        Assert.Equal(held.Id, refusal.Existing);
    }

    [Fact]
    public void Two_serials_with_different_issns_live_side_by_side() {
        // Arrange
        Catalogue catalogue = AnEmptyCatalogue();
        catalogue.Add(Any.BibliographicRecord().WithTitle(new Title("Nature")).WithoutIsbn().WithIssn(Issn1).Generate());

        // Act
        DuplicateReason reason = catalogue.CheckDuplicate(Any.BibliographicRecord().WithTitle(new Title("Science")).WithoutIsbn().WithIssn(Issn2).Generate());

        // Assert
        Assert.Equal(DuplicateReason.None, reason);
    }

    [Fact]
    public void A_monograph_sharing_the_issn_of_a_series_is_not_a_duplicate() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord series    = Any.BibliographicRecord().WithTitle(new Title("Lecture Notes")).WithoutIsbn().WithIssn(Issn1).Generate();
        BibliographicRecord volume    = Any.BibliographicRecord().WithTitle(new Title("Volume 7")).WithPublicationYear(2010).WithIsbn(Isbn978).WithIssn(Issn1).Generate();
        catalogue.Add(series);

        // Act
        catalogue.Add(volume);

        // Assert
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void A_monograph_already_held_does_not_make_a_later_serial_with_its_issn_a_duplicate() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord volume    = Any.BibliographicRecord().WithTitle(new Title("Volume 7")).WithPublicationYear(2010).WithIsbn(Isbn978).WithIssn(Issn1).Generate();
        BibliographicRecord series    = Any.BibliographicRecord().WithTitle(new Title("Lecture Notes")).WithoutIsbn().WithIssn(Issn1).Generate();
        catalogue.Add(volume);

        // Assert
        Assert.Equal(DuplicateReason.None, catalogue.CheckDuplicate(series));
    }

    [Fact]
    public void The_issn_comes_before_the_work() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord held      = Any.BibliographicRecord().WithoutIsbn().WithIssn(Issn1).Generate();
        BibliographicRecord sameWorkAndIssn = Any.BibliographicRecord().DescribingTheSameWorkAs(held).WithIssn(Issn1).Generate();
        catalogue.Add(held);

        // Assert
        Assert.Equal(DuplicateReason.SameIssn, catalogue.CheckDuplicate(sameWorkAndIssn));
    }

    [Fact]
    public void A_record_describing_the_same_work_is_refused() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord held      = Any.BibliographicRecord().WithoutIssn().Generate();
        BibliographicRecord sameWork  = Any.BibliographicRecord().DescribingTheSameWorkAs(held).WithoutIssn().Generate();
        catalogue.Add(held);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(sameWork));

        // Assert
        Assert.Equal(DuplicateReason.SameWork, refusal.Reason);
        Assert.Equal(held.Id, refusal.Existing);
    }

    [Fact]
    public void Two_different_isbns_keep_the_same_work_in_two_manifestations() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord hardback  = Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).WithoutIssn().Generate();
        BibliographicRecord paperback = Any.BibliographicRecord().DescribingTheSameWorkAs(hardback).WithIsbn(new Isbn13("979-10-90636-07-1")).WithoutIssn().Generate();
        catalogue.Add(hardback);

        // Act
        catalogue.Add(paperback);

        // Assert
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void A_refusal_is_an_invalid_operation_that_names_the_record_and_the_reason() {
        // Arrange
        Catalogue           catalogue = AnEmptyCatalogue();
        BibliographicRecord held      = Any.BibliographicRecord().Generate();
        catalogue.Add(held);

        // Act
        InvalidOperationException refusal = Assert.ThrowsAny<InvalidOperationException>(() => catalogue.Add(held));

        // Assert
        Assert.IsType<DuplicateRecordException>(refusal);
        Assert.Contains(held.Id.Value, refusal.Message);
        Assert.Contains("SameRecordId", refusal.Message);
    }

    [Fact]
    public void A_refusal_names_a_reason_and_the_record_it_duplicates() {
        // Arrange
        RecordId anyExisting = Any.RecordId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DuplicateRecordException(DuplicateReason.None, anyExisting, "none"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DuplicateRecordException((DuplicateReason)99, anyExisting, "unknown"));
        Assert.Throws<ArgumentNullException>(() => new DuplicateRecordException(DuplicateReason.SameWork, null!, "no record"));
    }

}
