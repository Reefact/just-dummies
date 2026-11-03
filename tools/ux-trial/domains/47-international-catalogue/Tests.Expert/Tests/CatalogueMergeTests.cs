using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class CatalogueMergeTests {

    private static readonly Contributor Tolstoi = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
    private static readonly Contributor Garnett = new Contributor(new PersonName("Garnett", "Constance"), ContributorRole.Translator);
    private static readonly Contributor Bakst   = new Contributor(new PersonName("Bakst", "Léon"), ContributorRole.Illustrator);

    private static readonly Isbn13 Isbn978 = new Isbn13("978-0-306-40615-7");
    private static readonly Isbn13 Isbn979 = new Isbn13("979-10-90636-07-1");

    /// <summary>A record bare of everything a merge could take from it: only what a merge compares is left to chance.</summary>
    private static AnyBibliographicRecord ABareRecord() {
        return Any.BibliographicRecord().WithoutSubtitle().WithoutIsbn().WithoutIssn().WithoutCallNumber().WithContributors([Tolstoi]);
    }

    /// <summary>The same edition recorded twice by two agencies who credit different contributors.</summary>
    private static (BibliographicRecord Survivor, BibliographicRecord Absorbed) TwoRecordsOfOneEdition() {
        BibliographicRecord survivor = ABareRecord().Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithoutSubtitle().WithContributors([Garnett]).Generate();

        return (survivor, absorbed);
    }

    private static Catalogue ACatalogueHolding(params BibliographicRecord[] records) {
        Catalogue catalogue = Any.Catalogue().Generate();
        foreach (BibliographicRecord record in records) {
            catalogue.Add(record);
        }

        return catalogue;
    }

    // ---- what a merge does --------------------------------------------------------------------------------------

    [Fact]
    public void A_merge_leaves_the_survivor_in_place_of_both_records() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = TwoRecordsOfOneEdition();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(1, catalogue.Count);
        Assert.Equal(survivor.Id, merged.Id);
        Assert.Same(merged, Assert.Single(catalogue.Records()));
        Assert.Same(merged, catalogue.Find(survivor.Id));
    }

    [Fact]
    public void The_absorbed_id_resolves_to_the_survivor() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = TwoRecordsOfOneEdition();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Same(merged, catalogue.Find(absorbed.Id));
    }

    [Fact]
    public void The_survivor_keeps_its_own_title_and_values() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithScript(WritingScript.Latin).WithTitle(new Title("War and Peace")).WithEdition(2).WithPublicationYear(1990).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithTitle(new Title("WAR AND PEACE")).WithContributors([Garnett]).Generate();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal("War and Peace", merged.Title.Value);
        Assert.Equal(2, merged.Edition);
        Assert.Equal(1990, merged.PublicationYear);
        Assert.Equal(survivor.Language, merged.Language);
    }

    [Fact]
    public void The_survivor_takes_the_subtitle_it_lacks() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithScript(WritingScript.Latin).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithSubtitle(new Title("A novel")).WithContributors([Garnett]).Generate();

        // Act
        BibliographicRecord merged = ACatalogueHolding(survivor, absorbed).Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal("A novel", merged.Subtitle!.Value);
    }

    [Fact]
    public void The_survivor_keeps_the_subtitle_it_has() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithScript(WritingScript.Latin).WithSubtitle(new Title("A novel")).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithSubtitle(new Title("A play")).WithContributors([Garnett]).Generate();

        // Act
        BibliographicRecord merged = ACatalogueHolding(survivor, absorbed).Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal("A novel", merged.Subtitle!.Value);
    }

    [Fact]
    public void The_survivor_takes_the_isbn_the_issn_and_the_call_number_it_lacks() {
        // Arrange
        CallNumber callNumber = new CallNumber("891.733 T654");
        Issn       issn       = new Issn("0378-5955");

        BibliographicRecord survivor = ABareRecord().WithPublicationYear(2010).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithContributors([Garnett]).WithIsbn(Isbn978).WithIssn(issn).WithCallNumber(callNumber).Generate();

        // Act
        BibliographicRecord merged = ACatalogueHolding(survivor, absorbed).Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Same(Isbn978, merged.Isbn);
        Assert.Same(issn, merged.Issn);
        Assert.Same(callNumber, merged.CallNumber);
    }

    [Fact]
    public void The_survivor_keeps_the_call_number_it_has() {
        // Arrange
        CallNumber ownCallNumber      = new CallNumber("891.733 T654");
        CallNumber absorbedCallNumber = new CallNumber("891.733 T655");

        BibliographicRecord survivor = ABareRecord().WithCallNumber(ownCallNumber).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithContributors([Garnett]).WithCallNumber(absorbedCallNumber).Generate();

        // Act
        BibliographicRecord merged = ACatalogueHolding(survivor, absorbed).Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Same(ownCallNumber, merged.CallNumber);
    }

    [Fact]
    public void The_merged_record_is_found_by_the_isbn_it_took() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithPublicationYear(2010).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithContributors([Garnett]).WithIsbn(Isbn978).Generate();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);

        // Act
        BibliographicRecord merged = catalogue.Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Same(merged, catalogue.FindByIsbn(Isbn978));
    }

    [Fact]
    public void Contributors_are_the_survivors_then_those_of_the_absorbed_it_does_not_list() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithContributors([Tolstoi, Garnett]).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithContributors([Garnett, Bakst]).Generate();

        // Act
        BibliographicRecord merged = ACatalogueHolding(survivor, absorbed).Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal([Tolstoi, Garnett, Bakst], merged.Contributors);
    }

    [Fact]
    public void The_same_person_in_another_role_is_a_contributor_to_append() {
        // Arrange
        Contributor tolstoiAsEditor = new Contributor(Tolstoi.Name, ContributorRole.Editor);

        // The two agencies must disagree about something else, or the catalogue would have refused the second record as the same work.
        BibliographicRecord survivor = ABareRecord().WithScript(WritingScript.Latin).WithContributors([Tolstoi]).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithSubtitle(new Title("A novel")).WithContributors([tolstoiAsEditor]).Generate();

        // Act
        BibliographicRecord merged = ACatalogueHolding(survivor, absorbed).Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal([Tolstoi, tolstoiAsEditor], merged.Contributors);
    }

    [Fact]
    public void Twenty_contributors_in_all_are_accepted() {
        // Arrange
        List<Contributor> twentyDistinct = Any.ListOf(Any.Contributor()).WithCount(20).Distinct().Generate();

        BibliographicRecord survivor = ABareRecord().WithContributors(twentyDistinct.Take(10)).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithContributors(twentyDistinct.Skip(10)).Generate();

        // Act
        BibliographicRecord merged = ACatalogueHolding(survivor, absorbed).Merge(survivor.Id, absorbed.Id);

        // Assert
        Assert.Equal(20, merged.Contributors.Count);
    }

    [Fact]
    public void Twenty_one_contributors_in_all_are_refused_and_nothing_changes() {
        // Arrange
        List<Contributor> twentyOneDistinct = Any.ListOf(Any.Contributor()).WithCount(21).Distinct().Generate();

        BibliographicRecord survivor = ABareRecord().WithContributors(twentyOneDistinct.Take(11)).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithContributors(twentyOneDistinct.Skip(11)).Generate();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);

        // Act
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));

        // Assert
        Assert.Equal(2, catalogue.Count);
        Assert.Same(survivor, catalogue.Find(survivor.Id));
        Assert.Same(absorbed, catalogue.Find(absorbed.Id));
    }

    // ---- what a merge refuses -----------------------------------------------------------------------------------

    [Fact]
    public void A_record_cannot_be_merged_into_itself() {
        // Arrange
        BibliographicRecord record    = Any.BibliographicRecord().Generate();
        Catalogue           catalogue = ACatalogueHolding(record);

        // Act & Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => catalogue.Merge(record.Id, record.Id));
        Assert.Equal("absorbedId", refusal.ParamName);
    }

    [Fact]
    public void Both_ids_are_required() {
        // Arrange
        RecordId  anyId     = Any.RecordId().Generate();
        Catalogue catalogue = Any.Catalogue().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => catalogue.Merge(null!, anyId));
        Assert.Throws<ArgumentNullException>(() => catalogue.Merge(anyId, null!));
    }

    [Fact]
    public void An_unknown_survivor_or_absorbed_cannot_be_merged() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = TwoRecordsOfOneEdition();
        Catalogue catalogue = ACatalogueHolding(survivor);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(absorbed.Id, survivor.Id));
    }

    [Fact]
    public void A_retired_record_cannot_be_merged_again() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = TwoRecordsOfOneEdition();
        BibliographicRecord third = ABareRecord().DescribingTheSameWorkAs(survivor).WithContributors([Bakst]).Generate();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed, third);
        catalogue.Merge(survivor.Id, absorbed.Id);

        // Act
        InvalidOperationException asAbsorbed = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(third.Id, absorbed.Id));
        InvalidOperationException asSurvivor = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(absorbed.Id, third.Id));

        // Assert
        Assert.Contains("retired", asAbsorbed.Message);
        Assert.Contains("retired", asSurvivor.Message);
    }

    [Fact]
    public void A_withdrawn_record_cannot_be_merged() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = TwoRecordsOfOneEdition();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);
        catalogue.Withdraw(absorbed.Id);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));
    }

    [Fact]
    public void Titles_that_differ_beyond_case_refuse_the_merge() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Dune")).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithTitle(new Title("Dune Messiah")).WithContributors([Garnett]).Generate();

        // Assert
        AssertTheMergeIsRefusedBecause("titles differ", survivor, absorbed);
    }

    [Fact]
    public void Languages_that_differ_refuse_the_merge() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithLanguage(new LanguageTag("fr")).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithLanguage(new LanguageTag("fr-CA")).WithContributors([Garnett]).Generate();

        // Assert
        AssertTheMergeIsRefusedBecause("languages differ", survivor, absorbed);
    }

    [Fact]
    public void Scripts_that_differ_refuse_the_merge() {
        // Arrange
        LanguageTag russian = new LanguageTag("ru");

        BibliographicRecord transliterated = ABareRecord().WithScript(WritingScript.Latin).WithTitle(new Title("1984")).WithLanguage(russian).Generate();
        BibliographicRecord original       = ABareRecord().DescribingTheSameWorkAs(transliterated).WithScript(WritingScript.Cyrillic).WithContributors([Garnett]).Generate();

        // Assert
        AssertTheMergeIsRefusedBecause("scripts differ", transliterated, original);
    }

    [Fact]
    public void Editions_that_differ_refuse_the_merge() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithEdition(1).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithEdition(2).WithContributors([Garnett]).Generate();

        // Assert
        AssertTheMergeIsRefusedBecause("editions differ", survivor, absorbed);
    }

    [Fact]
    public void Years_that_differ_refuse_the_merge() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithPublicationYear(1999).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithPublicationYear(2000).WithContributors([Garnett]).Generate();

        // Assert
        AssertTheMergeIsRefusedBecause("publication years differ", survivor, absorbed);
    }

    [Fact]
    public void Conflicting_isbns_refuse_the_merge() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithIsbn(Isbn979).WithContributors([Garnett]).Generate();

        // Assert
        AssertTheMergeIsRefusedBecause("ISBNs conflict", survivor, absorbed);
    }

    [Fact]
    public void Conflicting_issns_refuse_the_merge() {
        // Arrange
        BibliographicRecord survivor = ABareRecord().WithIssn(new Issn("0378-5955")).Generate();
        BibliographicRecord absorbed = ABareRecord().DescribingTheSameWorkAs(survivor).WithIssn(new Issn("2049-3630")).WithContributors([Garnett]).Generate();

        // Assert
        AssertTheMergeIsRefusedBecause("ISSNs conflict", survivor, absorbed);
    }

    private static void AssertTheMergeIsRefusedBecause(string mismatch, BibliographicRecord survivor, BibliographicRecord absorbed) {
        // Arrange
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => catalogue.Merge(survivor.Id, absorbed.Id));

        // Assert
        Assert.Contains(mismatch, refusal.Message);
        Assert.Equal(2, catalogue.Count);
        Assert.Same(survivor, catalogue.Find(survivor.Id));
        Assert.Same(absorbed, catalogue.Find(absorbed.Id));
    }

    // ---- retired ids --------------------------------------------------------------------------------------------

    [Fact]
    public void An_id_retired_by_a_merge_is_never_reused() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = TwoRecordsOfOneEdition();
        Catalogue           catalogue = ACatalogueHolding(survivor, absorbed);
        BibliographicRecord reusesId  = Any.BibliographicRecord().WithId(absorbed.Id).WithTitle(new Title("Emma")).Generate();
        catalogue.Merge(survivor.Id, absorbed.Id);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(reusesId));

        // Assert
        Assert.Equal(DuplicateReason.RetiredRecordId, refusal.Reason);
        Assert.Equal(survivor.Id, refusal.Existing);
    }

    [Fact]
    public void The_retired_id_check_comes_before_the_isbn_check() {
        // Arrange
        (BibliographicRecord survivor, BibliographicRecord absorbed) = TwoRecordsOfOneEdition();
        Catalogue catalogue = ACatalogueHolding(survivor, absorbed);
        catalogue.Merge(survivor.Id, absorbed.Id);

        BibliographicRecord reusesIdAndIsbn = Any.BibliographicRecord().WithId(absorbed.Id).WithPublicationYear(2010).WithIsbn(Isbn978).Generate();

        // Assert
        Assert.Equal(DuplicateReason.RetiredRecordId, catalogue.CheckDuplicate(reusesIdAndIsbn));
    }

    [Fact]
    public void Successive_merges_lead_every_retired_id_to_the_last_survivor() {
        // Arrange
        BibliographicRecord first  = ABareRecord().Generate();
        BibliographicRecord second = ABareRecord().DescribingTheSameWorkAs(first).WithContributors([Garnett]).Generate();
        BibliographicRecord third  = ABareRecord().DescribingTheSameWorkAs(first).WithContributors([Bakst]).Generate();
        Catalogue catalogue = ACatalogueHolding(first, second, third);

        // Act
        catalogue.Merge(second.Id, first.Id);
        BibliographicRecord last = catalogue.Merge(third.Id, second.Id);

        // Assert
        Assert.Same(last, catalogue.Find(first.Id));
        Assert.Same(last, catalogue.Find(second.Id));
        Assert.Same(last, catalogue.Find(third.Id));
        Assert.Equal(1, catalogue.Count);
    }

    [Fact]
    public void A_record_added_under_an_id_retired_through_a_chain_names_the_last_survivor() {
        // Arrange
        BibliographicRecord first  = ABareRecord().Generate();
        BibliographicRecord second = ABareRecord().DescribingTheSameWorkAs(first).WithContributors([Garnett]).Generate();
        BibliographicRecord third  = ABareRecord().DescribingTheSameWorkAs(first).WithContributors([Bakst]).Generate();
        Catalogue catalogue = ACatalogueHolding(first, second, third);
        catalogue.Merge(second.Id, first.Id);
        catalogue.Merge(third.Id, second.Id);

        // Act
        DuplicateRecordException refusal = Assert.Throws<DuplicateRecordException>(() => catalogue.Add(Any.BibliographicRecord().WithId(first.Id).Generate()));

        // Assert
        Assert.Equal(third.Id, refusal.Existing);
    }

}
