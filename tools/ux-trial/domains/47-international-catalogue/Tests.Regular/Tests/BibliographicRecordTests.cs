using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class BibliographicRecordTests {

    [Fact]
    public void A_record_exposes_what_it_was_built_from() {
        // Arrange
        RecordId     id           = new RecordId("FR00412873");
        Title        title        = new Title("Les Misérables");
        Title        subtitle     = new Title("Tome premier");
        Contributor  hugo         = Records.Author("Hugo", "Victor");
        LanguageTag  language     = new LanguageTag("fr");
        Isbn13       isbn         = new Isbn13("9780306406157");
        Issn         issn         = new Issn("0378-5955");
        CallNumber   callNumber   = new CallNumber("843.7 H894");

        // Act
        BibliographicRecord record = new BibliographicRecord(id, title, subtitle, [hugo], language, WritingScript.Latin, 3, 1985, isbn, issn, callNumber);

        // Assert
        Assert.Same(id, record.Id);
        Assert.Same(title, record.Title);
        Assert.Same(subtitle, record.Subtitle);
        Assert.Equal([hugo], record.Contributors);
        Assert.Same(language, record.Language);
        Assert.Equal(WritingScript.Latin, record.Script);
        Assert.Equal(3, record.Edition);
        Assert.Equal(1985, record.PublicationYear);
        Assert.Same(isbn, record.Isbn);
        Assert.Same(issn, record.Issn);
        Assert.Same(callNumber, record.CallNumber);
        Assert.Equal("FR00412873 Les Misérables (1985)", record.ToString());
    }

    [Fact]
    public void The_optional_parts_default_to_absent() {
        // Arrange
        RecordId     anyId       = Any.RecordId().Generate();
        Title        anyTitle    = Any.Title().Generate();
        LanguageTag  anyLanguage = Any.LanguageTag().WithoutScript().Generate();

        // Act
        BibliographicRecord record = new BibliographicRecord(anyId, anyTitle, null, [], anyLanguage, WritingScript.Latin, 1, 2000);

        // Assert
        Assert.Null(record.Subtitle);
        Assert.Null(record.Isbn);
        Assert.Null(record.Issn);
        Assert.Null(record.CallNumber);
        Assert.Empty(record.Contributors);
    }

    [Fact]
    public void Missing_mandatory_values_are_refused() {
        // Arrange
        RecordId     anyId       = Any.RecordId().Generate();
        Title        anyTitle    = Any.Title().Generate();
        LanguageTag  anyLanguage = new LanguageTag("fr");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(null!, anyTitle, null, [], anyLanguage, WritingScript.Latin, 1, 2000));
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(anyId, null!, null, [], anyLanguage, WritingScript.Latin, 1, 2000));
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(anyId, anyTitle, null, null!, anyLanguage, WritingScript.Latin, 1, 2000));
        Assert.Throws<ArgumentNullException>(() => new BibliographicRecord(anyId, anyTitle, null, [], null!, WritingScript.Latin, 1, 2000));
    }

    [Fact]
    public void An_undefined_script_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BibliographicRecord().WithScript((WritingScript)42).Generate());

        // Assert
        Assert.Equal("script", refusal.ParamName);
    }

    // ---- edition and year -----------------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(998)]
    [InlineData(999)]
    public void An_edition_from_1_to_999_is_accepted(int edition) {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithEdition(edition).Generate();

        // Assert
        Assert.Equal(edition, record.Edition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void An_edition_outside_1_to_999_is_refused(int edition) {
        // Arrange
        AnyBibliographicRecord anyRecord = Any.BibliographicRecord().WithEdition(edition);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyRecord.Generate());

        // Assert
        Assert.Equal("edition", refusal.ParamName);
    }

    [Theory]
    [InlineData(1450)]
    [InlineData(1451)]
    [InlineData(1969)]
    [InlineData(2000)]
    public void A_year_from_1450_on_is_accepted_without_an_ISBN(int year) {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithPublicationYear(year).Generate();

        // Assert
        Assert.Equal(year, record.PublicationYear);
    }

    [Theory]
    [InlineData(1449)]
    [InlineData(0)]
    [InlineData(-500)]
    [InlineData(int.MinValue)]
    public void A_year_before_1450_is_refused(int year) {
        // Arrange
        AnyBibliographicRecord anyRecord = Any.BibliographicRecord().WithPublicationYear(year);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyRecord.Generate());

        // Assert
        Assert.Equal("publicationYear", refusal.ParamName);
    }

    [Fact]
    public void The_year_after_the_current_one_is_accepted_and_the_one_after_that_is_not() {
        // Arrange
        int nextYear = DateTime.UtcNow.Year + 1;

        // Act
        BibliographicRecord forthcoming = Any.BibliographicRecord().WithPublicationYear(nextYear).Generate();
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BibliographicRecord().WithPublicationYear(nextYear + 1).Generate());

        // Assert
        Assert.Equal(nextYear, forthcoming.PublicationYear);
        Assert.Equal("publicationYear", refusal.ParamName);
    }

    // ---- ISBN and year --------------------------------------------------------------------------

    [Theory]
    [InlineData(1970)]
    [InlineData(1971)]
    [InlineData(2006)]
    [InlineData(2007)]
    [InlineData(2024)]
    public void A_978_ISBN_is_accepted_from_1970_on(int year) {
        // Arrange
        Isbn13 anyLegacyIsbn = Any.Isbn13().With978Prefix().Generate();

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithPublicationYear(year).WithIsbn(anyLegacyIsbn).Generate();

        // Assert
        Assert.Equal(anyLegacyIsbn, record.Isbn);
    }

    [Theory]
    [InlineData(1450)]
    [InlineData(1969)]
    public void An_edition_before_1970_cannot_bear_an_ISBN(int year) {
        // Arrange
        Isbn13                 anyIsbn   = Any.Isbn13().With978Prefix().Generate();
        AnyBibliographicRecord anyRecord = Any.BibliographicRecord().WithPublicationYear(year).WithIsbn(anyIsbn);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyRecord.Generate());

        // Assert
        Assert.Equal("isbn", refusal.ParamName);
    }

    [Theory]
    [InlineData(2007)]
    [InlineData(2008)]
    [InlineData(2024)]
    public void A_979_ISBN_is_accepted_from_2007_on(int year) {
        // Arrange
        Isbn13 anyNewIsbn = Any.Isbn13().With979Prefix().Generate();

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithPublicationYear(year).WithIsbn(anyNewIsbn).Generate();

        // Assert
        Assert.Equal(anyNewIsbn, record.Isbn);
    }

    [Theory]
    [InlineData(1970)]
    [InlineData(1999)]
    [InlineData(2006)]
    public void A_979_ISBN_is_refused_before_2007(int year) {
        // Arrange
        Isbn13                 anyNewIsbn = Any.Isbn13().With979Prefix().Generate();
        AnyBibliographicRecord anyRecord  = Any.BibliographicRecord().WithPublicationYear(year).WithIsbn(anyNewIsbn);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyRecord.Generate());

        // Assert
        Assert.Equal("isbn", refusal.ParamName);
        Assert.Contains("2007", refusal.Message);
    }

    // ---- script ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(WritingScript.Latin, "Pride and Prejudice")]
    [InlineData(WritingScript.Latin, "Études sur l’Ïle d’Œuf — Ǆ ŉ")]
    [InlineData(WritingScript.Greek, "Ιλιάδα")]
    [InlineData(WritingScript.Cyrillic, "Война и мир")]
    [InlineData(WritingScript.Hebrew, "שלום עליכם")]
    [InlineData(WritingScript.Arabic, "ألف ليلة وليلة")]
    [InlineData(WritingScript.Devanagari, "ग\u094Bद\u093Eन")]
    [InlineData(WritingScript.Cjk, "源氏物語")]
    [InlineData(WritingScript.Cjk, "ひらがなとカタカナ")]
    [InlineData(WritingScript.Cjk, "한국어 책")]
    [InlineData(WritingScript.Cjk, "漢字とひらがなと한글")]
    public void A_title_in_the_declared_script_is_accepted_as_title_and_as_subtitle(WritingScript script, string text) {
        // Arrange
        Title title = new Title(text);

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(script).WithTitle(title).WithSubtitle(title).Generate();

        // Assert
        Assert.Equal(script, record.Script);
    }

    [Theory]
    [InlineData(WritingScript.Latin)]
    [InlineData(WritingScript.Greek)]
    [InlineData(WritingScript.Cyrillic)]
    [InlineData(WritingScript.Hebrew)]
    [InlineData(WritingScript.Arabic)]
    [InlineData(WritingScript.Devanagari)]
    [InlineData(WritingScript.Cjk)]
    public void A_title_without_letters_fits_any_script(WritingScript script) {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(script).WithTitle(new Title("1984")).WithSubtitle(new Title("(2 : 7) !")).Generate();

        // Assert
        Assert.Equal("1984", record.Title.Value);
    }

    [Theory]
    [InlineData(WritingScript.Latin, "Война и мир")]
    [InlineData(WritingScript.Cyrillic, "War and Peace")]
    [InlineData(WritingScript.Greek, "Ιλιάδα Iliad")]
    [InlineData(WritingScript.Latin, "日本")]
    [InlineData(WritingScript.Cjk, "Tokyo 東京")]
    [InlineData(WritingScript.Hebrew, "שלום سلام")]
    [InlineData(WritingScript.Arabic, "श\u093E\u0902त\u093F")]
    public void A_title_with_a_letter_of_another_script_is_refused(WritingScript script, string text) {
        // Arrange
        AnyBibliographicRecord anyRecord = Any.BibliographicRecord().WithScript(script);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyRecord.WithTitle(new Title(text)).Generate());

        // Assert
        Assert.Equal("title", refusal.ParamName);
        Assert.Contains("but the record is declared in " + script, refusal.Message);
    }

    [Theory]
    [InlineData(WritingScript.Latin, "Война и мир")]
    [InlineData(WritingScript.Cyrillic, "War and Peace")]
    public void A_subtitle_with_a_letter_of_another_script_is_refused(WritingScript script, string text) {
        // Arrange
        AnyBibliographicRecord anyRecord = Any.BibliographicRecord().WithScript(script);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyRecord.WithSubtitle(new Title(text)).Generate());

        // Assert
        Assert.Equal("subtitle", refusal.ParamName);
    }

    [Theory]
    [InlineData("สว\u0E31สด\u0E35")]
    [InlineData("გამარჯობა")]
    [InlineData("Bangkok กร\u0E38งเทพ")]
    public void A_letter_of_an_unsupported_script_is_refused_whatever_the_declared_script(string text) {
        // Arrange
        WritingScript          anyScript = Any.Enum<WritingScript>().Generate();
        AnyBibliographicRecord anyRecord = Any.BibliographicRecord().WithScript(anyScript);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyRecord.WithTitle(new Title(text)).Generate());

        // Assert
        Assert.Contains("does not support", refusal.Message);
    }

    // ---- language tag and script ------------------------------------------------------------------

    [Theory]
    [InlineData("sr-Latn", WritingScript.Latin, "Beograd")]
    [InlineData("sr-Latn-RS", WritingScript.Latin, "Beograd")]
    [InlineData("sr-Cyrl", WritingScript.Cyrillic, "Београд")]
    [InlineData("ja-Jpan", WritingScript.Cjk, "東京")]
    [InlineData("zh-Hant", WritingScript.Cjk, "臺北")]
    [InlineData("ar-Arab-EG", WritingScript.Arabic, "القاهرة")]
    public void A_script_subtag_designating_the_declared_script_is_accepted(string tag, WritingScript script, string text) {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(script).WithTitle(new Title(text)).WithLanguage(new LanguageTag(tag)).Generate();

        // Assert
        Assert.Equal(tag, record.Language.Value);
    }

    [Theory]
    [InlineData("sr-Latn", WritingScript.Cyrillic)]
    [InlineData("sr-Cyrl", WritingScript.Latin)]
    [InlineData("zh-Hant", WritingScript.Latin)]
    [InlineData("el-Grek", WritingScript.Cjk)]
    public void A_script_subtag_designating_another_script_is_refused(string tag, WritingScript script) {
        // Arrange
        AnyBibliographicRecord anyRecord = Any.BibliographicRecord().WithScript(script).WithTitle(new Title("1984")).WithSubtitle(null).WithLanguage(new LanguageTag(tag));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyRecord.Generate());

        // Assert
        Assert.Equal("language", refusal.ParamName);
    }

    [Theory]
    [InlineData("th-Thai")]
    [InlineData("xx-Qaaa")]
    public void A_script_subtag_the_catalogue_does_not_know_matches_no_declared_script(string tag) {
        // Arrange
        WritingScript          anyScript = Any.Enum<WritingScript>().Generate();
        AnyBibliographicRecord anyRecord = Any.BibliographicRecord().WithScript(anyScript).WithTitle(new Title("1984")).WithSubtitle(null).WithLanguage(new LanguageTag(tag));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => anyRecord.Generate());
    }

    [Fact]
    public void A_transliteration_is_accepted_a_Russian_title_in_Latin_letters() {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(WritingScript.Latin).WithTitle(new Title("Voyna i mir")).WithLanguage(new LanguageTag("ru")).Generate();

        // Assert
        Assert.Equal("ru", record.Language.Value);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("fr-FR")]
    [InlineData("es-419")]
    public void A_tag_without_a_script_subtag_fits_any_script(string tag) {
        // Arrange
        WritingScript anyScript = Any.Enum<WritingScript>().Generate();

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithScript(anyScript).WithLanguage(new LanguageTag(tag)).Generate();

        // Assert
        Assert.Equal(tag, record.Language.Value);
    }

    // ---- contributors ---------------------------------------------------------------------------

    [Fact]
    public void Twenty_contributors_are_accepted_and_twenty_one_are_refused() {
        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithContributorCount(BibliographicRecord.MaxContributors).Generate();
        ArgumentException   refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithContributorCount(BibliographicRecord.MaxContributors + 1).Generate());

        // Assert
        Assert.Equal(20, record.Contributors.Count);
        Assert.Equal("contributors", refusal.ParamName);
    }

    [Fact]
    public void The_same_name_in_the_same_role_twice_is_refused() {
        // Arrange
        PersonName   anyName = Any.PersonName().Generate();
        ContributorRole role = ContributorRole.Editor;
        Contributor[] twice  = [new Contributor(anyName, role), new Contributor(anyName, role)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithContributors(twice).Generate());

        // Assert
        Assert.Contains("twice", refusal.Message);
    }

    [Fact]
    public void Names_equal_under_case_folding_count_as_the_same_name() {
        // Arrange
        Contributor[] twice = [new Contributor(new PersonName("Müller"), ContributorRole.Author), new Contributor(new PersonName("MÜLLER"), ContributorRole.Author)];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithContributors(twice).Generate());
    }

    [Fact]
    public void The_same_person_may_hold_two_roles() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Act
        BibliographicRecord record = Any.BibliographicRecord()
                                        .WithContributors([new Contributor(anyName, ContributorRole.Author), new Contributor(anyName, ContributorRole.Illustrator)])
                                        .Generate();

        // Assert
        Assert.Equal(2, record.Contributors.Count);
    }

    [Fact]
    public void A_null_contributor_is_refused() {
        // Arrange
        Contributor[] withNull = [Any.Contributor().Generate(), null!];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.BibliographicRecord().WithContributors(withNull).Generate());

        // Assert
        Assert.Equal("contributors", refusal.ParamName);
    }

    [Fact]
    public void Contributors_keep_the_order_of_the_title_page_and_are_copied() {
        // Arrange
        Contributor        first  = Records.Author("Ellison", "Ralph");
        Contributor        second = new Contributor(new PersonName("Wright", "Richard"), ContributorRole.Editor);
        List<Contributor>  source = [first, second];

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithContributors(source).Generate();
        source.Clear();

        // Assert
        Assert.Equal([first, second], record.Contributors);
    }

    [Fact]
    public void Contributor_names_ignore_roles() {
        // Arrange
        PersonName anna = new PersonName("Anna");
        PersonName boris = new PersonName("Boris");
        BibliographicRecord record = Any.BibliographicRecord()
                                        .WithContributors([new Contributor(anna, ContributorRole.Author), new Contributor(anna, ContributorRole.Translator), new Contributor(boris, ContributorRole.Editor)])
                                        .Generate();

        // Act
        HashSet<PersonName> names = record.ContributorNames();

        // Assert
        Assert.Equal(2, names.Count);
        Assert.Contains(anna, names);
        Assert.Contains(boris, names);
    }

    // ---- serials --------------------------------------------------------------------------------

    [Fact]
    public void A_record_with_an_ISSN_and_no_ISBN_is_a_serial() {
        // Act
        BibliographicRecord record = Records.Serial().Generate();

        // Assert
        Assert.True(record.IsSerial);
    }

    [Fact]
    public void A_record_with_an_ISBN_is_not_a_serial_even_with_an_ISSN() {
        // Arrange
        Issn anyIssn = Any.Issn().Generate();

        // Act
        BibliographicRecord record = Records.WithIsbn().WithIssn(anyIssn).Generate();

        // Assert
        Assert.False(record.IsSerial);
    }

    [Fact]
    public void A_record_without_an_ISSN_is_not_a_serial() {
        Assert.False(Records.Unnumbered().Generate().IsSerial);
    }

    // ---- same work -----------------------------------------------------------------------------

    [Fact]
    public void Two_records_of_the_same_edition_of_the_same_work_describe_the_same_work() {
        // Arrange
        BibliographicRecord original = Records.Unnumbered().Generate();
        BibliographicRecord copy     = Records.SameWorkAs(original).Generate();

        // Act & Assert
        Assert.True(original.DescribesSameWorkAs(copy));
        Assert.True(copy.DescribesSameWorkAs(original));
    }

    [Fact]
    public void A_record_describes_the_same_work_as_itself() {
        // Arrange
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();

        // Assert
        Assert.True(anyRecord.DescribesSameWorkAs(anyRecord));
    }

    [Fact]
    public void Only_the_title_the_subtitle_the_language_the_edition_the_year_and_the_contributors_decide() {
        // Arrange
        BibliographicRecord original = Records.Unnumbered().Generate();

        // Act
        BibliographicRecord elsewhere = Records.SameWorkAs(original).WithCallNumber(new CallNumber("100 A1")).WithIssn(Any.Issn().Generate()).Generate();

        // Assert
        Assert.True(original.DescribesSameWorkAs(elsewhere));
    }

    [Fact]
    public void A_title_equal_under_NFC_and_case_folding_is_the_same_title() {
        // Arrange
        BibliographicRecord original = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title("Café Noir")).Generate();
        BibliographicRecord shouted  = Records.SameWorkAs(original).WithTitle(new Title("CAFE\u0301 NOIR")).Generate();

        // Assert
        Assert.True(original.DescribesSameWorkAs(shouted));
    }

    [Fact]
    public void A_different_title_makes_a_different_work() {
        // Arrange
        BibliographicRecord original = Records.Unnumbered().WithScript(WritingScript.Latin).WithTitle(new Title("Dune")).Generate();
        BibliographicRecord other    = Records.SameWorkAs(original).WithTitle(new Title("Dune Messiah")).Generate();

        // Assert
        Assert.False(original.DescribesSameWorkAs(other));
    }

    [Fact]
    public void A_missing_or_different_subtitle_makes_a_different_work() {
        // Arrange
        BibliographicRecord bare     = Records.Unnumbered().WithScript(WritingScript.Latin).WithoutSubtitle().Generate();
        BibliographicRecord subtitled = Records.SameWorkAs(bare).WithSubtitle(new Title("A Novel")).Generate();
        BibliographicRecord otherSub  = Records.SameWorkAs(subtitled).WithSubtitle(new Title("A Poem")).Generate();

        // Assert
        Assert.False(bare.DescribesSameWorkAs(subtitled));
        Assert.False(subtitled.DescribesSameWorkAs(otherSub));
    }

    [Fact]
    public void Two_records_without_subtitle_have_equal_subtitles() {
        // Arrange
        BibliographicRecord bare  = Records.Unnumbered().WithoutSubtitle().Generate();
        BibliographicRecord other = Records.SameWorkAs(bare).Generate();

        // Assert
        Assert.True(bare.DescribesSameWorkAs(other));
    }

    [Theory]
    [InlineData("fr", "fr-FR")]
    [InlineData("fr", "fra")]
    [InlineData("sr-Latn", "sr")]
    public void A_different_language_tag_makes_a_different_work(string firstTag, string secondTag) {
        // Arrange
        BibliographicRecord first  = Records.Unnumbered().WithScript(WritingScript.Latin).WithLanguage(new LanguageTag(firstTag)).Generate();
        BibliographicRecord second = Records.SameWorkAs(first).WithLanguage(new LanguageTag(secondTag)).Generate();

        // Assert
        Assert.False(first.DescribesSameWorkAs(second));
    }

    [Fact]
    public void A_different_edition_makes_a_different_work() {
        // Arrange
        BibliographicRecord first  = Records.Unnumbered().WithEdition(2).Generate();
        BibliographicRecord second = Records.SameWorkAs(first).WithEdition(3).Generate();

        // Assert
        Assert.False(first.DescribesSameWorkAs(second));
    }

    [Fact]
    public void A_different_year_makes_a_different_work() {
        // Arrange
        BibliographicRecord first  = Records.Unnumbered().WithPublicationYear(1999).Generate();
        BibliographicRecord second = Records.SameWorkAs(first).WithPublicationYear(2000).Generate();

        // Assert
        Assert.False(first.DescribesSameWorkAs(second));
    }

    [Fact]
    public void The_contributors_are_compared_as_a_set_of_names_whatever_the_order_the_roles_or_the_repetitions() {
        // Arrange
        Contributor anna  = new Contributor(new PersonName("Anna"), ContributorRole.Author);
        Contributor boris = new Contributor(new PersonName("Boris"), ContributorRole.Editor);
        BibliographicRecord original  = Records.Unnumbered().WithContributors([anna, boris]).Generate();
        BibliographicRecord reordered = Records.SameWorkAs(original).WithContributors([boris, anna]).Generate();
        BibliographicRecord reroled   = Records.SameWorkAs(original).WithContributors([new Contributor(new PersonName("Anna"), ContributorRole.Translator), boris]).Generate();
        BibliographicRecord doubled   = Records.SameWorkAs(original).WithContributors([anna, new Contributor(new PersonName("Anna"), ContributorRole.Editor), boris]).Generate();

        // Assert
        Assert.True(original.DescribesSameWorkAs(reordered));
        Assert.True(original.DescribesSameWorkAs(reroled));
        Assert.True(original.DescribesSameWorkAs(doubled));
    }

    [Fact]
    public void A_different_set_of_names_makes_a_different_work() {
        // Arrange
        Contributor anna  = Records.Author("Anna", "A");
        Contributor boris = Records.Author("Boris", "B");
        BibliographicRecord one = Records.Unnumbered().WithContributors([anna]).Generate();
        BibliographicRecord two = Records.SameWorkAs(one).WithContributors([anna, boris]).Generate();
        BibliographicRecord none = Records.SameWorkAs(one).WithContributors([]).Generate();

        // Assert
        Assert.False(one.DescribesSameWorkAs(two));
        Assert.False(one.DescribesSameWorkAs(none));
    }

    [Fact]
    public void Different_ISBNs_make_different_manifestations_even_when_everything_else_is_equal() {
        // Arrange
        Isbn13 firstIsbn  = new Isbn13("9780306406157");
        Isbn13 secondIsbn = new Isbn13("9781234567897");
        BibliographicRecord first  = Records.WithIsbn().WithIsbn(firstIsbn).Generate();
        BibliographicRecord second = Records.SameWorkAs(first).WithIsbn(secondIsbn).Generate();

        // Assert
        Assert.False(first.DescribesSameWorkAs(second));
    }

    [Fact]
    public void An_ISBN_on_one_side_only_does_not_separate_two_records() {
        // Arrange
        BibliographicRecord numbered = Records.WithIsbn().Generate();
        BibliographicRecord bare     = Records.SameWorkAs(numbered).WithoutIsbn().Generate();

        // Assert
        Assert.True(numbered.DescribesSameWorkAs(bare));
        Assert.True(bare.DescribesSameWorkAs(numbered));
    }

    [Fact]
    public void The_same_ISBN_on_both_sides_does_not_separate_two_records() {
        // Arrange
        BibliographicRecord first  = Records.WithIsbn().Generate();
        BibliographicRecord second = Records.SameWorkAs(first).Generate();

        // Assert
        Assert.True(first.DescribesSameWorkAs(second));
    }

    [Fact]
    public void Comparing_with_null_is_refused() {
        // Arrange
        BibliographicRecord anyRecord = Any.BibliographicRecord().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyRecord.DescribesSameWorkAs(null!));
    }

}
