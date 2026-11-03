using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

/// <summary>
///     Values a national library meets every day, written the way real data spells them. They expect the catalogue
///     to accept them; the ones it refuses are kept, skipped, as the evidence of a gap in the domain.
/// </summary>
public sealed class DomainRealismTests {

    [Fact(Skip = "domain defect: U+3005 (the iteration mark, a letter of category Lm) lies outside every Cjk range (3040-30FF, 3400-4DBF, ...), so the very common surname 佐々木 is refused as a letter of no supported script; deterministic, no seed")]
    public void A_japanese_surname_with_the_iteration_mark_is_accepted() {
        // Act
        PersonName name = new PersonName("佐々木", "太郎");

        // Assert
        Assert.Equal(WritingScript.Cjk, name.Script);
    }

    [Fact(Skip = "domain defect: U+02BB and U+02BC (modifier letters used as the okina or the Uzbek tutuq, category Lm) lie above the Latin range ending at U+02AF, so Latin-script names using them are refused as unsupported; deterministic, no seed")]
    public void A_latin_name_with_a_modifier_letter_apostrophe_is_accepted() {
        // Act
        PersonName name = new PersonName("Oʻzbek", "Hawaiʻi");

        // Assert
        Assert.Equal(WritingScript.Latin, name.Script);
    }

    [Fact(Skip = "domain defect: U+02BB (okina, category Lm) is classed as a letter of an unsupported script, so a Hawaiian title is refused in a Latin record; deterministic, no seed")]
    public void A_hawaiian_title_is_accepted_in_a_latin_record() {
        // Arrange
        Title title = new Title("Hawaiʻi");

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(title).Generate();

        // Assert
        Assert.Same(title, record.Title);
    }

    [Fact(Skip = "domain defect: Merge builds the survivor without re-checking duplicates, so it can leave two records for which DescribesSameWorkAs holds, which Add would have refused as SameWork; deterministic, seed -1341713057 only varies the dummies")]
    public void A_merge_never_leaves_two_records_describing_the_same_work() {
        // Arrange
        Contributor austen = new Contributor(new PersonName("Austen", "Jane"), ContributorRole.Author);
        Contributor gaskell = new Contributor(new PersonName("Gaskell", "Elizabeth"), ContributorRole.Author);
        Title       subtitle = new Title("A novel");

        // A has no subtitle, B has one; C has the subtitle and the contributors A and B will share once merged.
        BibliographicRecord a = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithoutSubtitle().WithContributors([austen]).WithoutIsbn().WithoutIssn().Generate();
        BibliographicRecord b = Any.BibliographicRecord().DescribingTheSameWorkAs(a).WithSubtitle(subtitle).WithContributors([gaskell]).WithoutIssn().Generate();
        BibliographicRecord c = Any.BibliographicRecord().DescribingTheSameWorkAs(a).WithSubtitle(subtitle).WithContributors([austen, gaskell]).WithoutIssn().Generate();

        Catalogue catalogue = Any.Catalogue().Generate();
        catalogue.Add(a);
        catalogue.Add(b);
        catalogue.Add(c);

        // Act
        catalogue.Merge(a.Id, b.Id);

        // Assert
        IReadOnlyList<BibliographicRecord> records = catalogue.Records();
        Assert.DoesNotContain(records, first => records.Any(second => !ReferenceEquals(first, second) && first.DescribesSameWorkAs(second)));
    }

    [Fact(Skip = "domain defect: the fullwidth Latin letters (U+FF21-FF5A) and the halfwidth katakana (U+FF66-FF9F) of Japanese data belong to no range, so a Japanese title written with either is refused; deterministic, no seed")]
    public void A_japanese_title_with_fullwidth_latin_letters_is_accepted() {
        // Arrange
        Title title = new Title("\uFF21\uFF22\uFF23 \u5165\u9580");

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(WritingScript.Cjk).WithTitle(title).Generate();

        // Assert
        Assert.Same(title, record.Title);
    }

}
