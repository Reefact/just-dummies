namespace InternationalCatalogue.Domain;

/// <summary>
/// The description of one edition of a work, as contributed to the union catalogue. The declared script must
/// be the script of every letter of the title and subtitle, and of the language tag's script subtag if any.
/// </summary>
public sealed class BibliographicRecord
{
    public const int MaxContributors = 20;
    public const int FirstPrintingYear = 1450;
    public const int FirstIsbnYear = 1970;
    public const int FirstPrefix979Year = 2007;

    private readonly List<Contributor> _contributors;

    public BibliographicRecord(
        RecordId id,
        Title title,
        Title? subtitle,
        IEnumerable<Contributor> contributors,
        LanguageTag language,
        WritingScript script,
        int edition,
        int publicationYear,
        Isbn13? isbn = null,
        Issn? issn = null,
        CallNumber? callNumber = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(language);
        if (!Enum.IsDefined(script))
        {
            throw new ArgumentOutOfRangeException(nameof(script), script, "Unknown writing script.");
        }

        List<Contributor> list = contributors.ToList();
        if (list.Contains(null!))
        {
            throw new ArgumentException("A record cannot list a null contributor.", nameof(contributors));
        }

        if (list.Count > MaxContributors)
        {
            throw new ArgumentException($"A record lists at most {MaxContributors} contributors, not {list.Count}.", nameof(contributors));
        }

        if (list.Distinct().Count() != list.Count)
        {
            throw new ArgumentException("A contributor appears twice with the same role.", nameof(contributors));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(edition, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(edition, 999);

        int latestYear = DateTime.UtcNow.Year + 1;
        if (publicationYear < FirstPrintingYear || publicationYear > latestYear)
        {
            throw new ArgumentOutOfRangeException(nameof(publicationYear), publicationYear, $"Publication year must be between {FirstPrintingYear} and {latestYear}.");
        }

        if (isbn is not null && publicationYear < FirstIsbnYear)
        {
            throw new ArgumentException($"An edition published in {publicationYear} cannot bear an ISBN (first assigned in {FirstIsbnYear}).", nameof(isbn));
        }

        if (isbn is not null && !isbn.HasIsbn10Form && publicationYear < FirstPrefix979Year)
        {
            throw new ArgumentException($"A 979 ISBN was first assigned in {FirstPrefix979Year}, after {publicationYear}.", nameof(isbn));
        }

        CheckScript(title, script, nameof(title));
        if (subtitle is not null)
        {
            CheckScript(subtitle, script, nameof(subtitle));
        }

        if (language.Script is not null && language.DeclaredScript != script)
        {
            throw new ArgumentException($"Language tag {language} names script '{language.Script}', but the record is in {script}.", nameof(language));
        }

        Id = id;
        Title = title;
        Subtitle = subtitle;
        _contributors = list;
        Language = language;
        Script = script;
        Edition = edition;
        PublicationYear = publicationYear;
        Isbn = isbn;
        Issn = issn;
        CallNumber = callNumber;
    }

    public RecordId Id { get; }

    public Title Title { get; }

    public Title? Subtitle { get; }

    /// <summary>The contributors in the order of the title page.</summary>
    public IReadOnlyList<Contributor> Contributors => _contributors.AsReadOnly();

    public LanguageTag Language { get; }

    public WritingScript Script { get; }

    public int Edition { get; }

    public int PublicationYear { get; }

    public Isbn13? Isbn { get; }

    public Issn? Issn { get; }

    public CallNumber? CallNumber { get; }

    /// <summary>A record with an ISSN and no ISBN describes a serial.</summary>
    public bool IsSerial => Issn is not null && Isbn is null;

    /// <summary>
    /// Whether both records describe the same edition of the same work: equal title and subtitle (NFC and case
    /// folding), same language tag, edition and publication year, and the same contributor names whatever their
    /// roles — unless both bear ISBNs and they differ, which marks two distinct manifestations.
    /// </summary>
    public bool DescribesSameWorkAs(BibliographicRecord other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Isbn is not null && other.Isbn is not null && !Isbn.Equals(other.Isbn))
        {
            return false;
        }

        return Title.Equals(other.Title)
            && Equals(Subtitle, other.Subtitle)
            && Language.Equals(other.Language)
            && Edition == other.Edition
            && PublicationYear == other.PublicationYear
            && ContributorNames().SetEquals(other.ContributorNames());
    }

    public HashSet<PersonName> ContributorNames() => _contributors.Select(c => c.Name).ToHashSet();

    private static void CheckScript(Title text, WritingScript script, string paramName)
    {
        if (text.HasLettersOfUnsupportedScript())
        {
            throw new ArgumentException($"'{text}' contains letters of a script the catalogue does not support.", paramName);
        }

        IReadOnlySet<WritingScript> found = text.LetterScripts();
        if (found.Any(s => s != script))
        {
            throw new ArgumentException($"'{text}' contains {string.Join(", ", found.Where(s => s != script))} letters, but the record is declared in {script}.", paramName);
        }
    }

    public override string ToString() => $"{Id} {Title} ({PublicationYear})";
}
