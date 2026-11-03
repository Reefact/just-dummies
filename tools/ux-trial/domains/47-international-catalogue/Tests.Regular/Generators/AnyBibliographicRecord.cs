using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BibliographicRecord" /> values. Three groups of parameters are tied by
///     the constructor and are therefore drawn in order, not independently:
///     the script, then the title, subtitle and language tag that script admits;
///     the publication year, then the ISBN that year admits (none before 1970, 978 only before 2007).
///     A pin on a tied parameter is kept as given, so a contradictory pin is refused by the record itself.
/// </summary>
public sealed partial class AnyBibliographicRecord : IAny<BibliographicRecord> {

    private IAny<RecordId>                                  _id;
    private IAny<WritingScript>                             _script;
    private Func<WritingScript, IAny<Title>>                _title;
    private Func<WritingScript, IAny<Title?>>               _subtitle;
    private IAny<IEnumerable<Contributor>>                  _contributors;
    private Func<WritingScript, IAny<LanguageTag>>          _language;
    private IAny<int>                                       _edition;
    private IAny<int>                                       _publicationYear;
    private Func<int, IAny<Isbn13?>>                        _isbn;
    private IAny<Issn?>                                     _issn;
    private IAny<CallNumber?>                               _callNumber;

    public AnyBibliographicRecord() {
        _id              = new AnyRecordId();
        _script          = Any.Enum<WritingScript>();
        _title           = script => new AnyTitle().InScript(script);
        _subtitle        = script => new AnyTitle().InScript(script).OrNull();
        _contributors    = AnyValidContributors();
        _language        = AnyValidLanguage;
        _edition         = Any.Int32().Positive().LessThanOrEqualTo(999);
        _publicationYear = Any.Int32().Between(BibliographicRecord.FirstPrintingYear, DateTime.UtcNow.Year + 1);
        _isbn            = AnyValidIsbn;
        _issn            = new AnyIssn().OrNull();
        _callNumber      = new AnyCallNumber().OrNull();
    }

    private AnyBibliographicRecord(AnyBibliographicRecord from) {
        _id              = from._id;
        _script          = from._script;
        _title           = from._title;
        _subtitle        = from._subtitle;
        _contributors    = from._contributors;
        _language        = from._language;
        _edition         = from._edition;
        _publicationYear = from._publicationYear;
        _isbn            = from._isbn;
        _issn            = from._issn;
        _callNumber      = from._callNumber;
    }

    private static IAny<IEnumerable<Contributor>> AnyValidContributors() {
        return AnyContributorsOfCount(Any.Int32().Between(0, BibliographicRecord.MaxContributors));
    }

    private static IAny<IEnumerable<Contributor>> AnyContributorsOfCount(IAny<int> count) {
        // Two independent draws almost never collide on (name, role), but "almost" is not the invariant: Distinct() states it.
        return count.Then(n => Any.ListOf(new AnyContributor()).WithCount(n).Distinct().As(list => (IEnumerable<Contributor>)list));
    }

    private static IAny<LanguageTag> AnyValidLanguage(WritingScript script) {
        return Any.Combine(Any.StringMatching("[a-z]{2,3}"),
                           Any.OneOf(Generated.ScriptSubtags(script)).OrNull(),
                           Any.StringMatching("[A-Z]{2}|[0-9]{3}").OrNull(),
                           (language, scriptSubtag, region) => new LanguageTag(string.Join('-', new[] { language, scriptSubtag, region }.Where(part => part is not null))));
    }

    private static IAny<Isbn13?> AnyValidIsbn(int year) {
        if (year < BibliographicRecord.FirstIsbnYear) { return Any.Fixed<Isbn13?>(null); }
        if (year < BibliographicRecord.FirstPrefix979Year) { return new AnyIsbn13().With978Prefix().OrNull(); }

        return new AnyIsbn13().OrNull();
    }

    public AnyBibliographicRecord WithId(RecordId value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins the script. The title, subtitle and language tag drawn for it follow, unless pinned too.</summary>
    public AnyBibliographicRecord WithScript(WritingScript value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._script = Any.Fixed(value);

        return copy;
    }

    public AnyBibliographicRecord WithTitle(Title value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._title = _ => Any.Fixed(value);

        return copy;
    }

    public AnyBibliographicRecord WithTitle(Func<AnyTitle, AnyTitle> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._title = script => configure(new AnyTitle().InScript(script));

        return copy;
    }

    public AnyBibliographicRecord WithSubtitle(Title? value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._subtitle = _ => Any.Fixed(value);

        return copy;
    }

    public AnyBibliographicRecord WithoutSubtitle() {
        return WithSubtitle(null);
    }

    /// <summary>A subtitle that is always there, in the script of the record.</summary>
    public AnyBibliographicRecord WithAnySubtitle() {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._subtitle = script => new AnyTitle().InScript(script).As(title => (Title?)title);

        return copy;
    }

    public AnyBibliographicRecord WithContributors(IEnumerable<Contributor> value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._contributors = Any.Fixed(value);

        return copy;
    }

    /// <summary>Exactly <paramref name="count" /> contributors, all different.</summary>
    public AnyBibliographicRecord WithContributorCount(int count) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._contributors = AnyContributorsOfCount(Any.Fixed(count));

        return copy;
    }

    public AnyBibliographicRecord WithLanguage(LanguageTag value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._language = _ => Any.Fixed(value);

        return copy;
    }

    public AnyBibliographicRecord WithEdition(int value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._edition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins the year. The ISBN drawn for it follows, unless pinned too.</summary>
    public AnyBibliographicRecord WithPublicationYear(int value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._publicationYear = Any.Fixed(value);

        return copy;
    }

    public AnyBibliographicRecord WithIsbn(Isbn13? value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._isbn = _ => Any.Fixed(value);

        return copy;
    }

    public AnyBibliographicRecord WithoutIsbn() {
        return WithIsbn(null);
    }

    /// <summary>An ISBN is always there: a year before 1970 is then out of reach, and a pinned one is refused by the record.</summary>
    public AnyBibliographicRecord WithAnyIsbn() {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._publicationYear = Any.Int32().Between(BibliographicRecord.FirstPrefix979Year, DateTime.UtcNow.Year + 1);
        copy._isbn            = _ => new AnyIsbn13().As(isbn => (Isbn13?)isbn);

        return copy;
    }

    public AnyBibliographicRecord WithIssn(Issn? value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._issn = Any.Fixed(value);

        return copy;
    }

    public AnyBibliographicRecord WithoutIssn() {
        return WithIssn(null);
    }

    public AnyBibliographicRecord WithCallNumber(CallNumber? value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._callNumber = Any.Fixed(value);

        return copy;
    }

    public BibliographicRecord Generate() {
        WritingScript script = _script.Generate();
        int           year   = _publicationYear.Generate();

        return new BibliographicRecord(_id.Generate(),
                                       _title(script).Generate(),
                                       _subtitle(script).Generate(),
                                       _contributors.Generate(),
                                       _language(script).Generate(),
                                       script,
                                       _edition.Generate(),
                                       year,
                                       _isbn(year).Generate(),
                                       _issn.Generate(),
                                       _callNumber.Generate());
    }

}
