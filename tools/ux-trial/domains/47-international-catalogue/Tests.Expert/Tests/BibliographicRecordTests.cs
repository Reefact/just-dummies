using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class BibliographicRecordTests {

    private static readonly Isbn13 Isbn978 = new Isbn13("978-0-306-40615-7");
    private static readonly Isbn13 Isbn979 = new Isbn13("979-10-90636-07-1");

    public static TheoryData<WritingScript> AllScripts() {
        return new TheoryData<WritingScript>(Enum.GetValues<WritingScript>());
    }

    // ---- what a record holds ------------------------------------------------------------------------------------

    [Fact]
    public void A_record_keeps_what_it_is_given() {
        // Arrange
        RecordId       id           = new RecordId("FR00412873");
        Title          title        = new Title("Anna Karenina");
        Title          subtitle     = new Title("A novel in eight parts");
        Contributor    author       = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
        LanguageTag    language     = new LanguageTag("fr");
        CallNumber     callNumber   = new CallNumber("843.8 T654");

        // Act
        BibliographicRecord record = new BibliographicRecord(id, title, subtitle, [author], language, WritingScript.Latin, 3, 1999, Isbn978, null, callNumber);

        // Assert
        Assert.Same(id, record.Id);
        Assert.Same(title, record.Title);
        Assert.Same(subtitle, record.Subtitle);
        Assert.Equal([author], record.Contributors);
        Assert.Same(language, record.Language);
        Assert.Equal(WritingScript.Latin, record.Script);
        Assert.Equal(3, record.Edition);
        Assert.Equal(1999, record.PublicationYear);
        Assert.Same(Isbn978, record.Isbn);
        Assert.Null(record.Issn);
        Assert.Same(callNumber, record.CallNumber);
    }

    [Fact]
    public void A_record_is_written_with_its_id_its_title_and_its_year() {
        // Arrange
        BibliographicRecord record = Any.BibliographicRecord()
                                        .WithId(new RecordId("FR00412873"))
                                        .WithTitle(new Title("Dune"))
                                        .WithPublicationYear(1965)
                                        .Generate();

        // Assert
        Assert.Equal("FR00412873 Dune (1965)", record.ToString());
    }

    // ---- required parts -----------------------------------------------------------------------------------------

    [Fact]
    public void A_record_needs_an_id() {
        Assert.Throws<ArgumentNullException>(() => Any.BibliographicRecord().WithId((RecordId)null!).Generate());
    }

    [Fact]
    public void A_record_needs_a_title() {
        Assert.Throws<ArgumentNullException>(() => Any.BibliographicRecord().WithTitle((Title)null!).Generate());
    }

    [Fact]
    public void A_record_needs_a_list_of_contributors_even_if_it_is_empty() {
        Assert.Throws<ArgumentNullException>(() => Any.BibliographicRecord().WithContributors((IEnumerable<Contributor>)null!).Generate());
    }

    [Fact]
    public void A_record_needs_a_language() {
        // Arrange
        BibliographicRecord valid = Any.BibliographicRecord().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(valid.Id, valid.Title, valid.Subtitle, valid.Contributors, null!, valid.Script, valid.Edition, valid.PublicationYear));
    }

    [Fact]
    public void A_record_needs_a_defined_script() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.BibliographicRecord().WithScript((WritingScript)99).Generate());
    }

    [Fact]
    public void A_record_may_have_no_contributor_subtitle_isbn_issn_or_call_number() {
        // Act
        BibliographicRecord bare = Any.BibliographicRecord()
                                      .WithoutSubtitle()
                                      .WithContributors([])
                                      .WithoutIsbn()
                                      .WithoutIssn()
                                      .WithoutCallNumber()
                                      .Generate();

        // Assert
        Assert.Null(bare.Subtitle);
        Assert.Empty(bare.Contributors);
        Assert.Null(bare.Isbn);
        Assert.Null(bare.Issn);
        Assert.Null(bare.CallNumber);
    }

    // ---- contributors -------------------------------------------------------------------------------------------

    [Fact]
    public void A_record_lists_up_to_twenty_contributors() {
        // Arrange
        List<Contributor> twenty = Any.ListOf(Any.Contributor()).WithCount(BibliographicRecord.MaxContributors).Distinct().Generate();

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithContributors(twenty).Generate();

        // Assert
        Assert.Equal(20, record.Contributors.Count);
    }

    [Fact]
    public void A_record_cannot_list_twenty_one_contributors() {
        // Arrange
        List<Contributor> twentyOne = Any.ListOf(Any.Contributor()).WithCount(BibliographicRecord.MaxContributors + 1).Distinct().Generate();

        // Act & Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithContributors(twentyOne).Generate());
        Assert.Equal("contributors", refusal.ParamName);
    }

    [Fact]
    public void Contributors_keep_the_order_of_the_title_page() {
        // Arrange
        Contributor first  = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
        Contributor second = new Contributor(new PersonName("Garnett", "Constance"), ContributorRole.Translator);
        Contributor third  = new Contributor(new PersonName("Bakst", "Léon"), ContributorRole.Illustrator);

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithContributors([first, second, third]).Generate();

        // Assert
        Assert.Equal([first, second, third], record.Contributors);
    }

    [Fact]
    public void A_contributor_cannot_appear_twice_with_the_same_role() {
        // Arrange
        Contributor anyContributor = Any.Contributor().Generate();

        // Act & Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithContributors([anyContributor, anyContributor]).Generate());
        Assert.Equal("contributors", refusal.ParamName);
    }

    [Fact]
    public void Two_spellings_of_one_name_in_one_role_are_the_same_contributor() {
        // Arrange
        Contributor lower = new Contributor(new PersonName("tolstoï", "léon"), ContributorRole.Author);
        Contributor upper = new Contributor(new PersonName("TOLSTOÏ", "LÉON"), ContributorRole.Author);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithContributors([lower, upper]).Generate());
    }

    [Fact]
    public void One_person_may_hold_two_roles() {
        // Arrange
        PersonName person = new PersonName("Tolstoï", "Léon");

        // Act
        BibliographicRecord record = Any.BibliographicRecord()
                                        .WithContributors([new Contributor(person, ContributorRole.Author), new Contributor(person, ContributorRole.Editor)])
                                        .Generate();

        // Assert
        Assert.Equal(2, record.Contributors.Count);
    }

    [Fact]
    public void A_record_cannot_list_a_null_contributor() {
        Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithContributors([null!]).Generate());
    }

    [Fact]
    public void Contributor_names_ignore_the_roles() {
        // Arrange
        PersonName tolstoi = new PersonName("Tolstoï", "Léon");
        PersonName garnett = new PersonName("Garnett", "Constance");

        BibliographicRecord record = Any.BibliographicRecord()
                                        .WithContributors([new Contributor(tolstoi, ContributorRole.Author),
                                                           new Contributor(tolstoi, ContributorRole.Editor),
                                                           new Contributor(garnett, ContributorRole.Translator)])
                                        .Generate();

        // Act
        HashSet<PersonName> names = record.ContributorNames();

        // Assert
        Assert.Equal(2, names.Count);
        Assert.Contains(tolstoi, names);
        Assert.Contains(garnett, names);
    }

    // ---- edition and year ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(999)]
    public void An_edition_is_numbered_from_1_to_999(int edition) {
        Assert.Equal(edition, Any.BibliographicRecord().WithEdition(edition).Generate().Edition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void An_edition_outside_1_to_999_is_refused(int edition) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.BibliographicRecord().WithEdition(edition).Generate());
    }

    [Fact]
    public void The_first_printing_year_is_1450() {
        // Act
        BibliographicRecord incunabulum = Any.BibliographicRecord().WithPublicationYear(1450).Generate();

        // Assert
        Assert.Equal(1450, incunabulum.PublicationYear);
    }

    [Theory]
    [InlineData(1449)]
    [InlineData(0)]
    [InlineData(-500)]
    public void A_publication_year_before_1450_is_refused(int year) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.BibliographicRecord().WithPublicationYear(year).Generate());
    }

    [Fact]
    public void A_record_may_announce_a_book_for_next_year() {
        // Arrange
        int nextYear = DateTime.UtcNow.Year + 1;

        // Act
        BibliographicRecord forthcoming = Any.BibliographicRecord().WithPublicationYear(nextYear).Generate();

        // Assert
        Assert.Equal(nextYear, forthcoming.PublicationYear);
    }

    [Fact]
    public void A_record_cannot_announce_a_book_two_years_ahead() {
        // Arrange
        int inTwoYears = DateTime.UtcNow.Year + 2;

        // Act & Assert
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BibliographicRecord().WithPublicationYear(inTwoYears).Generate());
        Assert.Equal("publicationYear", refusal.ParamName);
    }

    // ---- isbn and year ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1970)]
    [InlineData(1971)]
    [InlineData(2006)]
    public void An_isbn_978_is_accepted_from_1970(int year) {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithPublicationYear(year).WithIsbn(Isbn978).Generate();

        // Assert
        Assert.Same(Isbn978, record.Isbn);
    }

    [Theory]
    [InlineData(1969)]
    [InlineData(1450)]
    public void An_isbn_cannot_be_borne_by_an_edition_older_than_1970(int year) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithPublicationYear(year).WithIsbn(Isbn978).Generate());

        // Assert
        Assert.Equal("isbn", refusal.ParamName);
    }

    [Theory]
    [InlineData(2007)]
    [InlineData(2008)]
    public void An_isbn_979_is_accepted_from_2007(int year) {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithPublicationYear(year).WithIsbn(Isbn979).Generate();

        // Assert
        Assert.Same(Isbn979, record.Isbn);
    }

    [Theory]
    [InlineData(2006)]
    [InlineData(1970)]
    [InlineData(1969)]
    public void An_isbn_979_is_refused_before_2007(int year) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithPublicationYear(year).WithIsbn(Isbn979).Generate());

        // Assert
        Assert.Equal("isbn", refusal.ParamName);
    }

    [Fact]
    public void An_old_edition_without_an_isbn_is_fine() {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithPublicationYear(1860).WithoutIsbn().Generate();

        // Assert
        Assert.Null(record.Isbn);
    }

    // ---- the script of the texts --------------------------------------------------------------------------------

    [Theory]
    [InlineData("Война и мир", WritingScript.Cyrillic)]
    [InlineData("Αθήνα", WritingScript.Greek)]
    [InlineData("שלום", WritingScript.Hebrew)]
    [InlineData("القاهرة", WritingScript.Arabic)]
    [InlineData("द\u093Fल\u094Dल\u0940", WritingScript.Devanagari)]
    [InlineData("東京物語", WritingScript.Cjk)]
    [InlineData("Dune", WritingScript.Latin)]
    public void A_title_is_accepted_in_its_own_script(string text, WritingScript script) {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(script).WithTitle(new Title(text)).Generate();

        // Assert
        Assert.Equal(text, record.Title.Value);
    }

    [Theory]
    [InlineData("Война и мир", WritingScript.Latin)]
    [InlineData("Dune", WritingScript.Cyrillic)]
    [InlineData("Αθήνα", WritingScript.Cjk)]
    [InlineData("東京物語", WritingScript.Latin)]
    [InlineData("Moscou Москва", WritingScript.Latin)]
    [InlineData("Moscou Москва", WritingScript.Cyrillic)]
    public void A_title_is_refused_in_another_script(string text, WritingScript script) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithScript(script).WithTitle(new Title(text)).Generate());

        // Assert
        Assert.Equal("title", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(AllScripts))]
    public void A_title_without_letters_fits_any_script(WritingScript script) {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(script).WithTitle(new Title("1984")).Generate();

        // Assert
        Assert.Equal(script, record.Script);
    }

    [Theory]
    [InlineData(WritingScript.Latin)]
    [InlineData(WritingScript.Cjk)]
    public void A_title_in_an_unsupported_script_fits_no_script(WritingScript script) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithScript(script).WithTitle(new Title("ไทย")).Generate());

        // Assert
        Assert.Equal("title", refusal.ParamName);
    }

    [Fact]
    public void A_title_mixing_a_supported_script_with_an_unsupported_one_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Bangkok กร\u0E38งเทพ")).Generate());
    }

    [Fact]
    public void A_subtitle_is_held_to_the_same_script_as_the_title() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithScript(WritingScript.Latin).WithSubtitle(new Title("Роман")).Generate());

        // Assert
        Assert.Equal("subtitle", refusal.ParamName);
    }

    [Fact]
    public void A_subtitle_in_the_script_of_the_record_is_accepted() {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(WritingScript.Cyrillic).WithSubtitle(new Title("Роман")).Generate();

        // Assert
        Assert.Equal("Роман", record.Subtitle!.Value);
    }

    [Fact]
    public void A_title_in_script_and_a_subtitle_with_no_letters_are_fine() {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(WritingScript.Greek).WithTitle(new Title("Ιλιάδα")).WithSubtitle(new Title("24")).Generate();

        // Assert
        Assert.Equal("24", record.Subtitle!.Value);
    }

    // ---- the script of the language tag -------------------------------------------------------------------------

    [Theory]
    [InlineData("sr-Latn", WritingScript.Latin)]
    [InlineData("sr-Cyrl", WritingScript.Cyrillic)]
    [InlineData("zh-Hant", WritingScript.Cjk)]
    [InlineData("ja-Jpan", WritingScript.Cjk)]
    [InlineData("ru", WritingScript.Latin)]
    [InlineData("ru", WritingScript.Cyrillic)]
    [InlineData("ru-RU", WritingScript.Latin)]
    public void A_language_tag_that_names_no_script_or_the_declared_one_is_accepted(string tag, WritingScript script) {
        // Arrange
        LanguageTag language = new LanguageTag(tag);

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(script).WithTitle(new Title("1984")).WithLanguage(language).Generate();

        // Assert
        Assert.Same(language, record.Language);
    }

    [Theory]
    [InlineData("sr-Latn", WritingScript.Cyrillic)]
    [InlineData("sr-Cyrl", WritingScript.Latin)]
    [InlineData("zh-Hant", WritingScript.Latin)]
    [InlineData("ja-Jpan", WritingScript.Cyrillic)]
    [InlineData("und-Thai", WritingScript.Latin)]
    public void A_language_tag_naming_another_script_is_refused(string tag, WritingScript script) {
        // Arrange
        LanguageTag language = new LanguageTag(tag);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithScript(script).WithTitle(new Title("1984")).WithLanguage(language).Generate());

        // Assert
        Assert.Equal("language", refusal.ParamName);
    }

    // ---- serials and the other optional identifiers -------------------------------------------------------------

    [Fact]
    public void A_record_with_an_issn_and_no_isbn_is_a_serial() {
        // Act
        BibliographicRecord serial = Any.BibliographicRecord().WithoutIsbn().WithIssn(new Issn("0378-5955")).Generate();

        // Assert
        Assert.True(serial.IsSerial);
    }

    [Fact]
    public void A_record_with_an_issn_and_an_isbn_is_not_a_serial() {
        // Act
        BibliographicRecord monograph = Any.BibliographicRecord().WithPublicationYear(1995).WithIsbn(Isbn978).WithIssn(new Issn("0378-5955")).Generate();

        // Assert
        Assert.False(monograph.IsSerial);
    }

    [Fact]
    public void A_record_without_an_issn_is_not_a_serial() {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithoutIssn().Generate();

        // Assert
        Assert.False(record.IsSerial);
    }

    // ---- describing the same work -------------------------------------------------------------------------------

    [Fact]
    public void A_record_describes_the_same_work_as_one_equal_in_every_respect_but_its_id() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().Generate();
        BibliographicRecord twin     = Any.BibliographicRecord().DescribingTheSameWorkAs(original).Generate();

        // Act
        bool same = original.DescribesSameWorkAs(twin);

        // Assert
        Assert.True(same);
        Assert.True(twin.DescribesSameWorkAs(original));
    }

    [Fact]
    public void Titles_are_compared_without_regard_to_case_or_normalisation() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Café")).Generate();
        BibliographicRecord twin     = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithTitle(new Title("CAFE\u0301")).Generate();

        // Assert
        Assert.True(original.DescribesSameWorkAs(twin));
    }

    [Fact]
    public void Roles_are_ignored_when_comparing_the_contributors() {
        // Arrange
        PersonName person = new PersonName("Tolstoï", "Léon");

        BibliographicRecord original = Any.BibliographicRecord().WithContributors([new Contributor(person, ContributorRole.Author)]).Generate();
        BibliographicRecord twin     = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithContributors([new Contributor(person, ContributorRole.Editor)]).Generate();

        // Assert
        Assert.True(original.DescribesSameWorkAs(twin));
    }

    [Fact]
    public void The_order_of_the_contributors_does_not_matter() {
        // Arrange
        Contributor tolstoi = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
        Contributor garnett = new Contributor(new PersonName("Garnett", "Constance"), ContributorRole.Translator);

        BibliographicRecord original = Any.BibliographicRecord().WithContributors([tolstoi, garnett]).Generate();
        BibliographicRecord twin     = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithContributors([garnett, tolstoi]).Generate();

        // Assert
        Assert.True(original.DescribesSameWorkAs(twin));
    }

    [Fact]
    public void Another_set_of_contributors_describes_another_work() {
        // Arrange
        Contributor tolstoi  = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);
        Contributor tchekhov = new Contributor(new PersonName("Tchekhov", "Anton"), ContributorRole.Author);

        BibliographicRecord original = Any.BibliographicRecord().WithContributors([tolstoi]).Generate();
        BibliographicRecord other    = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithContributors([tchekhov]).Generate();
        BibliographicRecord superset = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithContributors([tolstoi, tchekhov]).Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
        Assert.False(original.DescribesSameWorkAs(superset));
    }

    [Fact]
    public void Another_title_describes_another_work() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Dune")).Generate();
        BibliographicRecord other    = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithTitle(new Title("Dune Messiah")).Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
    }

    [Fact]
    public void A_subtitle_that_is_missing_on_one_side_describes_another_work() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithSubtitle(new Title("A novel")).Generate();
        BibliographicRecord other    = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithoutSubtitle().Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
        Assert.False(other.DescribesSameWorkAs(original));
    }

    [Fact]
    public void A_different_subtitle_describes_another_work() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithSubtitle(new Title("A novel")).Generate();
        BibliographicRecord other    = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithSubtitle(new Title("A play")).Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
    }

    [Theory]
    [InlineData("fr", "fr-CA")]
    [InlineData("fr", "en")]
    [InlineData("sr-Latn", "sr-Latn-RS")]
    public void Another_language_tag_describes_another_work(string tag, string otherTag) {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithLanguage(new LanguageTag(tag)).Generate();
        BibliographicRecord other    = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithLanguage(new LanguageTag(otherTag)).Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
    }

    [Fact]
    public void Another_edition_describes_another_work() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithEdition(2).Generate();
        BibliographicRecord other    = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithEdition(3).Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
    }

    [Fact]
    public void Another_year_describes_another_work() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithPublicationYear(1999).Generate();
        BibliographicRecord other    = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithPublicationYear(2000).Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
    }

    [Fact]
    public void Two_different_isbns_mark_two_manifestations_of_the_work() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().DescribingTheSameWorkAs(Any.BibliographicRecord().WithPublicationYear(2010).Generate()).WithIsbn(Isbn978).Generate();
        BibliographicRecord other    = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithIsbn(Isbn979).Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
    }

    [Fact]
    public void An_isbn_on_one_side_only_leaves_the_records_describing_the_same_work() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        BibliographicRecord twin     = Any.BibliographicRecord().DescribingTheSameWorkAs(original).Generate();

        // Assert
        Assert.True(original.DescribesSameWorkAs(twin));
        Assert.True(twin.DescribesSameWorkAs(original));
    }

    [Fact]
    public void The_same_isbn_on_both_sides_leaves_the_records_describing_the_same_work() {
        // Arrange
        BibliographicRecord original = Any.BibliographicRecord().WithPublicationYear(2010).WithIsbn(Isbn978).Generate();
        BibliographicRecord twin     = Any.BibliographicRecord().DescribingTheSameWorkAs(original).WithIsbn(Isbn978).Generate();

        // Assert
        Assert.True(original.DescribesSameWorkAs(twin));
    }

    [Fact]
    public void Comparing_with_nothing_is_a_mistake() {
        // Arrange
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyRecord.DescribesSameWorkAs(null!));
    }

}
