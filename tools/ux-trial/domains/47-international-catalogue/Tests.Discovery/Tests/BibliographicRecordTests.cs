using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class BibliographicRecordTests {

    private static readonly int LatestYear = DateTime.UtcNow.Year + 1;

    private static BibliographicRecord Build(
        Title? title = null,
        Title? subtitle = null,
        IEnumerable<Contributor>? contributors = null,
        LanguageTag? language = null,
        WritingScript script = WritingScript.Latin,
        int edition = 1,
        int publicationYear = 2001,
        Isbn13? isbn = null,
        Issn? issn = null,
        CallNumber? callNumber = null) {
        // The identifier and, unless the test is about them, the text are dummies.
        return new BibliographicRecord(Any.RecordId().Generate(),
                                       title ?? Any.Title().InScript(script).Generate(),
                                       subtitle,
                                       contributors ?? [],
                                       language ?? new LanguageTag("fr"),
                                       script,
                                       edition,
                                       publicationYear,
                                       isbn,
                                       issn,
                                       callNumber);
    }

    [Fact]
    public void A_record_exposes_what_it_was_built_from() {
        // Arrange
        RecordId anyId = Any.RecordId().Generate();
        Title anySubtitle = Any.Title().InScript(WritingScript.Cyrillic).Generate();
        Contributor anyContributor = Any.Contributor().Generate();
        Issn anyIssn = Any.Issn().Generate();
        CallNumber anyCallNumber = Any.CallNumber().Generate();
        Title title = new Title("Война и мир");
        Isbn13 isbn = new Isbn13("978-2-07-036822-8");

        // Act
        BibliographicRecord record = new BibliographicRecord(anyId, title, anySubtitle, [anyContributor], new LanguageTag("ru"), WritingScript.Cyrillic, 3, 1999, isbn, anyIssn, anyCallNumber);

        // Assert
        Assert.Same(anyId, record.Id);
        Assert.Same(title, record.Title);
        Assert.Same(anySubtitle, record.Subtitle);
        Assert.Equal([anyContributor], record.Contributors);
        Assert.Equal("ru", record.Language.Value);
        Assert.Equal(WritingScript.Cyrillic, record.Script);
        Assert.Equal(3, record.Edition);
        Assert.Equal(1999, record.PublicationYear);
        Assert.Same(isbn, record.Isbn);
        Assert.Same(anyIssn, record.Issn);
        Assert.Same(anyCallNumber, record.CallNumber);
    }

    [Fact]
    public void A_record_keeps_its_contributors_in_the_order_of_the_title_page() {
        // Arrange
        Contributor author = new Contributor(new PersonName("Zola"), ContributorRole.Author);
        Contributor illustrator = new Contributor(new PersonName("Adam"), ContributorRole.Illustrator);
        Contributor editor = new Contributor(new PersonName("Moulin"), ContributorRole.Editor);

        // Act
        BibliographicRecord record = Build(contributors: [author, illustrator, editor]);

        // Assert
        Assert.Equal([author, illustrator, editor], record.Contributors);
    }

    [Fact]
    public void A_record_requires_an_id_a_title_contributors_and_a_language() {
        // Arrange
        RecordId anyId = Any.RecordId().Generate();
        Title anyTitle = Any.Title().InScript(WritingScript.Latin).Generate();
        LanguageTag anyLanguage = new LanguageTag("fr");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(null!, anyTitle, null, [], anyLanguage, WritingScript.Latin, 1, 2001));
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(anyId, null!, null, [], anyLanguage, WritingScript.Latin, 1, 2001));
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(anyId, anyTitle, null, null!, anyLanguage, WritingScript.Latin, 1, 2001));
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(anyId, anyTitle, null, [], null!, WritingScript.Latin, 1, 2001));
    }

    [Fact]
    public void A_record_refuses_an_undefined_script() {
        // Arrange
        Title anyTitle = Any.Title().InScript(WritingScript.Latin).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(title: anyTitle, script: (WritingScript)42));
    }

    [Fact]
    public void A_record_refuses_a_null_contributor() {
        // Arrange
        Contributor anyContributor = Any.Contributor().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Build(contributors: [anyContributor, null!]));
    }

    [Fact]
    public void A_record_may_list_twenty_contributors_but_not_twenty_one() {
        // Arrange
        List<Contributor> twentyOne = Any.ListOf(Any.Contributor()).WithCount(21).Distinct().Generate();

        // Act
        BibliographicRecord record = Build(contributors: twentyOne.Take(20));

        // Assert
        Assert.Equal(20, record.Contributors.Count);
        Assert.Throws<ArgumentException>(() => Build(contributors: twentyOne));
    }

    [Fact]
    public void A_record_refuses_a_contributor_listed_twice_with_the_same_role() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();
        PersonName sameNameInCapitals = new PersonName(anyName.FamilyName.ToUpperInvariant(), anyName.GivenNames?.ToUpperInvariant());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Build(contributors: [new Contributor(anyName, ContributorRole.Author), new Contributor(sameNameInCapitals, ContributorRole.Author)]));
    }

    [Fact]
    public void A_record_accepts_one_person_in_two_roles() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Act
        BibliographicRecord record = Build(contributors: [new Contributor(anyName, ContributorRole.Author), new Contributor(anyName, ContributorRole.Translator)]);

        // Assert
        Assert.Equal(2, record.Contributors.Count);
        Assert.Single(record.ContributorNames());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(999)]
    public void An_edition_from_one_to_999_is_accepted(int edition) {
        Assert.Equal(edition, Build(edition: edition).Edition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void An_edition_outside_one_to_999_is_refused(int edition) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(edition: edition));
    }

    [Theory]
    [InlineData(1450)]
    [InlineData(1451)]
    [InlineData(2000)]
    public void A_publication_year_from_1450_is_accepted(int year) {
        Assert.Equal(year, Build(publicationYear: year).PublicationYear);
    }

    [Fact]
    public void A_publication_year_may_be_next_year_and_not_the_one_after() {
        // Act
        BibliographicRecord record = Build(publicationYear: LatestYear);

        // Assert
        Assert.Equal(LatestYear, record.PublicationYear);
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(publicationYear: LatestYear + 1));
    }

    [Theory]
    [InlineData(1449)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public void A_publication_year_before_the_first_printing_is_refused(int year) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(publicationYear: year));
    }

    [Theory]
    [InlineData(1970)]
    [InlineData(1980)]
    [InlineData(2006)]
    public void An_isbn_978_is_accepted_from_1970(int year) {
        // Arrange
        Isbn13 isbn978 = new Isbn13("9780306406157");

        // Act
        BibliographicRecord record = Build(publicationYear: year, isbn: isbn978);

        // Assert
        Assert.Same(isbn978, record.Isbn);
    }

    [Theory]
    [InlineData(1969)]
    [InlineData(1900)]
    [InlineData(1450)]
    public void An_isbn_cannot_be_borne_by_an_edition_published_before_1970(int year) {
        Assert.Throws<ArgumentException>(() => Build(publicationYear: year, isbn: new Isbn13("9780306406157")));
    }

    [Theory]
    [InlineData(2007)]
    [InlineData(2020)]
    public void An_isbn_979_is_accepted_from_2007(int year) {
        Assert.NotNull(Build(publicationYear: year, isbn: new Isbn13("9791090636071")).Isbn);
    }

    [Theory]
    [InlineData(2006)]
    [InlineData(1990)]
    [InlineData(1970)]
    public void An_isbn_979_cannot_be_borne_by_an_edition_published_before_2007(int year) {
        Assert.Throws<ArgumentException>(() => Build(publicationYear: year, isbn: new Isbn13("9791090636071")));
    }

    [Fact]
    public void A_record_without_an_isbn_may_be_as_old_as_the_first_printing() {
        Assert.Null(Build(publicationYear: 1450, isbn: null).Isbn);
    }

    [Theory]
    [InlineData("Война и мир", WritingScript.Latin)]
    [InlineData("Café", WritingScript.Cyrillic)]
    [InlineData("Café", WritingScript.Greek)]
    [InlineData("東京物語", WritingScript.Arabic)]
    [InlineData("Moscou Москва", WritingScript.Latin)]
    [InlineData("Moscou Москва", WritingScript.Cyrillic)]
    public void A_title_must_be_written_in_the_declared_script(string text, WritingScript declared) {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Build(title: new Title(text), script: declared));

        // Assert
        Assert.Equal("title", error.ParamName);
    }

    [Theory]
    [InlineData("Война и мир", WritingScript.Latin)]
    [InlineData("東京物語", WritingScript.Hebrew)]
    public void A_subtitle_must_be_written_in_the_declared_script(string text, WritingScript declared) {
        // Arrange
        Title anyTitle = Any.Title().InScript(declared).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Build(title: anyTitle, subtitle: new Title(text), script: declared));

        // Assert
        Assert.Equal("subtitle", error.ParamName);
    }

    [Theory]
    [InlineData("1984", WritingScript.Latin)]
    [InlineData("1984", WritingScript.Cyrillic)]
    [InlineData("1984", WritingScript.Arabic)]
    [InlineData("42 !", WritingScript.Cjk)]
    public void A_title_without_letters_fits_any_script(string text, WritingScript declared) {
        Assert.Equal(text, Build(title: new Title(text), script: declared).Title.Value);
    }

    [Theory]
    [InlineData("หนังสือ")]
    [InlineData("Livre หนังสือ")]
    public void A_title_in_an_unsupported_script_is_refused_whatever_the_declared_script(string text) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Build(title: new Title(text), script: WritingScript.Latin));
        Assert.Throws<ArgumentException>(() => Build(title: new Title(text), script: WritingScript.Cjk));
    }

    [Fact]
    public void A_title_may_hold_digits_and_punctuation_beside_its_letters() {
        Assert.NotNull(Build(title: new Title("Fahrenheit 451 : roman, 2e éd."), script: WritingScript.Latin));
    }

    [Theory]
    [InlineData("sr-Latn", WritingScript.Cyrillic)]
    [InlineData("sr-Cyrl", WritingScript.Latin)]
    [InlineData("zh-Hans", WritingScript.Latin)]
    [InlineData("ar-Arab", WritingScript.Hebrew)]
    [InlineData("th-Thai", WritingScript.Latin)]
    [InlineData("th-Thai", WritingScript.Cjk)]
    public void A_script_subtag_must_designate_the_declared_script(string tag, WritingScript declared) {
        // Arrange
        Title anyTitle = Any.Title().InScript(declared).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Build(title: anyTitle, language: new LanguageTag(tag), script: declared));

        // Assert
        Assert.Equal("language", error.ParamName);
    }

    [Theory]
    [InlineData("sr-Latn", WritingScript.Latin)]
    [InlineData("sr-Cyrl-RS", WritingScript.Cyrillic)]
    [InlineData("zh-Hant", WritingScript.Cjk)]
    [InlineData("ja-Jpan-JP", WritingScript.Cjk)]
    [InlineData("hi-Deva", WritingScript.Devanagari)]
    public void A_script_subtag_that_designates_the_declared_script_is_accepted(string tag, WritingScript declared) {
        // Arrange
        Title anyTitle = Any.Title().InScript(declared).Generate();

        // Act
        BibliographicRecord record = Build(title: anyTitle, language: new LanguageTag(tag), script: declared);

        // Assert
        Assert.Equal(tag.ToLowerInvariant(), record.Language.Value.ToLowerInvariant());
    }

    [Theory]
    [InlineData("ru", WritingScript.Latin)]
    [InlineData("ar", WritingScript.Latin)]
    [InlineData("fr-FR", WritingScript.Greek)]
    public void A_language_without_a_script_subtag_goes_with_any_script_so_that_transliterations_are_recorded(string tag, WritingScript declared) {
        // Arrange
        Title anyTitle = Any.Title().InScript(declared).Generate();

        // Act
        BibliographicRecord record = Build(title: anyTitle, language: new LanguageTag(tag), script: declared);

        // Assert
        Assert.Equal(declared, record.Script);
    }

    [Fact]
    public void A_record_with_an_issn_and_no_isbn_is_a_serial() {
        // Arrange
        Issn anyIssn = Any.Issn().Generate();
        Isbn13 anyIsbn = new Isbn13("9780306406157");

        // Act & Assert
        Assert.True(Build(issn: anyIssn).IsSerial);
        Assert.False(Build(issn: anyIssn, isbn: anyIsbn).IsSerial);
        Assert.False(Build(isbn: anyIsbn).IsSerial);
        Assert.False(Build().IsSerial);
    }

    [Fact]
    public void A_record_is_described_by_its_id_title_and_year() {
        // Arrange
        RecordId id = new RecordId("FR00412873");
        BibliographicRecord record = new BibliographicRecord(id, new Title("La Peste"), null, [], new LanguageTag("fr"), WritingScript.Latin, 1, 1947);

        // Assert
        Assert.Equal("FR00412873 La Peste (1947)", record.ToString());
    }

    // ---- DescribesSameWorkAs ----

    private static AnyBibliographicRecord SameEditionAs(Title title, params Contributor[] contributors) {
        return Any.BibliographicRecord()
                  .WithTitle(title)
                  .WithSubtitle(null)
                  .WithLanguage(new LanguageTag("fr"))
                  .WithEdition(2)
                  .WithPublicationYear(1999)
                  .WithContributors(contributors)
                  .WithoutIsbn();
    }

    private static readonly Contributor Camus = new Contributor(new PersonName("Camus", "Albert"), ContributorRole.Author);

    private static readonly Contributor Gallimard = new Contributor(new PersonName("Gallimard", "Gaston"), ContributorRole.Editor);

    [Fact]
    public void Two_records_of_the_same_edition_describe_the_same_work() {
        // Arrange
        BibliographicRecord record = SameEditionAs(new Title("La Peste"), Camus, Gallimard).Generate();
        BibliographicRecord other = SameEditionAs(new Title("LA PESTE"), Gallimard, Camus).Generate();

        // Act & Assert
        Assert.True(record.DescribesSameWorkAs(other));
        Assert.True(other.DescribesSameWorkAs(record));
    }

    [Fact]
    public void The_roles_of_the_contributors_are_ignored_when_comparing_works() {
        // Arrange
        Contributor camusAsEditor = new Contributor(Camus.Name, ContributorRole.Editor);
        BibliographicRecord record = SameEditionAs(new Title("La Peste"), Camus).Generate();
        BibliographicRecord other = SameEditionAs(new Title("La Peste"), camusAsEditor).Generate();

        // Act & Assert
        Assert.True(record.DescribesSameWorkAs(other));
    }

    [Fact]
    public void Records_with_different_contributor_names_describe_different_works() {
        // Arrange
        BibliographicRecord record = SameEditionAs(new Title("La Peste"), Camus).Generate();
        BibliographicRecord other = SameEditionAs(new Title("La Peste"), Camus, Gallimard).Generate();

        // Act & Assert
        Assert.False(record.DescribesSameWorkAs(other));
    }

    [Fact]
    public void Records_with_different_isbns_never_describe_the_same_work() {
        // Arrange
        BibliographicRecord record = SameEditionAs(new Title("La Peste"), Camus).WithIsbn(new Isbn13("9782070368228")).Generate();
        BibliographicRecord other = SameEditionAs(new Title("La Peste"), Camus).WithIsbn(new Isbn13("9780306406157")).Generate();

        // Act & Assert
        Assert.False(record.DescribesSameWorkAs(other));
    }

    [Fact]
    public void A_record_with_an_isbn_still_describes_the_same_work_as_one_without() {
        // Arrange
        BibliographicRecord withIsbn = SameEditionAs(new Title("La Peste"), Camus).WithIsbn(new Isbn13("9782070368228")).Generate();
        BibliographicRecord withoutIsbn = SameEditionAs(new Title("La Peste"), Camus).Generate();

        // Act & Assert
        Assert.True(withIsbn.DescribesSameWorkAs(withoutIsbn));
        Assert.True(withoutIsbn.DescribesSameWorkAs(withIsbn));
    }

    [Fact]
    public void Equal_isbns_do_not_separate_two_records_of_the_same_edition() {
        // Arrange
        Isbn13 isbn = new Isbn13("9782070368228");
        BibliographicRecord record = SameEditionAs(new Title("La Peste"), Camus).WithIsbn(isbn).Generate();
        BibliographicRecord other = SameEditionAs(new Title("La Peste"), Camus).WithIsbn(new Isbn13("978-2-07-036822-8")).Generate();

        // Act & Assert
        Assert.True(record.DescribesSameWorkAs(other));
    }

    [Fact]
    public void A_record_of_another_edition_year_or_language_describes_another_work() {
        // Arrange
        AnyBibliographicRecord edition = SameEditionAs(new Title("La Peste"), Camus);
        BibliographicRecord record = edition.Generate();

        // Act & Assert
        Assert.False(record.DescribesSameWorkAs(edition.WithEdition(3).Generate()));
        Assert.False(record.DescribesSameWorkAs(edition.WithPublicationYear(2000).Generate()));
        Assert.False(record.DescribesSameWorkAs(edition.WithLanguage(new LanguageTag("fr-CA")).Generate()));
        Assert.False(record.DescribesSameWorkAs(edition.WithLanguage(new LanguageTag("en")).Generate()));
    }

    [Fact]
    public void A_record_with_a_subtitle_differs_from_one_without() {
        // Arrange
        Title subtitle = new Title("Chronique");
        BibliographicRecord record = SameEditionAs(new Title("La Peste"), Camus).WithSubtitle(subtitle).Generate();
        BibliographicRecord same = SameEditionAs(new Title("La Peste"), Camus).WithSubtitle(new Title("CHRONIQUE")).Generate();
        BibliographicRecord without = SameEditionAs(new Title("La Peste"), Camus).Generate();

        // Act & Assert
        Assert.True(record.DescribesSameWorkAs(same));
        Assert.False(record.DescribesSameWorkAs(without));
        Assert.False(without.DescribesSameWorkAs(record));
    }

    [Fact]
    public void Two_records_without_contributors_and_with_the_same_edition_describe_the_same_work() {
        // Arrange
        BibliographicRecord record = SameEditionAs(new Title("Annuaire")).Generate();
        BibliographicRecord other = SameEditionAs(new Title("Annuaire")).Generate();

        // Act & Assert
        Assert.True(record.DescribesSameWorkAs(other));
    }

    [Fact]
    public void Describing_the_same_work_as_nothing_is_refused() {
        // Arrange
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyRecord.DescribesSameWorkAs(null!));
    }

    [Fact]
    public void Generated_records_are_all_valid() {
        // The generator ties script, language, year and ISBN together; drawing many records checks it honours the constructor.
        for (int i = 0; i < 200; i++) {
            Assert.NotNull(Any.BibliographicRecord().Generate());
        }
    }

}
