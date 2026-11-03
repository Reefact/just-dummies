using System.Globalization;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class CatalogueTests {

    private static readonly Contributor Camus = new Contributor(new PersonName("Camus", "Albert"), ContributorRole.Author);

    private static Catalogue AnyCatalogue() {
        return new Catalogue(Scripts.TitleText(WritingScript.Latin).Generate());
    }

    private static Catalogue AnyCatalogueHolding(params BibliographicRecord[] records) {
        Catalogue catalogue = AnyCatalogue();
        foreach (BibliographicRecord record in records) { catalogue.Add(record); }

        return catalogue;
    }

    /// <summary>A record of one edition of a work; two of them built from the same title and contributors are the same work.</summary>
    private static AnyBibliographicRecord Edition(string title) {
        return Any.BibliographicRecord()
                  .WithTitle(new Title(title))
                  .WithSubtitle(null)
                  .WithLanguage(new LanguageTag("fr"))
                  .WithEdition(1)
                  .WithPublicationYear(2005)
                  .WithContributors(Camus)
                  .WithoutIsbn()
                  .WithoutIssn();
    }

    // ---- Name ----

    [Fact]
    public void A_catalogue_has_a_trimmed_name_in_normalisation_form_C() {
        // Act
        Catalogue catalogue = new Catalogue("  Bibliothèque nationale\t");

        // Assert
        Assert.Equal("Bibliothèque nationale", catalogue.Name);
        Assert.Equal(0, catalogue.Count);
        Assert.Empty(catalogue.Records());
    }

    [Fact]
    public void A_catalogue_name_may_mix_scripts_and_hold_an_emoji() {
        // Act
        Catalogue catalogue = new Catalogue("Bibliothèque nationale / المكتبة الوطنية 📚");

        // Assert
        Assert.Equal("Bibliothèque nationale / المكتبة الوطنية 📚", catalogue.Name);
    }

    [Fact]
    public void A_catalogue_needs_a_name() {
        Assert.Throws<ArgumentNullException>(() => new Catalogue(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_catalogue_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new Catalogue(name));
    }

    [Theory]
    [InlineData("\u0000")]
    [InlineData("\n")]
    [InlineData("\t")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void A_catalogue_name_refuses_control_and_line_break_characters(string forbidden) {
        // Arrange
        string anyPrefix = Scripts.TitleText(WritingScript.Latin).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Catalogue(anyPrefix + forbidden + anyPrefix));
    }

    [Fact]
    public void A_catalogue_name_with_an_unpaired_surrogate_is_refused() {
        // Arrange
        string anyName = Scripts.TitleText(WritingScript.Latin).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Catalogue(anyName + (char)0xD800));
    }

    [Fact]
    public void A_catalogue_name_may_hold_120_grapheme_clusters_but_not_121() {
        // Arrange
        string cluster = "ạ̈";

        // Act
        Catalogue catalogue = new Catalogue(string.Concat(Enumerable.Repeat(cluster, Catalogue.MaxNameLength)));

        // Assert
        Assert.Equal(360 - 120, catalogue.Name.Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Catalogue(string.Concat(Enumerable.Repeat(cluster, 121))));
    }

    // ---- Add and duplicates ----

    [Fact]
    public void A_catalogue_holds_the_records_it_is_given() {
        // Arrange
        Catalogue catalogue = AnyCatalogue();
        BibliographicRecord first = Any.BibliographicRecord().Generate();
        BibliographicRecord second = Any.BibliographicRecord().Generate();

        // Act
        catalogue.Add(first);
        catalogue.Add(second);

        // Assert
        Assert.Equal(2, catalogue.Count);
        Assert.Equal([first, second], catalogue.Records().OrderBy(r => r.Id.Value == first.Id.Value ? 0 : 1));
        Assert.Same(first, catalogue.Find(first.Id));
    }

    [Fact]
    public void Adding_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => AnyCatalogue().Add(null!));
        Assert.Throws<ArgumentNullException>(() => AnyCatalogue().CheckDuplicate(null!));
    }

    [Fact]
    public void A_record_that_is_not_a_duplicate_checks_out_as_none() {
        // Arrange
        Catalogue catalogue = AnyCatalogueHolding(Any.BibliographicRecord().Generate());
        BibliographicRecord anotherRecord = Any.BibliographicRecord().Generate();

        // Act & Assert
        Assert.Equal(DuplicateReason.None, catalogue.CheckDuplicate(anotherRecord));
    }

    [Fact]
    public void A_record_whose_id_is_held_is_refused() {
        // Arrange
        RecordId id = new RecordId("FR00412873");
        BibliographicRecord held = Any.BibliographicRecord().WithId(id).Generate();
        BibliographicRecord sameId = Any.BibliographicRecord().WithId(new RecordId("FR00412873")).Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act
        DuplicateRecordException error = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(sameId));

        // Assert
        Assert.Equal(DuplicateReason.SameRecordId, error.Reason);
        Assert.Equal(id, error.Existing);
        Assert.Equal(DuplicateReason.SameRecordId, catalogue.CheckDuplicate(sameId));
        Assert.Equal(1, catalogue.Count);
        Assert.Same(held, catalogue.Find(id));
    }

    [Fact]
    public void A_duplicate_refusal_is_an_invalid_operation_that_says_which_record_it_duplicates() {
        // Arrange
        BibliographicRecord held = Any.BibliographicRecord().Generate();
        BibliographicRecord sameId = Any.BibliographicRecord().WithId(held.Id).Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act
        InvalidOperationException error = Assert.ThrowsAny<InvalidOperationException>(() => catalogue.Add(sameId));

        // Assert
        Assert.IsType<DuplicateRecordException>(error);
        Assert.Contains(held.Id.Value, error.Message);
    }

    [Fact]
    public void A_record_with_the_isbn_of_a_held_record_is_refused() {
        // Arrange
        Isbn13 isbn = new Isbn13("9782070368228");
        BibliographicRecord held = Any.BibliographicRecord().WithIsbn(isbn).WithPublicationYear(2005).Generate();
        BibliographicRecord sameIsbn = Any.BibliographicRecord().WithIsbn(new Isbn13("978-2-07-036822-8")).WithPublicationYear(2010).Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act
        DuplicateRecordException error = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(sameIsbn));

        // Assert
        Assert.Equal(DuplicateReason.SameIsbn, error.Reason);
        Assert.Equal(held.Id, error.Existing);
    }

    [Fact]
    public void Two_serials_with_the_same_issn_are_duplicates() {
        // Arrange
        Issn issn = new Issn("0378-5955");
        BibliographicRecord held = Any.BibliographicRecord().WithIssn(issn).WithoutIsbn().Generate();
        BibliographicRecord sameIssn = Any.BibliographicRecord().WithIssn(issn).WithoutIsbn().Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act
        DuplicateRecordException error = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(sameIssn));

        // Assert
        Assert.Equal(DuplicateReason.SameIssn, error.Reason);
        Assert.Equal(held.Id, error.Existing);
    }

    [Fact]
    public void A_monograph_sharing_the_issn_of_its_series_is_not_a_duplicate() {
        // Arrange
        Issn seriesIssn = new Issn("0378-5955");
        BibliographicRecord series = Any.BibliographicRecord().WithIssn(seriesIssn).WithoutIsbn().Generate();
        BibliographicRecord volume = Any.BibliographicRecord().WithIssn(seriesIssn).WithPublicationYear(2005).WithIsbn(new Isbn13("9780306406157")).Generate();
        Catalogue catalogue = AnyCatalogueHolding(series);

        // Act
        catalogue.Add(volume);

        // Assert
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void A_serial_is_not_a_duplicate_of_a_monograph_of_its_issn() {
        // Arrange
        Issn seriesIssn = new Issn("0378-5955");
        BibliographicRecord volume = Any.BibliographicRecord().WithIssn(seriesIssn).WithPublicationYear(2005).WithIsbn(new Isbn13("9780306406157")).Generate();
        BibliographicRecord serial = Any.BibliographicRecord().WithIssn(seriesIssn).WithoutIsbn().Generate();
        Catalogue catalogue = AnyCatalogueHolding(volume);

        // Act & Assert
        Assert.Equal(DuplicateReason.None, catalogue.CheckDuplicate(serial));
    }

    [Fact]
    public void A_record_describing_the_same_work_as_a_held_one_is_refused() {
        // Arrange
        BibliographicRecord held = Edition("La Peste").Generate();
        BibliographicRecord sameWork = Edition("LA PESTE").Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act
        DuplicateRecordException error = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(sameWork));

        // Assert
        Assert.Equal(DuplicateReason.SameWork, error.Reason);
        Assert.Equal(held.Id, error.Existing);
    }

    [Fact]
    public void A_different_edition_of_the_same_work_is_not_a_duplicate() {
        // Arrange
        BibliographicRecord held = Edition("La Peste").Generate();
        BibliographicRecord secondEdition = Edition("La Peste").WithEdition(2).Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act
        catalogue.Add(secondEdition);

        // Assert
        Assert.Equal(2, catalogue.Count);
    }

    [Fact]
    public void Duplicates_are_checked_in_order_id_then_isbn_then_issn_then_work() {
        // Arrange: the candidate duplicates a held record on every count at once.
        Isbn13 isbn = new Isbn13("9782070368228");
        Issn issn = new Issn("0378-5955");
        RecordId id = new RecordId("FR00412873");
        BibliographicRecord held = Edition("La Peste").WithId(id).WithIsbn(isbn).WithIssn(issn).Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act & Assert
        Assert.Equal(DuplicateReason.SameRecordId, catalogue.CheckDuplicate(Edition("La Peste").WithId(id).WithIsbn(isbn).WithIssn(issn).Generate()));
        Assert.Equal(DuplicateReason.SameIsbn, catalogue.CheckDuplicate(Edition("La Peste").WithIsbn(isbn).WithIssn(issn).Generate()));
        Assert.Equal(DuplicateReason.SameWork, catalogue.CheckDuplicate(Edition("La Peste").WithIssn(issn).Generate()));
    }

    [Fact]
    public void Serials_are_checked_on_their_issn_before_their_work() {
        // Arrange
        Issn issn = new Issn("0378-5955");
        BibliographicRecord held = Edition("Le Monde").WithIssn(issn).Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act & Assert
        Assert.Equal(DuplicateReason.SameIssn, catalogue.CheckDuplicate(Edition("Le Monde").WithIssn(issn).Generate()));
    }

    [Fact]
    public void A_refused_record_leaves_the_catalogue_unchanged() {
        // Arrange
        BibliographicRecord held = Any.BibliographicRecord().Generate();
        Catalogue catalogue = AnyCatalogueHolding(held);

        // Act
        Assert.Throws<DuplicateRecordException>(() => catalogue.Add(Any.BibliographicRecord().WithId(held.Id).Generate()));

        // Assert
        Assert.Equal(1, catalogue.Count);
        Assert.Equal([held], catalogue.Records());
    }

    [Fact]
    public void A_duplicate_exception_needs_a_reason_and_a_record() {
        // Arrange
        RecordId anyId = Any.RecordId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DuplicateRecordException(DuplicateReason.None, anyId, "none"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DuplicateRecordException((DuplicateReason)99, anyId, "unknown"));
        Assert.Throws<ArgumentNullException>(() => new DuplicateRecordException(DuplicateReason.SameIsbn, null!, "no record"));
    }

    // ---- Find ----

    [Fact]
    public void An_unknown_id_finds_nothing() {
        // Arrange
        Catalogue catalogue = AnyCatalogueHolding(Any.BibliographicRecord().Generate());
        RecordId unknown = new RecordId("ZZ99999999");

        // Act & Assert
        Assert.Null(catalogue.Find(unknown));
        Assert.Throws<ArgumentNullException>(() => catalogue.Find(null!));
    }

    [Fact]
    public void A_record_is_found_by_its_isbn_13_or_its_isbn_10() {
        // Arrange
        BibliographicRecord record = Any.BibliographicRecord().WithPublicationYear(1999).WithIsbn(new Isbn13("9780306406157")).Generate();
        BibliographicRecord other = Any.BibliographicRecord().WithPublicationYear(1999).WithIsbn(new Isbn13("9782070368228")).Generate();
        Catalogue catalogue = AnyCatalogueHolding(record, other);

        // Act & Assert
        Assert.Same(record, catalogue.FindByIsbn(new Isbn13("978-0-306-40615-7")));
        Assert.Same(record, catalogue.FindByIsbn(new Isbn10("0-306-40615-2")));
        Assert.Null(catalogue.FindByIsbn(new Isbn13("9780140283297")));
        Assert.Null(catalogue.FindByIsbn(new Isbn10("020161622X")));
    }

    [Fact]
    public void A_record_is_found_by_a_title_equal_under_case_and_normalisation() {
        // Arrange
        BibliographicRecord cafe = Any.BibliographicRecord().WithTitle(new Title("Café de Flore")).Generate();
        BibliographicRecord other = Any.BibliographicRecord().WithTitle(new Title("Café")).Generate();
        Catalogue catalogue = AnyCatalogueHolding(cafe, other);

        // Act
        IReadOnlyList<BibliographicRecord> found = catalogue.FindByTitle(new Title("CAFÉ DE FLORE"));

        // Assert
        Assert.Equal([cafe], found);
        Assert.Empty(catalogue.FindByTitle(new Title("Cafe")));
    }

    [Fact]
    public void Several_records_may_share_a_title() {
        // Arrange
        BibliographicRecord first = Edition("La Peste").Generate();
        BibliographicRecord second = Edition("La Peste").WithEdition(2).Generate();
        Catalogue catalogue = AnyCatalogueHolding(first, second);

        // Act
        IReadOnlyList<BibliographicRecord> found = catalogue.FindByTitle(new Title("la peste"));

        // Assert
        Assert.Equal(2, found.Count);
    }

    // ---- Merge ----

    private static readonly Title DigitsOnly = new Title("2");

    private static readonly Contributor Translator = new Contributor(new PersonName("Gilbert", "Stuart"), ContributorRole.Translator);

    /// <summary>
    /// The survivor of a merge, and an absorbed record that describes the same edition. The two differ by a subtitle,
    /// which the duplicate check looks at and the merge does not, so that the catalogue accepts both.
    /// </summary>
    private static (BibliographicRecord Survivor, BibliographicRecord Absorbed, Catalogue Catalogue) Pair(Func<AnyBibliographicRecord, AnyBibliographicRecord> absorbedIs,
                                                                                                           Func<AnyBibliographicRecord, AnyBibliographicRecord>? survivorIs = null) {
        AnyBibliographicRecord survivor = Edition("La Peste").WithSubtitle(null);
        AnyBibliographicRecord absorbed = Edition("La Peste").WithSubtitle(DigitsOnly);
        BibliographicRecord survivorRecord = (survivorIs?.Invoke(survivor) ?? survivor).Generate();
        BibliographicRecord absorbedRecord = absorbedIs(absorbed).Generate();
        Catalogue catalogue = AnyCatalogueHolding(survivorRecord, absorbedRecord);

        return (survivorRecord, absorbedRecord, catalogue);
    }

    [Fact]
    public void The_survivor_takes_what_it_lacks_from_the_absorbed_record() {
        // Arrange
        Title subtitle = new Title("Chronique");
        Isbn13 isbn = new Isbn13("9782070368228");
        Issn issn = new Issn("0378-5955");
        CallNumber callNumber = new CallNumber("843.914 C213");
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(
            record => record.WithSubtitle(subtitle).WithIsbn(isbn).WithIssn(issn).WithCallNumber(callNumber).WithContributors(Camus, Translator),
            record => record.WithCallNumber(null).WithContributors(Camus));

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(survivor.Id, merged.Id);
        Assert.Equal(subtitle, merged.Subtitle);
        Assert.Equal(isbn, merged.Isbn);
        Assert.Equal(issn, merged.Issn);
        Assert.Equal(callNumber, merged.CallNumber);
        Assert.Equal([Camus, Translator], merged.Contributors);
        Assert.Same(merged, catalogue.Find(survivor.Id));
        Assert.Equal(1, catalogue.Count);
    }

    [Fact]
    public void The_survivor_keeps_its_own_values() {
        // Arrange
        Title survivorSubtitle = new Title("Chronique");
        CallNumber survivorCallNumber = new CallNumber("843.914 C213");
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(
            record => record.WithSubtitle(DigitsOnly).WithCallNumber(new CallNumber("100 A1")),
            record => record.WithSubtitle(survivorSubtitle).WithCallNumber(survivorCallNumber));

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(survivorSubtitle, merged.Subtitle);
        Assert.Equal(survivorCallNumber, merged.CallNumber);
        Assert.Equal("La Peste", merged.Title.Value);
    }

    [Fact]
    public void The_survivor_keeps_the_title_as_it_wrote_it() {
        // Arrange
        BibliographicRecord survivor = Edition("La Peste").WithSubtitle(null).Generate();
        BibliographicRecord absorbed = Edition("LA PESTE").WithSubtitle(DigitsOnly).Generate();
        Catalogue catalogue = AnyCatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal("La Peste", merged.Title.Value);
    }

    [Fact]
    public void The_absorbed_id_is_retired_and_resolves_to_the_survivor() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(record => record);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Same(merged, catalogue.Find(absorbed.Id));
        Assert.DoesNotContain(catalogue.Records(), record => record.Id.Equals(absorbed.Id));
    }

    [Fact]
    public void A_retired_id_cannot_be_reused() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(record => record);
        catalogue.Merge(survivor.Id, absorbed.Id);
        BibliographicRecord reusing = Any.BibliographicRecord().WithId(absorbed.Id).Generate();

        // Act
        DuplicateRecordException error = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(reusing));

        // Assert
        Assert.Equal(DuplicateReason.RetiredRecordId, error.Reason);
        Assert.Equal(survivor.Id, error.Existing);
    }

    [Fact]
    public void Merges_chain_through_their_redirects() {
        // Arrange
        BibliographicRecord first = Edition("La Peste").WithSubtitle(null).Generate();
        BibliographicRecord second = Edition("La Peste").WithSubtitle(new Title("2")).Generate();
        BibliographicRecord third = Edition("La Peste").WithSubtitle(new Title("3")).Generate();
        Catalogue catalogue = AnyCatalogueHolding(first, second, third);

        // Act
        catalogue.Merge(second.Id, first.Id);
        BibliographicRecord final = catalogue.Merge(third.Id, second.Id);

        // Assert
        Assert.Same(final, catalogue.Find(first.Id));
        Assert.Same(final, catalogue.Find(second.Id));
        Assert.Same(final, catalogue.Find(third.Id));
        Assert.Equal(1, catalogue.Count);
    }

    [Fact]
    public void A_retired_id_cannot_take_part_in_another_merge() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(record => record);
        BibliographicRecord third = Edition("La Peste").WithSubtitle(new Title("3")).Generate();
        catalogue.Add(third);
        catalogue.Merge(survivor.Id, absorbed.Id);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(absorbed.Id, third.Id));
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(third.Id, absorbed.Id));
    }

    [Fact]
    public void A_record_cannot_be_merged_into_itself() {
        // Arrange
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();
        Catalogue catalogue = AnyCatalogueHolding(anyRecord);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => catalogue.Merge(anyRecord.Id, new RecordId(anyRecord.Id.Value)));
    }

    [Fact]
    public void Merging_an_unknown_record_is_refused() {
        // Arrange
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();
        Catalogue catalogue = AnyCatalogueHolding(anyRecord);
        RecordId unknown = new RecordId("ZZ99999999");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(anyRecord.Id, unknown));
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(unknown, anyRecord.Id));
        Assert.Throws<ArgumentNullException>(() => catalogue.Merge(null!, anyRecord.Id));
        Assert.Throws<ArgumentNullException>(() => catalogue.Merge(anyRecord.Id, null!));
    }

    [Fact]
    public void Records_of_different_titles_are_not_merged() {
        AssertMergeRefused(record => record.WithTitle(new Title("La Chute")), "titles differ");
    }

    [Fact]
    public void Records_of_different_language_tags_are_not_merged() {
        AssertMergeRefused(record => record.WithLanguage(new LanguageTag("fr-CA")), "languages differ");
    }

    [Fact]
    public void Records_of_different_editions_are_not_merged() {
        AssertMergeRefused(record => record.WithEdition(2), "editions differ");
    }

    [Fact]
    public void Records_of_different_years_are_not_merged() {
        AssertMergeRefused(record => record.WithPublicationYear(2006), "publication years differ");
    }

    [Fact]
    public void Records_of_conflicting_isbns_are_not_merged() {
        AssertMergeRefused(record => record.WithIsbn(new Isbn13("9782070368228")), "ISBNs conflict",
                           record => record.WithIsbn(new Isbn13("9780306406157")));
    }

    [Fact]
    public void Records_of_conflicting_issns_are_not_merged() {
        AssertMergeRefused(record => record.WithIssn(new Issn("0378-5955")), "ISSNs conflict",
                           record => record.WithIssn(new Issn("0317-8471")));
    }

    [Fact]
    public void Records_of_different_scripts_are_not_merged() {
        // Arrange: a title without letters fits any script.
        BibliographicRecord latin = Edition("1984").WithScript(WritingScript.Latin).WithSubtitle(null).Generate();
        BibliographicRecord cyrillic = Edition("1984").WithScript(WritingScript.Cyrillic).WithSubtitle(DigitsOnly).Generate();
        Catalogue catalogue = AnyCatalogueHolding(latin, cyrillic);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(latin.Id, cyrillic.Id));

        // Assert
        Assert.Contains("scripts differ", error.Message);
    }

    private static void AssertMergeRefused(Func<AnyBibliographicRecord, AnyBibliographicRecord> absorbedIs, string reason,
                                           Func<AnyBibliographicRecord, AnyBibliographicRecord>? survivorIs = null) {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(absorbedIs, survivorIs);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));

        // Assert: the refusal names its reason and leaves both records where they were.
        Assert.Contains(reason, error.Message);
        Assert.Equal(2, catalogue.Count);
        Assert.Same(survivor, catalogue.Find(survivor.Id));
        Assert.Same(absorbed, catalogue.Find(absorbed.Id));
    }

    [Fact]
    public void Contributors_of_the_absorbed_record_are_appended_once() {
        // Arrange: 15 + 10 contributors, 5 of them in both, makes exactly 20.
        List<Contributor> shared = Any.ListOf(Any.Contributor()).WithCount(5).Distinct().Generate();
        List<Contributor> onlySurvivor = Any.ListOf(Any.Contributor()).WithCount(10).Distinct().Generate();
        List<Contributor> onlyAbsorbed = Any.ListOf(Any.Contributor()).WithCount(5).Distinct().Generate();
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(
            record => record.WithContributors(onlyAbsorbed.Concat(shared).ToArray()),
            record => record.WithContributors(shared.Concat(onlySurvivor).ToArray()));

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(20, merged.Contributors.Count);
        Assert.Equal(shared.Concat(onlySurvivor).Concat(onlyAbsorbed), merged.Contributors);
    }

    [Fact]
    public void A_merge_that_would_list_more_than_twenty_contributors_is_refused() {
        // Arrange
        List<Contributor> twentyOne = Any.ListOf(Any.Contributor()).WithCount(21).Distinct().Generate();
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(
            record => record.WithContributors(twentyOne.Skip(20).ToArray()),
            record => record.WithContributors(twentyOne.Take(20).ToArray()));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));

        // Assert
        Assert.Contains("21 contributors", error.Message);
        Assert.Equal(2, catalogue.Count);
        Assert.Same(absorbed, catalogue.Find(absorbed.Id));
    }

    // ---- Withdraw ----

    [Fact]
    public void A_withdrawn_record_is_no_longer_found_and_its_id_is_retired() {
        // Arrange
        BibliographicRecord record = Any.BibliographicRecord().Generate();
        BibliographicRecord other = Any.BibliographicRecord().Generate();
        Catalogue catalogue = AnyCatalogueHolding(record, other);

        // Act
        catalogue.Withdraw(record.Id);

        // Assert
        Assert.Null(catalogue.Find(record.Id));
        Assert.Equal(1, catalogue.Count);
        Assert.Same(other, catalogue.Find(other.Id));
    }

    [Fact]
    public void A_withdrawn_id_is_never_reused() {
        // Arrange
        BibliographicRecord record = Any.BibliographicRecord().Generate();
        Catalogue catalogue = AnyCatalogueHolding(record);
        catalogue.Withdraw(record.Id);

        // Act
        DuplicateRecordException error = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(Any.BibliographicRecord().WithId(record.Id).Generate()));

        // Assert
        Assert.Equal(DuplicateReason.RetiredRecordId, error.Reason);
        Assert.Equal(record.Id, error.Existing);
    }

    [Fact]
    public void A_withdrawn_record_is_not_a_duplicate_of_its_isbn_any_more() {
        // Arrange
        Isbn13 isbn = new Isbn13("9782070368228");
        BibliographicRecord withdrawn = Any.BibliographicRecord().WithPublicationYear(2005).WithIsbn(isbn).Generate();
        Catalogue catalogue = AnyCatalogueHolding(withdrawn);
        catalogue.Withdraw(withdrawn.Id);
        BibliographicRecord reissued = Any.BibliographicRecord().WithPublicationYear(2005).WithIsbn(isbn).Generate();

        // Act
        catalogue.Add(reissued);

        // Assert
        Assert.Same(reissued, catalogue.FindByIsbn(isbn));
    }

    [Fact]
    public void Only_a_current_record_can_be_withdrawn() {
        // Arrange
        BibliographicRecord record = Any.BibliographicRecord().Generate();
        Catalogue catalogue = AnyCatalogueHolding(record);
        catalogue.Withdraw(record.Id);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalogue.Withdraw(record.Id));
        Assert.Throws<InvalidOperationException>(() => catalogue.Withdraw(new RecordId("ZZ99999999")));
        Assert.Throws<ArgumentNullException>(() => catalogue.Withdraw(null!));
    }

    [Fact]
    public void An_id_merged_into_a_withdrawn_record_finds_nothing() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(record => record);
        catalogue.Merge(survivor.Id, absorbed.Id);

        // Act
        catalogue.Withdraw(survivor.Id);

        // Assert
        Assert.Null(catalogue.Find(absorbed.Id));
        Assert.Null(catalogue.Find(survivor.Id));
        Assert.Equal(0, catalogue.Count);
    }

    [Fact]
    public void A_withdrawn_survivor_cannot_take_part_in_a_merge() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed, Catalogue catalogue) = Pair(record => record);
        catalogue.Withdraw(survivor.Id);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));
    }

    // ---- Listing ----

    private static readonly ListingCollation EnglishCollation = new ListingCollation(CultureInfo.GetCultureInfo("en-US"),
                                                                                   [new ArticleRule(new LanguageTag("en"), ["the", "a", "an"])]);

    private static AnyBibliographicRecord Titled(string title, WritingScript script = WritingScript.Latin, string language = "en") {
        return Any.BibliographicRecord().WithTitle(new Title(title)).WithScript(script).WithLanguage(new LanguageTag(language)).WithPublicationYear(2001);
    }

    [Fact]
    public void A_listing_is_sorted_by_the_collation() {
        // Arrange
        BibliographicRecord hobbit = Titled("The Hobbit").Generate();
        BibliographicRecord emma = Titled("Emma").Generate();
        BibliographicRecord ulysses = Titled("ulysses").Generate();
        Catalogue catalogue = AnyCatalogueHolding(ulysses, hobbit, emma);

        // Act
        IReadOnlyList<BibliographicRecord> listing = catalogue.Listing(EnglishCollation);

        // Assert
        Assert.Equal([emma, hobbit, ulysses], listing);
    }

    [Fact]
    public void A_listing_may_keep_the_records_of_one_script() {
        // Arrange
        BibliographicRecord latin = Titled("Emma").Generate();
        BibliographicRecord cyrillic = Titled("Азбука", WritingScript.Cyrillic, "ru").Generate();
        BibliographicRecord otherCyrillic = Titled("Война", WritingScript.Cyrillic, "ru").Generate();
        Catalogue catalogue = AnyCatalogueHolding(latin, cyrillic, otherCyrillic);

        // Act
        IReadOnlyList<BibliographicRecord> listing = catalogue.Listing(EnglishCollation, WritingScript.Cyrillic);

        // Assert
        Assert.Equal([cyrillic, otherCyrillic], listing);
        Assert.Empty(catalogue.Listing(EnglishCollation, WritingScript.Hebrew));
    }

    [Fact]
    public void A_listing_refuses_an_undefined_script_and_a_missing_collation() {
        // Arrange
        Catalogue catalogue = AnyCatalogue();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => catalogue.Listing(EnglishCollation, (WritingScript)42));
        Assert.Throws<ArgumentNullException>(() => catalogue.Listing(null!));
        Assert.Throws<ArgumentNullException>(() => catalogue.RenderListing(null!));
    }

    [Fact]
    public void An_empty_catalogue_lists_and_renders_nothing() {
        // Arrange
        Catalogue catalogue = AnyCatalogue();

        // Assert
        Assert.Empty(catalogue.Listing(EnglishCollation));
        Assert.Equal("", catalogue.RenderListing(EnglishCollation));
    }

    [Fact]
    public void A_rendered_listing_gives_the_filing_title_the_first_contributor_and_the_year() {
        // Arrange
        Contributor tolkien = new Contributor(new PersonName("Tolkien", "J. R. R."), ContributorRole.Author);
        Contributor lewis = new Contributor(new PersonName("Lewis", "C. S."), ContributorRole.Author);
        BibliographicRecord hobbit = Titled("The Hobbit").WithPublicationYear(1937).WithContributors(tolkien, lewis).Generate();
        BibliographicRecord anonymous = Titled("Beowulf").WithPublicationYear(1999).WithContributors().Generate();
        Catalogue catalogue = AnyCatalogueHolding(hobbit, anonymous);

        // Act
        string rendered = catalogue.RenderListing(EnglishCollation);

        // Assert
        Assert.Equal("Beowulf (1999)\nHobbit / Tolkien, J. R. R. (1937)\n", rendered);
    }

    [Fact]
    public void A_rendered_cjk_name_has_no_separator() {
        // Arrange
        Contributor soseki = new Contributor(new PersonName("夏目", "漱石"), ContributorRole.Author);
        BibliographicRecord record = Titled("吾輩は猫である", WritingScript.Cjk, "ja").WithPublicationYear(1905).WithContributors(soseki).Generate();
        Catalogue catalogue = AnyCatalogueHolding(record);

        // Act
        string rendered = catalogue.RenderListing(EnglishCollation, WritingScript.Cjk);

        // Assert
        Assert.Equal("吾輩は猫である / 夏目漱石 (1905)\n", rendered);
    }

}
