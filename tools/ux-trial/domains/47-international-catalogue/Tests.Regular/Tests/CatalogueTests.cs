using System.Globalization;

using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class CatalogueTests {

    // ---- name ---------------------------------------------------------------------------------

    [Fact]
    public void The_name_is_stored_in_NFC_and_trimmed() {
        // Act
        Catalogue catalogue = new Catalogue("  Bibliothe\u0300que nationale \t");

        // Assert
        Assert.Equal("Bibliothèque nationale", catalogue.Name);
        Assert.Equal(0, catalogue.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_catalogue_needs_a_name(string name) {
        Assert.Throws<ArgumentException>(() => new Catalogue(name));
    }

    [Fact]
    public void A_null_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Catalogue(null!));
    }

    [Theory]
    [InlineData("two\nlines")]
    [InlineData("tab\there")]
    [InlineData("a\u2028b")]
    [InlineData("a\u0007b")]
    public void A_name_cannot_contain_a_control_or_line_break_character(string name) {
        Assert.Throws<ArgumentException>(() => new Catalogue(name));
    }

    [Fact]
    public void An_unpaired_surrogate_in_the_name_is_refused() {
        Assert.Throws<ArgumentException>(() => new Catalogue("name\uD800"));
    }

    [Fact]
    public void A_name_of_120_clusters_is_accepted_and_121_are_refused() {
        // Arrange
        string hundredTwenty = string.Concat(Enumerable.Repeat("x\u0301", Catalogue.MaxNameLength));

        // Act
        Catalogue accepted = new Catalogue(hundredTwenty);
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Catalogue(hundredTwenty + "y"));

        // Assert
        Assert.Equal(240, accepted.Name.Length);
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_name_may_be_in_any_script() {
        Assert.Equal("国立国会図書館", new Catalogue("国立国会図書館").Name);
    }

    // ---- add and find -------------------------------------------------------------------------

    [Fact]
    public void A_record_added_is_found_by_its_id() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord record    = Any.BibliographicRecord().Generate();

        // Act
        catalogue.Add(record);

        // Assert
        Assert.Same(record, catalogue.Find(record.Id));
        Assert.Equal(1, catalogue.Count);
        Assert.Equal([record], catalogue.Records());
    }

    [Fact]
    public void Several_distinct_records_can_be_held() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();
        List<BibliographicRecord> records = Any.ListOf(Any.BibliographicRecord()).WithCount(8).Generate();

        // Act
        foreach (BibliographicRecord record in records) {
            catalogue.Add(record);
        }

        // Assert
        Assert.Equal(8, catalogue.Count);
        Assert.All(records, record => Assert.Same(record, catalogue.Find(record.Id)));
    }

    [Fact]
    public void An_unknown_id_finds_nothing() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();
        RecordId  anyId     = Any.RecordId().Generate();

        // Assert
        Assert.Null(catalogue.Find(anyId));
    }

    [Fact]
    public void Finding_with_a_null_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Catalogue().Generate().Find(null!));
    }

    [Fact]
    public void Adding_null_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Catalogue().Generate().Add(null!));
    }

    [Fact]
    public void Records_gives_a_snapshot() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();
        catalogue.Add(Any.BibliographicRecord().Generate());

        // Act
        IReadOnlyList<BibliographicRecord> snapshot = catalogue.Records();
        catalogue.Add(Any.BibliographicRecord().Generate());

        // Assert
        Assert.Single(snapshot);
        Assert.Equal(2, catalogue.Count);
    }

    // ---- duplicates ---------------------------------------------------------------------------

    [Fact]
    public void A_record_with_an_id_already_held_is_refused_as_the_same_record_id() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord held      = Any.BibliographicRecord().Generate();
        catalogue.Add(held);
        BibliographicRecord another = Any.BibliographicRecord().WithId(held.Id).Generate();

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(another));

        // Assert
        Assert.Equal(DuplicateReason.SameRecordId, refusal.Reason);
        Assert.Equal(held.Id, refusal.Existing);
        Assert.Contains(held.Id.Value, refusal.Message);
        Assert.Equal(1, catalogue.Count);
        Assert.Same(held, catalogue.Find(held.Id));
    }

    [Fact]
    public void A_record_with_an_ISBN_already_held_is_refused_as_the_same_ISBN() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord held      = Records.WithIsbn().Generate();
        catalogue.Add(held);
        BibliographicRecord another = Records.WithIsbn().WithIsbn(held.Isbn).Generate();

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(another));

        // Assert
        Assert.Equal(DuplicateReason.SameIsbn, refusal.Reason);
        Assert.Equal(held.Id, refusal.Existing);
    }

    [Fact]
    public void Two_serials_with_the_same_ISSN_are_refused_as_the_same_ISSN() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        Issn                issn      = Any.Issn().Generate();
        BibliographicRecord held      = Records.Serial().WithIssn(issn).Generate();
        catalogue.Add(held);
        BibliographicRecord another = Records.Serial().WithIssn(issn).Generate();

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(another));

        // Assert
        Assert.Equal(DuplicateReason.SameIssn, refusal.Reason);
        Assert.Equal(held.Id, refusal.Existing);
    }

    [Fact]
    public void A_monograph_sharing_the_ISSN_of_its_series_is_not_a_duplicate_of_the_serial() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        Issn                issn      = Any.Issn().Generate();
        BibliographicRecord serial    = Records.Serial().WithIssn(issn).Generate();
        BibliographicRecord volume    = Records.WithIsbn().WithIssn(issn).Generate();
        catalogue.Add(serial);

        // Act
        DuplicateReason reason = catalogue.CheckDuplicate(volume);
        catalogue.Add(volume);

        // Assert
        Assert.Equal(DuplicateReason.None, reason);
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void Two_monographs_of_the_same_series_are_not_duplicates_of_each_other() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        Issn                issn      = Any.Issn().Generate();
        BibliographicRecord first     = Records.WithIsbn().WithIssn(issn).Generate();
        BibliographicRecord second    = Records.WithIsbn().WithIssn(issn).Generate();
        catalogue.Add(first);

        // Act
        catalogue.Add(second);

        // Assert
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void A_serial_is_not_a_duplicate_of_a_monograph_sharing_its_ISSN() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        Issn                issn      = Any.Issn().Generate();
        BibliographicRecord volume    = Records.WithIsbn().WithIssn(issn).Generate();
        BibliographicRecord serial    = Records.Serial().WithIssn(issn).Generate();
        catalogue.Add(volume);

        // Act & Assert
        Assert.Equal(DuplicateReason.None, catalogue.CheckDuplicate(serial));
    }

    [Fact]
    public void A_record_describing_the_same_work_is_refused_as_the_same_work() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord held      = Records.Unnumbered().Generate();
        catalogue.Add(held);
        BibliographicRecord twin = Records.SameWorkAs(held).Generate();

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(twin));

        // Assert
        Assert.Equal(DuplicateReason.SameWork, refusal.Reason);
        Assert.Equal(held.Id, refusal.Existing);
    }

    [Fact]
    public void Two_manifestations_with_different_ISBNs_are_both_accepted() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord hardback  = Records.WithIsbn().WithIsbn(new Isbn13("9780306406157")).Generate();
        BibliographicRecord paperback = Records.SameWorkAs(hardback).WithIsbn(new Isbn13("9781234567897")).Generate();
        catalogue.Add(hardback);

        // Act
        catalogue.Add(paperback);

        // Assert
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void A_record_without_ISBN_describing_a_numbered_edition_is_the_same_work() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord numbered  = Records.WithIsbn().Generate();
        BibliographicRecord bare      = Records.SameWorkAs(numbered).WithoutIsbn().Generate();
        catalogue.Add(numbered);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(bare));

        // Assert
        Assert.Equal(DuplicateReason.SameWork, refusal.Reason);
    }

    [Fact]
    public void The_same_id_is_reported_before_the_same_ISBN() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord held      = Records.WithIsbn().Generate();
        catalogue.Add(held);
        BibliographicRecord same = Records.Like(held).Generate();

        // Assert
        Assert.Equal(DuplicateReason.SameRecordId, catalogue.CheckDuplicate(same));
    }

    [Fact]
    public void The_same_ISBN_is_reported_before_the_same_work() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord held      = Records.WithIsbn().Generate();
        catalogue.Add(held);
        BibliographicRecord twin = Records.SameWorkAs(held).Generate();

        // Assert
        Assert.Equal(DuplicateReason.SameIsbn, catalogue.CheckDuplicate(twin));
    }

    [Fact]
    public void The_same_ISSN_is_reported_before_the_same_work() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord held      = Records.Serial().Generate();
        catalogue.Add(held);
        BibliographicRecord twin = Records.SameWorkAs(held).Generate();

        // Assert
        Assert.Equal(DuplicateReason.SameIssn, catalogue.CheckDuplicate(twin));
    }

    [Fact]
    public void CheckDuplicate_reports_none_for_a_record_that_would_be_accepted_and_changes_nothing() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord candidate = Any.BibliographicRecord().Generate();

        // Act
        DuplicateReason reason = catalogue.CheckDuplicate(candidate);

        // Assert
        Assert.Equal(DuplicateReason.None, reason);
        Assert.Equal(0, catalogue.Count);
    }

    [Fact]
    public void CheckDuplicate_with_null_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Catalogue().Generate().CheckDuplicate(null!));
    }

    [Fact]
    public void A_refused_record_leaves_the_catalogue_as_it_was() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord held      = Records.Unnumbered().Generate();
        catalogue.Add(held);
        BibliographicRecord twin = Records.SameWorkAs(held).Generate();

        // Act
        Assert.Throws<DuplicateRecordException>(() => catalogue.Add(twin));

        // Assert
        Assert.Equal([held], catalogue.Records());
        Assert.Null(catalogue.Find(twin.Id));
    }

    [Fact]
    public void A_duplicate_exception_is_an_invalid_operation() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord held      = Any.BibliographicRecord().Generate();
        catalogue.Add(held);

        // Act
        Exception refusal = Assert.ThrowsAny<Exception>(() => catalogue.Add(held));

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
    }

    // ---- withdraw -----------------------------------------------------------------------------

    [Fact]
    public void A_withdrawn_record_is_gone_and_its_id_is_retired() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord record    = Any.BibliographicRecord().Generate();
        catalogue.Add(record);

        // Act
        catalogue.Withdraw(record.Id);

        // Assert
        Assert.Null(catalogue.Find(record.Id));
        Assert.Equal(0, catalogue.Count);
        Assert.Empty(catalogue.Records());
    }

    [Fact]
    public void A_withdrawn_id_is_never_reused_and_names_itself_as_the_existing_record() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord record    = Any.BibliographicRecord().Generate();
        catalogue.Add(record);
        catalogue.Withdraw(record.Id);
        BibliographicRecord comeback = Any.BibliographicRecord().WithId(record.Id).Generate();

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(comeback));

        // Assert
        Assert.Equal(DuplicateReason.RetiredRecordId, refusal.Reason);
        Assert.Equal(record.Id, refusal.Existing);
    }

    [Fact]
    public void Withdrawing_frees_the_ISBN_and_the_work() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord record    = Records.WithIsbn().Generate();
        catalogue.Add(record);
        catalogue.Withdraw(record.Id);
        BibliographicRecord republished = Records.SameWorkAs(record).Generate();

        // Act
        catalogue.Add(republished);

        // Assert
        Assert.Same(republished, catalogue.FindByIsbn(record.Isbn!));
    }

    [Fact]
    public void Withdrawing_an_unknown_record_is_refused() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();
        RecordId  anyId     = Any.RecordId().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalogue.Withdraw(anyId));

        // Assert
        Assert.Contains("not in the catalogue", refusal.Message);
    }

    [Fact]
    public void Withdrawing_twice_is_refused() {
        // Arrange
        Catalogue           catalogue = Any.Catalogue().Generate();
        BibliographicRecord record    = Any.BibliographicRecord().Generate();
        catalogue.Add(record);
        catalogue.Withdraw(record.Id);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalogue.Withdraw(record.Id));

        // Assert
        Assert.Contains("retired", refusal.Message);
        Assert.Contains("withdrawn", refusal.Message);
    }

    [Fact]
    public void Withdrawing_with_a_null_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Catalogue().Generate().Withdraw(null!));
    }

    // ---- merge --------------------------------------------------------------------------------

    /// <summary>Two records of one edition that the catalogue accepts side by side: they differ in their contributors.</summary>
    private static (BibliographicRecord Survivor, BibliographicRecord Absorbed) MergeablePair(Contributor survivorContributor, Contributor absorbedContributor) {
        BibliographicRecord survivor = Records.Unnumbered().WithContributors([survivorContributor]).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithContributors([absorbedContributor]).Generate();

        return (survivor, absorbed);
    }

    private static Catalogue CatalogueHolding(params BibliographicRecord[] records) {
        Catalogue catalogue = Any.Catalogue().Generate();
        foreach (BibliographicRecord record in records) {
            catalogue.Add(record);
        }

        return catalogue;
    }

    [Fact]
    public void A_merge_keeps_the_survivor_retires_the_absorbed_id_and_redirects_it() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = MergeablePair(Records.Author("Anna", "A"), Records.Author("Boris", "B"));
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(1, catalogue.Count);
        Assert.Same(merged, catalogue.Find(survivor.Id));
        Assert.Same(merged, catalogue.Find(absorbed.Id));
        Assert.Equal(survivor.Id, merged.Id);
        Assert.Equal([merged], catalogue.Records());
    }

    [Fact]
    public void A_merge_appends_the_contributors_the_survivor_does_not_list() {
        // Arrange
        Contributor anna  = Records.Author("Anna", "A");
        Contributor boris = Records.Author("Boris", "B");
        Contributor carl  = new Contributor(new PersonName("Carl"), ContributorRole.Translator);
        BibliographicRecord survivor = Records.Unnumbered().WithContributors([anna, boris]).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithContributors([carl, boris]).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal([anna, boris, carl], merged.Contributors);
    }

    [Fact]
    public void The_same_person_in_another_role_is_a_contributor_of_its_own() {
        // Arrange
        PersonName anna = new PersonName("Anna");
        Contributor author     = new Contributor(anna, ContributorRole.Author);
        Contributor translator = new Contributor(anna, ContributorRole.Translator);
        Contributor boris = new Contributor(new PersonName("Boris"), ContributorRole.Editor);
        BibliographicRecord survivor = Records.Unnumbered().WithContributors([author, boris]).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithContributors([translator]).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal([author, boris, translator], merged.Contributors);
    }

    [Fact]
    public void The_survivor_takes_what_it_lacks_from_the_absorbed_record() {
        // Arrange
        Isbn13     isbn       = new Isbn13("9780306406157");
        Issn       issn       = new Issn("0378-5955");
        CallNumber callNumber = new CallNumber("823.914 R884");
        Title      subtitle   = new Title("A Novel");
        BibliographicRecord survivor = Records.Unnumbered().WithScript(WritingScript.Latin).WithoutSubtitle().WithCallNumber(null).WithPublicationYear(2000).WithContributors([Records.Author("Anna", "A")]).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithContributors([Records.Author("Boris", "B")])
                                              .WithSubtitle(subtitle).WithIsbn(isbn).WithIssn(issn).WithCallNumber(callNumber).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(subtitle, merged.Subtitle);
        Assert.Equal(isbn, merged.Isbn);
        Assert.Equal(issn, merged.Issn);
        Assert.Equal(callNumber, merged.CallNumber);
        Assert.Same(merged, catalogue.FindByIsbn(isbn));
    }

    [Fact]
    public void The_survivor_keeps_its_own_values_when_it_has_them() {
        // Arrange
        Title      survivorSubtitle = new Title("Survivor subtitle");
        CallNumber survivorCall     = new CallNumber("100 A1");
        Issn       sharedSeries     = new Issn("0378-5955");
        BibliographicRecord survivor = Records.WithIsbn().WithScript(WritingScript.Latin).WithSubtitle(survivorSubtitle).WithCallNumber(survivorCall).WithIssn(sharedSeries)
                                              .WithContributors([Records.Author("Anna", "A")]).Generate();
        BibliographicRecord absorbed = Records.Like(survivor).WithId(Any.RecordId().DifferentFrom(survivor.Id).Generate())
                                              .WithIsbn(null).WithSubtitle(new Title("Absorbed subtitle")).WithCallNumber(new CallNumber("200 B2"))
                                              .WithContributors([Records.Author("Boris", "B")]).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(survivorSubtitle, merged.Subtitle);
        Assert.Equal(survivorCall, merged.CallNumber);
        Assert.Equal(survivor.Isbn, merged.Isbn);
        Assert.Equal(sharedSeries, merged.Issn);
    }

    [Fact]
    public void Merging_a_record_into_itself_is_an_argument_error() {
        // Arrange
        BibliographicRecord record  = Any.BibliographicRecord().Generate();
        Catalogue           catalogue = CatalogueHolding(record);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => catalogue.Merge(record.Id, new RecordId(record.Id.Value)));

        // Assert
        Assert.Equal("absorbedId", refusal.ParamName);
    }

    [Fact]
    public void The_same_id_is_refused_as_an_argument_error_even_if_unknown() {
        // Arrange
        RecordId anyId = Any.RecordId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.Catalogue().Generate().Merge(anyId, anyId));
    }

    [Fact]
    public void Merging_with_null_ids_is_refused() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();
        RecordId  anyId     = Any.RecordId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => catalogue.Merge(null!, anyId));
        Assert.Throws<ArgumentNullException>(() => catalogue.Merge(anyId, null!));
    }

    [Fact]
    public void Merging_an_unknown_record_is_refused_on_either_side() {
        // Arrange
        BibliographicRecord held    = Any.BibliographicRecord().Generate();
        Catalogue           catalogue = CatalogueHolding(held);
        RecordId            unknown = Any.RecordId().DifferentFrom(held.Id).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(held.Id, unknown));
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(unknown, held.Id));
    }

    [Fact]
    public void A_retired_record_cannot_be_merged_again() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = MergeablePair(Records.Author("Anna", "A"), Records.Author("Boris", "B"));
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);
        catalogue.Merge(survivor.Id, absorbed.Id);

        // Act
        InvalidOperationException asAbsorbed = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));
        InvalidOperationException asSurvivor = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(absorbed.Id, survivor.Id));

        // Assert
        Assert.Contains("retired", asAbsorbed.Message);
        Assert.Contains(survivor.Id.Value, asAbsorbed.Message);
        Assert.Contains("retired", asSurvivor.Message);
    }

    [Fact]
    public void A_merged_away_id_is_never_reused() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = MergeablePair(Records.Author("Anna", "A"), Records.Author("Boris", "B"));
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);
        catalogue.Merge(survivor.Id, absorbed.Id);
        BibliographicRecord comeback = Any.BibliographicRecord().WithId(absorbed.Id).Generate();

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(comeback));

        // Assert
        Assert.Equal(DuplicateReason.RetiredRecordId, refusal.Reason);
        Assert.Equal(survivor.Id, refusal.Existing);
    }

    [Fact]
    public void Redirects_are_followed_through_successive_merges() {
        // Arrange
        BibliographicRecord first  = Records.Unnumbered().WithContributors([Records.Author("Anna", "A")]).Generate();
        BibliographicRecord second = Records.SameWorkAs(first).WithContributors([Records.Author("Boris", "B")]).Generate();
        BibliographicRecord third  = Records.SameWorkAs(first).WithContributors([Records.Author("Carl", "C")]).Generate();
        Catalogue catalogue = CatalogueHolding(first, second, third);

        // Act
        catalogue.Merge(second.Id, first.Id);
        BibliographicRecord merged = catalogue.Merge(third.Id, second.Id);

        // Assert
        Assert.Same(merged, catalogue.Find(first.Id));
        Assert.Same(merged, catalogue.Find(second.Id));
        Assert.Same(merged, catalogue.Find(third.Id));
        Assert.Equal(1, catalogue.Count);
        Assert.Equal(3, merged.Contributors.Count);
    }

    [Fact]
    public void Withdrawing_a_survivor_leaves_the_ids_it_absorbed_resolving_to_nothing() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = MergeablePair(Records.Author("Anna", "A"), Records.Author("Boris", "B"));
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);
        catalogue.Merge(survivor.Id, absorbed.Id);

        // Act
        catalogue.Withdraw(survivor.Id);

        // Assert
        Assert.Null(catalogue.Find(survivor.Id));
        Assert.Null(catalogue.Find(absorbed.Id));
        Assert.Equal(0, catalogue.Count);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("language")]
    [InlineData("script")]
    [InlineData("edition")]
    [InlineData("year")]
    public void Records_of_different_editions_cannot_be_merged(string differing) {
        // Arrange
        BibliographicRecord survivor = Records.Unnumbered().WithScript(WritingScript.Latin).WithLanguage(new LanguageTag("fr"))
                                              .WithTitle(new Title("Dune")).WithEdition(2).WithPublicationYear(1999)
                                              .WithContributors([Records.Author("Anna", "A")]).Generate();
        AnyBibliographicRecord other = Records.Like(survivor).WithId(Any.RecordId().DifferentFrom(survivor.Id).Generate())
                                              .WithContributors([Records.Author("Boris", "B")]);
        BibliographicRecord absorbed = differing switch {
            "title"    => other.WithTitle(new Title("Dune Messiah")).Generate(),
            "language" => other.WithLanguage(new LanguageTag("fr-CA")).Generate(),
            "script"   => other.WithScript(WritingScript.Cyrillic).WithTitle(new Title("Дюна")).WithoutSubtitle().Generate(),
            "edition"  => other.WithEdition(3).Generate(),
            _          => other.WithPublicationYear(2000).Generate(),
        };
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));

        // Assert
        Assert.Contains("differ", refusal.Message);
        Assert.Equal(2, catalogue.Count);
        Assert.Same(absorbed, catalogue.Find(absorbed.Id));
    }

    [Fact]
    public void A_title_equal_under_case_folding_does_not_prevent_a_merge() {
        // Arrange
        BibliographicRecord survivor = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title("Café")).WithContributors([Records.Author("Anna", "A")]).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithTitle(new Title("CAFÉ")).WithContributors([Records.Author("Boris", "B")]).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal("Café", merged.Title.Value);
    }

    [Fact]
    public void Conflicting_ISBNs_prevent_a_merge() {
        // Arrange
        BibliographicRecord survivor = Records.WithIsbn().WithIsbn(new Isbn13("9780306406157")).WithContributors([Records.Author("Anna", "A")]).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithIsbn(new Isbn13("9781234567897")).WithContributors([Records.Author("Boris", "B")]).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));

        // Assert
        Assert.Contains("ISBNs conflict", refusal.Message);
    }

    [Fact]
    public void Conflicting_ISSNs_prevent_a_merge() {
        // Arrange
        BibliographicRecord survivor = Records.Unnumbered().WithIssn(new Issn("0378-5955")).WithContributors([Records.Author("Anna", "A")]).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithIssn(new Issn("2434-561X")).WithContributors([Records.Author("Boris", "B")]).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));

        // Assert
        Assert.Contains("ISSNs conflict", refusal.Message);
    }

    [Fact]
    public void A_union_of_more_than_twenty_contributors_prevents_a_merge() {
        // Arrange
        List<Contributor> eleven = Enumerable.Range(0, 11).Select(i => Records.Author("Family" + (char)('a' + i), "Given")).ToList();
        List<Contributor> otherEleven = Enumerable.Range(0, 11).Select(i => Records.Author("Other" + (char)('a' + i), "Given")).ToList();
        BibliographicRecord survivor = Records.Unnumbered().WithContributors(eleven).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithContributors(otherEleven).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));

        // Assert
        Assert.Contains("22 contributors", refusal.Message);
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void A_union_of_exactly_twenty_contributors_is_allowed() {
        // Arrange
        List<Contributor> ten      = Enumerable.Range(0, 10).Select(i => Records.Author("Family" + (char)('a' + i), "Given")).ToList();
        List<Contributor> otherTen = Enumerable.Range(0, 10).Select(i => Records.Author("Other" + (char)('a' + i), "Given")).ToList();
        BibliographicRecord survivor = Records.Unnumbered().WithContributors(ten).Generate();
        BibliographicRecord absorbed = Records.SameWorkAs(survivor).WithContributors(otherTen).Generate();
        Catalogue catalogue = CatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(20, merged.Contributors.Count);
    }

    // ---- lookups ------------------------------------------------------------------------------

    [Fact]
    public void A_record_is_found_by_its_ISBN_13_and_by_the_equivalent_ISBN_10() {
        // Arrange
        Isbn13              isbn   = new Isbn13("9780306406157");
        BibliographicRecord record = Records.WithIsbn().WithIsbn(isbn).Generate();
        Catalogue           catalogue = CatalogueHolding(record, Any.BibliographicRecord().WithAnyIsbn().Generate());

        // Assert
        Assert.Same(record, catalogue.FindByIsbn(new Isbn13("978-0-306-40615-7")));
        Assert.Same(record, catalogue.FindByIsbn(new Isbn10("0-306-40615-2")));
    }

    [Fact]
    public void An_unknown_ISBN_finds_nothing() {
        // Arrange
        Catalogue catalogue = CatalogueHolding(Records.WithIsbn().WithIsbn(new Isbn13("9780306406157")).Generate());

        // Assert
        Assert.Null(catalogue.FindByIsbn(new Isbn13("9781234567897")));
        Assert.Null(catalogue.FindByIsbn(new Isbn10("080442957X")));
    }

    [Fact]
    public void Finding_by_a_null_ISBN_is_refused() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => catalogue.FindByIsbn((Isbn13)null!));
        Assert.Throws<ArgumentNullException>(() => catalogue.FindByIsbn((Isbn10)null!));
    }

    [Fact]
    public void Records_are_found_by_title_under_NFC_and_case_folding() {
        // Arrange
        BibliographicRecord cafe      = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title("Café")).Generate();
        BibliographicRecord cafeAgain = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title("CAFE\u0301")).WithEdition(cafe.Edition % 999 + 1).Generate();
        BibliographicRecord other     = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title("Cafe")).Generate();
        Catalogue catalogue = CatalogueHolding(cafe, cafeAgain, other);

        // Act
        IReadOnlyList<BibliographicRecord> found = catalogue.FindByTitle(new Title("café"));

        // Assert
        Assert.Equal(2, found.Count);
        Assert.Contains(cafe, found);
        Assert.Contains(cafeAgain, found);
    }

    [Fact]
    public void A_title_nobody_has_finds_nothing() {
        // Arrange
        Catalogue catalogue = CatalogueHolding(Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title("Dune")).Generate());

        // Assert
        Assert.Empty(catalogue.FindByTitle(new Title("Emma")));
        Assert.Throws<ArgumentNullException>(() => catalogue.FindByTitle(null!));
    }

    // ---- listings -----------------------------------------------------------------------------

    private static ListingCollation EnglishAndFrench() {
        return new ListingCollation(CultureInfo.GetCultureInfo("fr-FR"),
                                    [new ArticleRule(new LanguageTag("en"), ["the", "a", "an"]), new ArticleRule(new LanguageTag("fr"), ["le", "la", "les", "l'"])]);
    }

    private static BibliographicRecord Book(string title, string language, int year, params Contributor[] contributors) {
        return Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title(title)).WithoutSubtitle().WithLanguage(new LanguageTag(language))
                      .WithPublicationYear(year).WithContributors(contributors).Generate();
    }

    [Fact]
    public void A_listing_is_sorted_by_the_collation() {
        // Arrange
        BibliographicRecord gatsby   = Book("The Great Gatsby", "en", 1925);
        BibliographicRecord etranger = Book("L'étranger", "fr", 1942);
        BibliographicRecord farewell = Book("A Farewell to Arms", "en", 1929);
        Catalogue catalogue = CatalogueHolding(gatsby, etranger, farewell);

        // Act
        IReadOnlyList<BibliographicRecord> listing = catalogue.Listing(EnglishAndFrench());

        // Assert
        Assert.Equal([etranger, farewell, gatsby], listing);
    }

    [Fact]
    public void A_listing_can_be_restricted_to_one_script() {
        // Arrange
        BibliographicRecord latin    = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title("Moscow")).Generate();
        BibliographicRecord cyrillic = Records.Unnumbered().WithScript(WritingScript.Cyrillic).WithTitle(new Title("Москва")).WithLanguage(new LanguageTag("ru")).Generate();
        BibliographicRecord hebrew   = Records.Unnumbered().WithScript(WritingScript.Hebrew).WithTitle(new Title("מוסקבה")).Generate();
        Catalogue catalogue = CatalogueHolding(latin, cyrillic, hebrew);

        // Act
        IReadOnlyList<BibliographicRecord> cyrillicOnly = catalogue.Listing(EnglishAndFrench(), WritingScript.Cyrillic);
        IReadOnlyList<BibliographicRecord> everything    = catalogue.Listing(EnglishAndFrench());

        // Assert
        Assert.Equal([cyrillic], cyrillicOnly);
        Assert.Equal(3, everything.Count);
        Assert.Empty(catalogue.Listing(EnglishAndFrench(), WritingScript.Greek));
    }

    [Fact]
    public void A_listing_of_an_empty_catalogue_is_empty() {
        Assert.Empty(Any.Catalogue().Generate().Listing(EnglishAndFrench()));
        Assert.Equal(string.Empty, Any.Catalogue().Generate().RenderListing(EnglishAndFrench()));
    }

    [Fact]
    public void A_listing_needs_a_collation_and_a_defined_script() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => catalogue.Listing(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => catalogue.Listing(EnglishAndFrench(), (WritingScript)77));
    }

    [Fact]
    public void A_listing_does_not_change_the_catalogue() {
        // Arrange
        BibliographicRecord zebra = Book("Zebra", "en", 2000);
        BibliographicRecord apple = Book("Apple", "en", 2000);
        Catalogue catalogue = CatalogueHolding(zebra, apple);

        // Act
        catalogue.Listing(EnglishAndFrench());

        // Assert
        Assert.Equal(2, catalogue.Count);
        Assert.Contains(zebra, catalogue.Records());
    }

    [Fact]
    public void The_rendered_listing_gives_one_line_per_record_with_filing_title_first_contributor_and_year() {
        // Arrange
        BibliographicRecord gatsby = Book("The Great Gatsby", "en", 1925, Records.Author("Fitzgerald", "F. Scott"));
        BibliographicRecord arms   = Book("A Farewell to Arms", "en", 1929, Records.Author("Hemingway", "Ernest"), Records.Author("Other", "Person"));
        Catalogue catalogue = CatalogueHolding(gatsby, arms);

        // Act
        string rendered = catalogue.RenderListing(EnglishAndFrench());

        // Assert
        Assert.Equal("Farewell to Arms / Hemingway, Ernest (1929)\nGreat Gatsby / Fitzgerald, F. Scott (1925)\n", rendered);
    }

    [Fact]
    public void A_record_without_contributors_is_rendered_without_the_separator() {
        // Arrange
        Catalogue catalogue = CatalogueHolding(Book("Anonymous Work", "en", 1500));

        // Act
        string rendered = catalogue.RenderListing(EnglishAndFrench());

        // Assert
        Assert.Equal("Anonymous Work (1500)\n", rendered);
    }

    [Fact]
    public void A_CJK_contributor_is_rendered_by_its_heading() {
        // Arrange
        BibliographicRecord record = Records.Unnumbered().WithScript(WritingScript.Cjk).WithTitle(new Title("ノルウェイの森")).WithoutSubtitle().WithPublicationYear(1987)
                                            .WithContributors([new Contributor(new PersonName("村上", "春樹"), ContributorRole.Author)]).Generate();
        Catalogue catalogue = CatalogueHolding(record);

        // Act
        string rendered = catalogue.RenderListing(EnglishAndFrench(), WritingScript.Cjk);

        // Assert
        Assert.Equal("ノルウェイの森 / 村上春樹 (1987)\n", rendered);
    }

}
