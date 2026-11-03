// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BibliographicRecord --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BibliographicRecord" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBibliographicRecord : IAny<BibliographicRecord> {

    private IAny<RecordId>                 _id;
    private IAny<Title>                    _title;
    private IAny<Title?>                   _subtitle;
    private IAny<IEnumerable<Contributor>> _contributors;
    private IAny<LanguageTag>              _language;
    private IAny<WritingScript>            _script;
    private IAny<int>                      _edition;
    private IAny<int>                      _publicationYear;
    private IAny<Isbn13?>                  _isbn;
    private IAny<Issn?>                    _issn;
    private IAny<CallNumber?>              _callNumber;

    private AnyRecordId              _idRecipe;
    private AnyTitle                 _titleRecipe;
    private AnyTitle                 _subtitleRecipe;
    private IAny<IEnumerable<Contributor>> _contributorsRecipe;
    private AnyLanguageTag           _languageRecipe;
    private AnyEnum<WritingScript>   _scriptRecipe;
    private AnyInt32                 _editionRecipe;
    private AnyInt32                 _publicationYearRecipe;
    private AnyIsbn13                _isbnRecipe;
    private AnyIssn                  _issnRecipe;
    private AnyCallNumber            _callNumberRecipe;

    // A pin is a chosen value, never completed by the draw; anything else follows the script the record is in.
    private bool _titlePinned;
    private bool _subtitlePinned;
    private bool _languagePinned;
    private bool _scriptPinned;
    private bool _yearPinned;
    private bool _isbnPinned;
    private bool _subtitleDeclared;   // refined through a lambda: a value, never absent
    private bool _isbnDeclared;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBibliographicRecord()
        : this(id:              AnyValidId(),
               title:           AnyValidTitle(),
               subtitle:        AnyValidSubtitle(),
               contributors:    AnyValidContributors(),
               language:        AnyValidLanguage(),
               script:          AnyValidScript(),
               edition:         AnyValidEdition(),
               publicationYear: AnyValidPublicationYear(),
               isbn:            AnyValidIsbn(),
               issn:            AnyValidIssn(),
               callNumber:      AnyValidCallNumber()) { }

    private static AnyRecordId AnyValidId() {
        return new AnyRecordId();
    }

    private static AnyTitle AnyValidTitle() {
        return new AnyTitle();
    }

    private static AnyTitle AnyValidSubtitle() {
        return new AnyTitle();
    }

    private static IAny<IEnumerable<Contributor>> AnyValidContributors() {
        // At most 20 contributors, none twice (a contributor is equal by name and role).
        return Any.ListOf(new AnyContributor()).WithCountBetween(0, 4).Distinct().As(contributors => (IEnumerable<Contributor>)contributors);
    }

    private static AnyLanguageTag AnyValidLanguage() {
        return new AnyLanguageTag();
    }

    private static AnyEnum<WritingScript> AnyValidScript() {
        return Any.Enum<WritingScript>();
    }

    private static AnyInt32 AnyValidEdition() {
        return Any.Int32().Positive().LessThanOrEqualTo(999);
    }

    private static AnyInt32 AnyValidPublicationYear() {
        return Any.Int32().Between(BibliographicRecord.FirstPrintingYear, DateTime.UtcNow.Year + 1);
    }

    private static AnyIsbn13 AnyValidIsbn() {
        return new AnyIsbn13();
    }

    private static AnyIssn AnyValidIssn() {
        return new AnyIssn();
    }

    private static AnyCallNumber AnyValidCallNumber() {
        return new AnyCallNumber();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBibliographicRecord(AnyRecordId              id,
                                   AnyTitle                 title,
                                   AnyTitle                 subtitle,
                                   IAny<IEnumerable<Contributor>> contributors,
                                   AnyLanguageTag           language,
                                   AnyEnum<WritingScript>   script,
                                   AnyInt32                 edition,
                                   AnyInt32                 publicationYear,
                                   AnyIsbn13                isbn,
                                   AnyIssn                  issn,
                                   AnyCallNumber            callNumber) {
        _id                    = id;
        _idRecipe              = id;
        _title                 = title;
        _titleRecipe           = title;
        _subtitle              = subtitle.OrNull();
        _subtitleRecipe        = subtitle;
        _contributors          = contributors;
        _contributorsRecipe    = contributors;
        _language              = language;
        _languageRecipe        = language;
        _script                = script;
        _scriptRecipe          = script;
        _edition               = edition;
        _editionRecipe         = edition;
        _publicationYear       = publicationYear;
        _publicationYearRecipe = publicationYear;
        _isbn                  = isbn.OrNull();
        _isbnRecipe            = isbn;
        _issn                  = issn.OrNull();
        _issnRecipe            = issn;
        _callNumber            = callNumber.OrNull();
        _callNumberRecipe      = callNumber;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBibliographicRecord(AnyBibliographicRecord from) {
        _id                    = from._id;
        _idRecipe              = from._idRecipe;
        _title                 = from._title;
        _titleRecipe           = from._titleRecipe;
        _subtitle              = from._subtitle;
        _subtitleRecipe        = from._subtitleRecipe;
        _contributors          = from._contributors;
        _contributorsRecipe    = from._contributorsRecipe;
        _language              = from._language;
        _languageRecipe        = from._languageRecipe;
        _script                = from._script;
        _scriptRecipe          = from._scriptRecipe;
        _edition               = from._edition;
        _editionRecipe         = from._editionRecipe;
        _publicationYear       = from._publicationYear;
        _publicationYearRecipe = from._publicationYearRecipe;
        _isbn                  = from._isbn;
        _isbnRecipe            = from._isbnRecipe;
        _issn                  = from._issn;
        _issnRecipe            = from._issnRecipe;
        _callNumber            = from._callNumber;
        _callNumberRecipe      = from._callNumberRecipe;
        _titlePinned           = from._titlePinned;
        _subtitlePinned        = from._subtitlePinned;
        _languagePinned        = from._languagePinned;
        _scriptPinned          = from._scriptPinned;
        _yearPinned            = from._yearPinned;
        _isbnPinned            = from._isbnPinned;
        _subtitleDeclared      = from._subtitleDeclared;
        _isbnDeclared          = from._isbnDeclared;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithId(RecordId value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRecordId, AnyRecordId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithId(Func<AnyRecordId, AnyRecordId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>title</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithTitle(Title value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._title = Any.Fixed(value);
        copy._titlePinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>title</c> is drawn from — <c>AnyValidTitle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTitle, AnyTitle&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithTitle(Func<AnyTitle, AnyTitle> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._titleRecipe = configure(_titleRecipe);
        copy._title       = copy._titleRecipe;
        copy._titlePinned = false;

        return copy;
    }

    /// <summary>Pins <c>subtitle</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithSubtitle(Title? value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._subtitle = Any.Fixed(value);
        copy._subtitlePinned = true;
        copy._subtitleDeclared = false;

        return copy;
    }

    /// <summary>Refines the recipe <c>subtitle</c> is drawn from — <c>AnyValidSubtitle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTitle, AnyTitle&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithSubtitle(Func<AnyTitle, AnyTitle> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._subtitleRecipe = configure(_subtitleRecipe);
        copy._subtitle       = copy._subtitleRecipe;
        copy._subtitlePinned = false;
        copy._subtitleDeclared = true;

        return copy;
    }

    /// <summary>Pins <c>subtitle</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithoutSubtitle() {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._subtitle = Any.Fixed<Title?>(null);
        copy._subtitlePinned = true;
        copy._subtitleDeclared = false;

        return copy;
    }

    /// <summary>Pins <c>contributors</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithContributors(IEnumerable<Contributor> value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._contributors = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>language</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithLanguage(LanguageTag value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._language = Any.Fixed(value);
        copy._languagePinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>language</c> is drawn from — <c>AnyValidLanguage()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLanguageTag, AnyLanguageTag&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithLanguage(Func<AnyLanguageTag, AnyLanguageTag> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._languageRecipe = configure(_languageRecipe);
        copy._language       = copy._languageRecipe;
        copy._languagePinned = false;

        return copy;
    }

    /// <summary>Pins <c>script</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithScript(WritingScript value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._script = Any.Fixed(value);
        copy._scriptPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>script</c> is drawn from — <c>AnyValidScript()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;WritingScript&gt;, AnyEnum&lt;WritingScript&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithScript(Func<AnyEnum<WritingScript>, AnyEnum<WritingScript>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._scriptRecipe = configure(_scriptRecipe);
        copy._script       = copy._scriptRecipe;
        copy._scriptPinned = false;

        return copy;
    }

    /// <summary>Pins <c>edition</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithEdition(int value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._edition = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>edition</c> is drawn from — <c>AnyValidEdition()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithEdition(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._editionRecipe = configure(_editionRecipe);
        copy._edition       = copy._editionRecipe;

        return copy;
    }

    /// <summary>Pins <c>publicationYear</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithPublicationYear(int value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._publicationYear = Any.Fixed(value);
        copy._yearPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>publicationYear</c> is drawn from — <c>AnyValidPublicationYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithPublicationYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._publicationYearRecipe = configure(_publicationYearRecipe);
        copy._publicationYear       = copy._publicationYearRecipe;
        copy._yearPinned = false;

        return copy;
    }

    /// <summary>Pins <c>isbn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithIsbn(Isbn13? value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._isbn = Any.Fixed(value);
        copy._isbnPinned = true;
        copy._isbnDeclared = false;

        return copy;
    }

    /// <summary>Refines the recipe <c>isbn</c> is drawn from — <c>AnyValidIsbn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIsbn13, AnyIsbn13&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithIsbn(Func<AnyIsbn13, AnyIsbn13> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._isbnRecipe = configure(_isbnRecipe);
        copy._isbn       = copy._isbnRecipe;
        copy._isbnPinned = false;
        copy._isbnDeclared = true;

        return copy;
    }

    /// <summary>Pins <c>isbn</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithoutIsbn() {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._isbn = Any.Fixed<Isbn13?>(null);
        copy._isbnPinned = true;
        copy._isbnDeclared = false;

        return copy;
    }

    /// <summary>Pins <c>issn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithIssn(Issn? value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._issn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>issn</c> is drawn from — <c>AnyValidIssn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyIssn, AnyIssn&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithIssn(Func<AnyIssn, AnyIssn> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._issnRecipe = configure(_issnRecipe);
        copy._issn       = copy._issnRecipe;

        return copy;
    }

    /// <summary>Pins <c>issn</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithoutIssn() {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._issn = Any.Fixed<Issn?>(null);

        return copy;
    }

    /// <summary>Pins <c>callNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithCallNumber(CallNumber? value) {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._callNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>callNumber</c> is drawn from — <c>AnyValidCallNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCallNumber, AnyCallNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBibliographicRecord WithCallNumber(Func<AnyCallNumber, AnyCallNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._callNumberRecipe = configure(_callNumberRecipe);
        copy._callNumber       = copy._callNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>callNumber</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyBibliographicRecord WithoutCallNumber() {
        AnyBibliographicRecord copy = new AnyBibliographicRecord(this);

        copy._callNumber = Any.Fixed<CallNumber?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BibliographicRecord" />.</summary>
    /// <remarks>
    ///     Hand-completed: the draws the constructor ties together are made in an order that keeps them coherent —
    ///     the language first when it is pinned with a script subtag, then the script, then the texts in that
    ///     script, and the year and the ISBN each inside what the other allows.
    /// </remarks>
    public BibliographicRecord Generate() {
        LanguageTag?  pinnedLanguage = _languagePinned ? _language.Generate() : null;
        WritingScript script         = _scriptPinned ? _script.Generate()
                                     : pinnedLanguage?.DeclaredScript
                                       ?? (_titlePinned ? ScriptOf(_title.Generate()) : null)
                                       ?? (_subtitlePinned ? ScriptOf(_subtitle.Generate()) : null)
                                       ?? _script.Generate();

        Title         title    = _titlePinned ? _title.Generate() : _titleRecipe.InScript(script).Generate();
        Title?        subtitle = _subtitlePinned ? _subtitle.Generate()
                                 : _subtitleDeclared ? _subtitleRecipe.InScript(script).Generate()
                                 : _subtitleRecipe.InScript(script).OrNull().Generate();
        LanguageTag   language = _languagePinned ? pinnedLanguage! : _languageRecipe.ForScript(script).Generate();

        int     year;
        Isbn13? isbn;
        if (_isbnPinned || _isbnDeclared) {
            isbn = _isbn.Generate();
            year = _yearPinned ? _publicationYear.Generate() : _publicationYearRecipe.GreaterThanOrEqualTo(EarliestYearAllowing(isbn)).Generate();
        } else {
            year = _publicationYear.Generate();
            isbn = year < BibliographicRecord.FirstIsbnYear ? null
                 : year < BibliographicRecord.FirstPrefix979Year ? _isbnRecipe.Prefix978().OrNull().Generate()
                 : _isbn.Generate();
        }

        return new BibliographicRecord(_id.Generate(),
                                       title,
                                       subtitle,
                                       _contributors.Generate(),
                                       language,
                                       script,
                                       _edition.Generate(),
                                       year,
                                       isbn,
                                       _issn.Generate(),
                                       _callNumber.Generate());
    }

    private static WritingScript? ScriptOf(Title? text) {
        return text is not null && text.LetterScripts().Count == 1 ? text.LetterScripts().Single() : null;
    }

    private static int EarliestYearAllowing(Isbn13? isbn) {
        return isbn is null ? BibliographicRecord.FirstPrintingYear
             : isbn.HasIsbn10Form ? BibliographicRecord.FirstIsbnYear
             : BibliographicRecord.FirstPrefix979Year;
    }

}
