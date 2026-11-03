// Scaffolded by `any`, then rewritten by hand. The constructor ties the parameters together (the script of
// the title, the subtitle and the language tag; the year and the kind of ISBN), and no per-parameter recipe can
// honour that, so Generate() draws the script and the year first and builds the rest from them.

using System;
using System.Collections.Generic;
using System.Linq;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyBibliographicRecord : IAny<BibliographicRecord> {

    private static readonly int LatestYear = DateTime.UtcNow.Year + 1;

    private IAny<RecordId>?                 _id;
    private IAny<Title>?                    _title;
    private IAny<Title?>?                   _subtitle;
    private IAny<IEnumerable<Contributor>>? _contributors;
    private IAny<LanguageTag>?              _language;
    private IAny<WritingScript>?            _script;
    private IAny<int>?                      _edition;
    private IAny<int>?                      _publicationYear;
    private IAny<Isbn13?>?                  _isbn;
    private IAny<Issn?>?                    _issn;
    private IAny<CallNumber?>?              _callNumber;

    public AnyBibliographicRecord() { }

    private AnyBibliographicRecord(AnyBibliographicRecord from) {
        _id              = from._id;
        _title           = from._title;
        _subtitle        = from._subtitle;
        _contributors    = from._contributors;
        _language        = from._language;
        _script          = from._script;
        _edition         = from._edition;
        _publicationYear = from._publicationYear;
        _isbn            = from._isbn;
        _issn            = from._issn;
        _callNumber      = from._callNumber;
    }

    public AnyBibliographicRecord WithId(RecordId id) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._id = Any.Fixed(id);

        return copy;
    }

    public AnyBibliographicRecord WithTitle(Title title) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._title = Any.Fixed(title);

        // A pinned title written in one script settles the record's script unless the test pinned it too.
        if (_script is null && title.LetterScripts().Count == 1) { copy._script = Any.Fixed(title.LetterScripts().Single()); }

        return copy;
    }

    public AnyBibliographicRecord WithSubtitle(Title? subtitle) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._subtitle = Any.Fixed(subtitle);

        return copy;
    }

    public AnyBibliographicRecord WithoutSubtitle() {
        return WithSubtitle(null);
    }

    public AnyBibliographicRecord WithContributors(params Contributor[] contributors) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._contributors = Any.Fixed<IEnumerable<Contributor>>(contributors);

        return copy;
    }

    public AnyBibliographicRecord WithLanguage(LanguageTag language) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._language = Any.Fixed(language);

        return copy;
    }

    public AnyBibliographicRecord WithScript(WritingScript script) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._script = Any.Fixed(script);

        return copy;
    }

    public AnyBibliographicRecord WithEdition(int edition) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._edition = Any.Fixed(edition);

        return copy;
    }

    public AnyBibliographicRecord WithPublicationYear(int year) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._publicationYear = Any.Fixed(year);

        return copy;
    }

    public AnyBibliographicRecord WithIsbn(Isbn13? isbn) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._isbn = Any.Fixed(isbn);

        return copy;
    }

    public AnyBibliographicRecord WithoutIsbn() {
        return WithIsbn(null);
    }

    public AnyBibliographicRecord WithIssn(Issn? issn) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._issn = Any.Fixed(issn);

        return copy;
    }

    public AnyBibliographicRecord WithoutIssn() {
        return WithIssn(null);
    }

    public AnyBibliographicRecord WithCallNumber(CallNumber? callNumber) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._callNumber = Any.Fixed(callNumber);

        return copy;
    }

    /// <summary>A serial: an ISSN, no ISBN.</summary>
    public AnyBibliographicRecord AsSerial() {
        return WithoutIsbn().WithIssn(new AnyIssn().Generate());
    }

    public BibliographicRecord Generate() {
        WritingScript script = (_script ?? Any.Enum<WritingScript>()).Generate();

        Title       title    = (_title ?? new AnyTitle().InScript(script)).Generate();
        Title?      subtitle = (_subtitle ?? new AnyTitle().InScript(script).OrNull()).Generate();
        LanguageTag language = (_language ?? LanguageFor(script)).Generate();

        // The year follows a pinned ISBN, and the ISBN follows the drawn year, so the pair is valid either way.
        Isbn13? pinnedIsbn = _isbn?.Generate();
        int     year       = (_publicationYear ?? YearFor(pinnedIsbn)).Generate();
        Isbn13? isbn       = _isbn is not null ? pinnedIsbn : IsbnFor(year).Generate();

        return new BibliographicRecord(
            (_id ?? new AnyRecordId()).Generate(),
            title,
            subtitle,
            (_contributors ?? Any.ListOf(new AnyContributor()).WithCountBetween(0, 6).Distinct().As(list => (IEnumerable<Contributor>)list)).Generate(),
            language,
            script,
            (_edition ?? Any.Int32().Between(1, 999)).Generate(),
            year,
            isbn,
            (_issn ?? new AnyIssn().OrNull()).Generate(),
            (_callNumber ?? new AnyCallNumber().OrNull()).Generate());
    }

    private static IAny<LanguageTag> LanguageFor(WritingScript script) {
        string[] tags = script switch {
            WritingScript.Latin      => ["fr", "en", "de", "es-419", "pt-BR", "sr-Latn-RS", "tr", "pl", "ru", "ar", "en-Latn-GB"],
            WritingScript.Greek      => ["el", "el-Grek-GR", "grc", "fr"],
            WritingScript.Cyrillic   => ["ru", "uk", "bg", "sr-Cyrl-RS", "ru-Cyrl"],
            WritingScript.Hebrew     => ["he", "yi", "he-Hebr-IL"],
            WritingScript.Arabic     => ["ar", "fa", "ur", "ar-Arab-EG"],
            WritingScript.Devanagari => ["hi", "ne", "mr", "hi-Deva-IN"],
            WritingScript.Cjk        => ["zh-Hans", "zh-Hant-TW", "ja", "ko-Kore", "zh", "ja-Jpan-JP"],
            _                        => throw new ArgumentOutOfRangeException(nameof(script)),
        };

        return Any.OneOf(tags).As(tag => new LanguageTag(tag));
    }

    private static IAny<int> YearFor(Isbn13? pinnedIsbn) {
        if (pinnedIsbn is null) { return Any.Int32().Between(BibliographicRecord.FirstPrintingYear, LatestYear); }

        int floor = pinnedIsbn.HasIsbn10Form ? BibliographicRecord.FirstIsbnYear : BibliographicRecord.FirstPrefix979Year;

        return Any.Int32().Between(floor, LatestYear);
    }

    private static IAny<Isbn13?> IsbnFor(int year) {
        if (year < BibliographicRecord.FirstIsbnYear) { return Any.Fixed<Isbn13?>(null); }

        IAny<string> body = year < BibliographicRecord.FirstPrefix979Year ? Any.StringMatching("978[0-9]{9}") : Any.StringMatching("97[89][0-9]{9}");

        return body.As(digits => new Isbn13(digits + CheckCharacters.Isbn13(digits))).OrNull();
    }

}
